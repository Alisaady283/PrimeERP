using PrimeERP.Data.Core;
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
using PrimeERP.Application.Services.Admin;

namespace PrimeERP.Application.Services.Assets
{
    /// <summary>قسط الإهلاك سجلٌّ مستقلّ</summary>
    public interface IAssetDepreciationService
    {
        Result<decimal> MonthlyAmount(int assetId);

        Result<int> RunFor(DateTime upTo);

        Result<PagedResult<AssetDepreciationDto>> GetPaged(int page, int pageSize, AssetDepreciationFilter filter = null);
        Result<AssetDepreciationDto> GetById(int id);
        Result<AssetDepreciationDto> Create(CreateAssetDepreciationDto dto);
        Result Update(UpdateAssetDepreciationDto dto);
        Result Delete(int id);
    }

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

            Tx(db =>
            {
                ReverseEntry(db, charge.JournalEntryId);
                _charges.Delete(id, db);

                Recalculate(db, charge.AssetId);
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

        private Result Post(AssetDepreciation charge, Asset asset, string expenseAccount)
        {
            try
            {
                Tx(db =>
                {
                    charge.Id = _charges.Insert(charge, db);

                    charge.JournalEntryId = PostEntry(db, charge.PeriodDate, charge.Notes,
                        expenseAccount, asset.DepreciationAccountCode, charge.Amount, asset.Name);

                    _charges.SetJournalEntryId(db, charge.Id, charge.JournalEntryId.Value);

                    Recalculate(db, charge.AssetId);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message, ErrorCode.ValidationFailed);
            }

            return Result.Ok();
        }

        private void Recalculate(PrimeDbContext db, int assetId)
        {
            var asset = _assets.GetById(assetId, db);
            if (asset == null) return;

            var charges = _charges.OfAsset(assetId, db);

            asset.AccumulatedDepreciation = charges.Sum(c => c.Amount);
            asset.CurrentValue = DepreciationRules.BookValue(Base(asset), asset.AccumulatedDepreciation);
            asset.LastDepreciationDate = charges.Count == 0 ? null : charges.Max(c => c.PeriodDate);

            _assets.Update(asset, db);
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
