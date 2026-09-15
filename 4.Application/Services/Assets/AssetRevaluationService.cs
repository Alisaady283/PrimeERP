using System.Collections.Generic;
using System.Linq;
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
    public interface IAssetRevaluationService
    {
        Result<PagedResult<AssetRevaluationDto>> GetPaged(int page, int pageSize, AssetRevaluationFilter filter = null);
        Result<AssetRevaluationDto> GetById(int id);
        Result<AssetRevaluationDto> Create(CreateAssetRevaluationDto dto);
        Result Update(UpdateAssetRevaluationDto dto);
        Result Delete(int id);
    }

    /// <summary>
    /// إعادة تقييم الأصل: القيمة قبلها تُلتقَط من الأصل نفسه، والنوع يُشتقّ من الفرق — زيادةً تُقيَّد
    /// أرباحاً رأسمالية ونقصاً خسائر. مستندٌ كالسند: يُرحّل قيده، وحذفه يعكسه ويُعيد القيمة إلى ما قبله.
    /// </summary>
    public class AssetRevaluationService
        : AssetMovementServiceBase<AssetRevaluation, AssetRevaluationDto, AssetRevaluationFilter>, IAssetRevaluationService
    {
        protected override string EntityName => "AssetRevaluations";

        private readonly IAssetRevaluationRepository _revaluations;
        private readonly IAssetRepository _assets;

        public AssetRevaluationService(IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IJournalService journals, ISettingsService settingsService,
            IAssetRevaluationRepository revaluations, IAssetRepository assets)
            : base(permissions, settings, localization, audit, journals, settingsService)
        {
            _revaluations = revaluations;
            _assets = assets;
        }

        protected override AssetRevaluation FindById(int id) => _revaluations.GetById(id);

        protected override (List<AssetRevaluation> Items, int Total) FindPaged(int page, int pageSize, AssetRevaluationFilter filter)
        {
            filter ??= new AssetRevaluationFilter();
            return _revaluations.GetPaged(page, pageSize, filter.SearchText, filter.AssetId, filter.SortBy, filter.SortDescending);
        }

        protected override List<AssetRevaluation> FindSearch(string term, int maxResults) =>
            _revaluations.GetPaged(1, maxResults, term).Items;

        public Result<AssetRevaluationDto> Create(CreateAssetRevaluationDto dto)
        {
            if (!Can("Create")) return FailDenied<AssetRevaluationDto>();

            var asset = _assets.GetById(dto.AssetId);
            if (asset == null) return Result.Fail<AssetRevaluationDto>(Msg("NotFound"), ErrorCode.NotFound);

            var revaluation = new AssetRevaluation
            {
                AssetId = dto.AssetId,
                RevaluationDate = dto.RevaluationDate,
                // القيمة قبل التقييم من الأصل لا من المستخدم — فلا تُدخَل خطأً ولا تتقادم.
                OldValue = asset.RevaluedValue,
                NewValue = dto.NewValue,
                Notes = dto.Notes,
                CreatedBy = CurrentUser
            };

            var invalid = Check(new AssetRevaluationValidator(), revaluation);
            if (invalid.IsFailure) return Result.Fail<AssetRevaluationDto>(invalid.ErrorMessage, invalid.ErrorCode);

            var accounts = Sides(asset, revaluation.Difference);
            if (accounts.IsFailure) return Result.Fail<AssetRevaluationDto>(accounts.ErrorMessage, accounts.ErrorCode);

            try
            {
                Db.RunTransaction((conn, tx) =>
                {
                    revaluation.Id = _revaluations.Insert(revaluation, conn, tx);
                    Apply(conn, tx, asset, revaluation.NewValue);

                    revaluation.JournalEntryId = PostEntry(conn, tx, revaluation.RevaluationDate,
                        $"{Msg("Revaluation")} — {asset.Name}",
                        accounts.Value.Debit, accounts.Value.Credit, System.Math.Abs(revaluation.Difference));

                    _revaluations.SetJournalEntryId(conn, tx, revaluation.Id, revaluation.JournalEntryId.Value);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<AssetRevaluationDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log(EntityName, revaluation.Id, AuditAction.Insert,
                newValue: new { asset.Code, revaluation.OldValue, revaluation.NewValue });

            return Result.Ok(ToDto(revaluation));
        }

        /// <summary>التعديل حذفٌ ثم إنشاء: القيد لا يُعدَّل في مكانه — نفس ما يفعله السند.</summary>
        public Result Update(UpdateAssetRevaluationDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            if (_revaluations.GetById(dto.Id) == null) return Fail("NotFound", ErrorCode.NotFound);

            var removed = Delete(dto.Id);
            if (removed.IsFailure) return removed;

            var created = Create(new CreateAssetRevaluationDto
            {
                AssetId = dto.AssetId, RevaluationDate = dto.RevaluationDate,
                NewValue = dto.NewValue, Notes = dto.Notes
            });

            return created.IsSuccess ? Result.Ok() : Result.Fail(created.ErrorMessage, created.ErrorCode);
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var revaluation = _revaluations.GetById(id);
            if (revaluation == null) return Fail("NotFound", ErrorCode.NotFound);

            var asset = _assets.GetById(revaluation.AssetId);
            if (asset == null) return Result.Fail(Msg("NotFound"), ErrorCode.NotFound);

            var funds = EnsureReversible(revaluation.JournalEntryId);
            if (funds.IsFailure) return funds;

            Db.RunTransaction((conn, tx) =>
            {
                ReverseEntry(conn, tx, revaluation.JournalEntryId);

                // القيمة تعود إلى ما قبل هذه الإعادة — لا إلى التكلفة، فقد تسبقها إعاداتٌ أخرى.
                Apply(conn, tx, asset, revaluation.OldValue);
                _revaluations.Delete(revaluation.Id, CurrentUser, conn, tx);
            });

            Audit.Log(EntityName, id, AuditAction.Delete, details: asset.Code);
            return Result.Ok();
        }

        /// <summary>
        /// الزيادة أرباحٌ رأسمالية والنقص خسائر — بندان غير تشغيليَّين تحت «إيرادات أخرى» و«مصروفات
        /// أخرى». والأصل يزيد بالزيادة وينقص بالنقص في الطرف المقابل.
        /// </summary>
        private Result<(string Debit, string Credit)> Sides(Asset asset, decimal difference)
        {
            // حساب الأصل نفسه لا جذر الأصول الثابتة: الجذر تجميعيّ يحمل الفئات فلا يقبل ترحيلاً —
            // نفس ما يفعله الإهلاك بـ DepreciationAccountCode. الزيادة تُدين الأصل وتُقيّد أرباحاً
            // رأسمالية، والنقص يعكسهما.
            var own = Required(asset.AccountCode, "AccountsMissing");
            if (own.IsFailure) return Result.Fail<(string, string)>(own.ErrorMessage, own.ErrorCode);

            var counter = Account(difference > 0 ? SettingKeys.Accounts.CapitalGains : SettingKeys.Accounts.CapitalLosses);
            if (counter.IsFailure) return Result.Fail<(string, string)>(counter.ErrorMessage, counter.ErrorCode);

            return difference > 0
                ? Result.Ok((own.Value, counter.Value))
                : Result.Ok((counter.Value, own.Value));
        }

        /// <summary>القيمة الجديدة تصير أساس الإهلاك، والدفترية تتبعها ناقصةً ما أُهلك.</summary>
        private void Apply(System.Data.Common.DbConnection conn, System.Data.Common.DbTransaction tx,
            Asset asset, decimal value)
        {
            asset.RevaluedValue = value;
            asset.CurrentValue = DepreciationRules.BookValue(value, asset.AccumulatedDepreciation);
            _assets.Update(asset, conn, tx);
        }

        protected override AssetRevaluationDto ToDto(AssetRevaluation r)
        {
            var asset = _assets.GetById(r.AssetId);
            var increase = r.Difference >= 0;

            return new AssetRevaluationDto
            {
                Id = r.Id, AssetId = r.AssetId,
                AssetCode = asset?.Code, AssetName = asset?.Name,
                RevaluationDate = r.RevaluationDate,
                OldValue = r.OldValue, NewValue = r.NewValue, Difference = r.Difference,
                KindText = LocalizationService.Get(increase ? "Str.Asset.Increase" : "Str.Asset.Decrease"),
                KindVariant = increase ? StatusVariant.Success : StatusVariant.Danger,
                Notes = r.Notes, JournalEntryId = r.JournalEntryId,
                CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt,
                CanEdit = Can("Edit"), CanDelete = Can("Delete")
            };
        }
    }
}
