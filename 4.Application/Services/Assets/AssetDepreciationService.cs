using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Domain.Rules;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services.Assets
{
    public interface IAssetDepreciationService
    {
        Result<decimal> MonthlyAmount(int assetId);

        /// <summary>يُنشئ أقساط كل الأصول المستحقّة حتى تاريخٍ — قسطٌ لكل أصلٍ عن كل شهر.</summary>
        Result<int> RunFor(DateTime upTo);

        Result<PagedResult<AssetDepreciationDto>> GetPaged(int page, int pageSize, AssetDepreciationFilter filter = null);
        Result<AssetDepreciationDto> GetById(int id);
        Result<AssetDepreciationDto> Create(CreateAssetDepreciationDto dto);
        Result Update(UpdateAssetDepreciationDto dto);
        Result Delete(int id);
    }

    /// <summary>
    /// قسط الإهلاك سجلٌّ مستقلّ كالسند: إنشاءٌ وتعديلٌ وحذف، ولكلٍّ قيده — فحذف القسط يحذف قيده معه،
    /// ولا يبقى قيدٌ يتيم. وزرّ «احتساب الإهلاك» يمرّ من نفس مسار الإنشاء لكل أصلٍ وكل شهرٍ مستحقّ،
    /// فلا مسار ترحيلٍ ثانٍ. ومجمّع الأصل ودفتريّته وآخر شهرٍ أُهلك **تُشتقّ من أقساطه** لا من رقمٍ
    /// محفوظ قد يتناقض معها.
    /// </summary>
    public class AssetDepreciationService
        : AssetMovementServiceBase<AssetDepreciation, AssetDepreciationDto, AssetDepreciationFilter>, IAssetDepreciationService
    {
        protected override string EntityName => "AssetDepreciation";

        private readonly IAssetRepository _assets;
        private readonly IAssetDepreciationRepository _charges;

        public AssetDepreciationService(IAssetRepository assets, IAssetDepreciationRepository charges,
            IJournalService journals, ISettingsService settingsService,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit, journals, settingsService)
        {
            _assets = assets; _charges = charges;
        }

        protected override AssetDepreciation FindById(int id) => _charges.GetById(id);

        protected override (List<AssetDepreciation> Items, int Total) FindPaged(int page, int pageSize, AssetDepreciationFilter filter)
        {
            filter ??= new AssetDepreciationFilter();
            return _charges.GetPaged(page, pageSize, filter.SearchText, filter.AssetId, filter.SortBy, filter.SortDescending);
        }

        protected override List<AssetDepreciation> FindSearch(string term, int maxResults) =>
            _charges.GetPaged(1, maxResults, term).Items;

        private static decimal Base(Asset asset) =>
            asset.RevaluedValue > 0 ? asset.RevaluedValue : asset.PurchaseCost;

        public Result<decimal> MonthlyAmount(int assetId)
        {
            var asset = _assets.GetById(assetId);
            return asset == null
                ? Result.Fail<decimal>(Msg("NotFound"), ErrorCode.NotFound)
                : Result.Ok(DepreciationRules.PerMonth(Base(asset), asset.SalvageValue, asset.UsefulLifeYears));
        }

        public Result<AssetDepreciationDto> Create(CreateAssetDepreciationDto dto)
        {
            if (!Can("Create")) return FailDenied<AssetDepreciationDto>();

            var expense = Account(SettingKeys.Accounts.DepreciationExpense);
            if (expense.IsFailure) return Result.Fail<AssetDepreciationDto>(expense.ErrorMessage, expense.ErrorCode);

            var asset = _assets.GetById(dto.AssetId);
            if (asset == null) return Result.Fail<AssetDepreciationDto>(Msg("NotFound"), ErrorCode.NotFound);
            if (string.IsNullOrWhiteSpace(asset.DepreciationAccountCode))
                return Result.Fail<AssetDepreciationDto>(Msg("MirrorMissing"), ErrorCode.ValidationFailed);

            var charge = new AssetDepreciation
            {
                AssetId = dto.AssetId,
                PeriodDate = DepreciationRules.EndOfMonth(dto.PeriodDate),
                Amount = dto.Amount,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? $"إهلاك {asset.Name} — {dto.PeriodDate:yyyy-MM}" : dto.Notes,
                CreatedBy = CurrentUser
            };

            var invalid = Check(new AssetDepreciationValidator(), charge);
            if (invalid.IsFailure) return Result.Fail<AssetDepreciationDto>(invalid.ErrorMessage, invalid.ErrorCode);

            var posted = Post(charge, asset, expense.Value);
            if (posted.IsFailure) return Result.Fail<AssetDepreciationDto>(posted.ErrorMessage, posted.ErrorCode);

            Audit.Log(EntityName, charge.Id, AuditAction.Insert, newValue: new { asset.Code, charge.PeriodDate, charge.Amount });
            return Result.Ok(ToDto(charge));
        }

        /// <summary>التعديل حذفٌ ثم إنشاء: القيد لا يُعدَّل في مكانه، ويبقى الأصل ودفاتره متطابقَين.</summary>
        public Result Update(UpdateAssetDepreciationDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            if (_charges.GetById(dto.Id) == null) return Fail("NotFound", ErrorCode.NotFound);

            var removed = Delete(dto.Id);
            if (removed.IsFailure) return removed;

            var created = Create(new CreateAssetDepreciationDto
            { AssetId = dto.AssetId, PeriodDate = dto.PeriodDate, Amount = dto.Amount, Notes = dto.Notes });

            return created.IsSuccess ? Result.Ok() : Result.Fail(created.ErrorMessage, created.ErrorCode);
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var charge = _charges.GetById(id);
            if (charge == null) return Fail("NotFound", ErrorCode.NotFound);

            var funds = EnsureReversible(charge.JournalEntryId);
            if (funds.IsFailure) return funds;

            Db.RunTransaction((conn, tx) =>
            {
                ReverseEntry(conn, tx, charge.JournalEntryId);
                _charges.Delete(id, conn, tx);

                Recalculate(conn, tx, charge.AssetId);
            });

            Audit.Log(EntityName, id, AuditAction.Delete, details: charge.Notes);
            return Result.Ok();
        }

        public Result<int> RunFor(DateTime upTo)
        {
            if (!Can("Create")) return FailDenied<int>();

            var expense = Account(SettingKeys.Accounts.DepreciationExpense);
            if (expense.IsFailure) return Result.Fail<int>(expense.ErrorMessage, expense.ErrorCode);

            var created = 0;

            foreach (var asset in _assets.GetPaged(1, 100000).Items
                         .Where(a => a.IsActive && a.UsefulLifeYears > 0 && !string.IsNullOrWhiteSpace(a.DepreciationAccountCode)))
            {
                var schedule = DepreciationRules.Schedule(
                    Base(asset), asset.SalvageValue, asset.UsefulLifeYears, asset.AccumulatedDepreciation,
                    DepreciationRules.FirstUndepreciatedMonth(asset.LastDepreciationDate, asset.PurchaseDate), upTo).ToList();

                foreach (var (period, amount) in schedule)
                {
                    var charge = new AssetDepreciation
                    {
                        AssetId = asset.Id, PeriodDate = period, Amount = amount,
                        Notes = $"إهلاك {asset.Name} — {period:yyyy-MM}", CreatedBy = CurrentUser
                    };

                    var posted = Post(charge, asset, expense.Value);
                    if (posted.IsFailure) return Result.Fail<int>(posted.ErrorMessage, posted.ErrorCode);

                    created++;
                }
            }

            if (created > 0) Audit.Log(EntityName, 0, AuditAction.Insert, details: $"{created} قسط إهلاك");
            return Result.Ok(created);
        }

        /// <summary>القسط وقيده في معاملة واحدة، ثم يُشتقّ مجمّع الأصل من أقساطه.</summary>
        private Result Post(AssetDepreciation charge, Asset asset, string expenseAccount)
        {
            try
            {
                Db.RunTransaction((conn, tx) =>
                {
                    charge.Id = _charges.Insert(charge, conn, tx);

                    // الدائن مجمّع الأصل نفسه داخل مجمّع فئته — فيجمع كل أبٍ أبناءه صعوداً بلا حسابٍ عامّ.
                    charge.JournalEntryId = PostEntry(conn, tx, charge.PeriodDate, charge.Notes,
                        expenseAccount, asset.DepreciationAccountCode, charge.Amount, asset.Name);

                    _charges.SetJournalEntryId(conn, tx, charge.Id, charge.JournalEntryId.Value);

                    Recalculate(conn, tx, charge.AssetId);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message, ErrorCode.ValidationFailed);
            }

            return Result.Ok();
        }

        /// <summary>
        /// مجمّع الأصل ودفتريّته وآخر شهرٍ أُهلك — كلها مشتقّة من أقساطه القائمة، فحذف أي قسطٍ أو
        /// إضافته يُصحّح الأصل تلقائياً ولا يبقى رقمٌ محفوظ يخالف سجلّاته.
        /// </summary>
        private void Recalculate(System.Data.Common.DbConnection conn, System.Data.Common.DbTransaction tx, int assetId)
        {
            var asset = _assets.GetById(assetId, conn, tx);
            if (asset == null) return;

            var charges = _charges.OfAsset(assetId, conn, tx);

            asset.AccumulatedDepreciation = charges.Sum(c => c.Amount);
            asset.CurrentValue = DepreciationRules.BookValue(Base(asset), asset.AccumulatedDepreciation);
            asset.LastDepreciationDate = charges.Count == 0 ? null : charges.Max(c => c.PeriodDate);

            _assets.Update(asset, conn, tx);
        }

        protected override AssetDepreciationDto ToDto(AssetDepreciation charge)
        {
            var asset = _assets.GetById(charge.AssetId);

            return new AssetDepreciationDto
            {
                Id = charge.Id, AssetId = charge.AssetId,
                AssetCode = asset?.Code, AssetName = asset?.Name,
                PeriodDate = charge.PeriodDate, Amount = charge.Amount,
                JournalEntryId = charge.JournalEntryId, Notes = charge.Notes,
                CreatedAt = charge.CreatedAt,
                CanEdit = Can("Edit"), CanDelete = Can("Delete")
            };
        }
    }
}
