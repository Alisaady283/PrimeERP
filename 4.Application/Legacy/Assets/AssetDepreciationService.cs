using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Legacy.Admin;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Assets
{
    /// <summary>قسط الإهلاك سجلٌّ مستقلّ</summary>
    public interface IAssetDepreciationService
    {
        Result<decimal> MonthlyAmount(int assetId);

        Result<int> RunFor(DateTime upTo);

        Result<PagedResult<AssetDepreciation>> GetPaged(int page, int pageSize, AssetDepreciationFilter filter = null);
        Result<AssetDepreciation> GetById(int id);
        Result<AssetDepreciation> Create(AssetDepreciation charge);
        Result Update(AssetDepreciation charge);
        Result Delete(int id);
    }

    public class AssetDepreciationService
        : AssetMovementServiceBase<AssetDepreciation, AssetDepreciation, AssetDepreciationFilter>, IAssetDepreciationService
    {
        protected override string EntityName => "AssetDepreciation";

        private readonly IAssetRepository _assets;
        private readonly IAssetDepreciationRepository _charges;
        private readonly DepreciationCharges _depreciation;

        public AssetDepreciationService(IAssetRepository assets, IAssetDepreciationRepository charges, DepreciationCharges depreciation,
            Entries journals, AccountOf accountsOf,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit, journals, accountsOf)
        {
            _assets = assets; _charges = charges; _depreciation = depreciation;
        }

        protected override AssetDepreciation FindById(int id) => _charges.GetById(id);

        protected override (List<AssetDepreciation> Items, int Total) FindPaged(int page, int pageSize, AssetDepreciationFilter filter)
        {
            filter ??= new AssetDepreciationFilter();
            return _charges.GetPaged(page, pageSize, filter.SearchText, filter.AssetId, filter.SortBy, filter.SortDescending);
        }

        protected override List<AssetDepreciation> FindSearch(string term, int maxResults) =>
            _charges.GetPaged(1, maxResults, term).Items;

        public Result<decimal> MonthlyAmount(int assetId)
        {
            var asset = _assets.GetById(assetId);
            return asset == null
                ? Result.Fail<decimal>(Msg("NotFound"), ErrorCode.NotFound)
                : Result.Ok(AssetCalc.PerMonth(AssetCalc.Basis(asset.RevaluedValue, asset.PurchaseCost), asset.SalvageValue, asset.UsefulLifeYears));
        }

        public Result<AssetDepreciation> Create(AssetDepreciation charge) => Record(db => Write(db, charge));

        public Result Update(AssetDepreciation charge) => Replace(charge, Write);

        public static readonly Field<AssetDepreciation>[] ChargeFields =
        {
            new(x => x.AssetId, "", Required: true, Message: "Str.Asset.PickAsset"),
            new(x => x.PeriodDate, "", Required: true, Message: "Str.Asset.DepreciationMonthRequired"),
            new(x => x.Amount, "Str.Asset.ChargeAmount", From: 0),
        };

        protected override int? EntryOf(AssetDepreciation charge) => charge.JournalEntryId;
        protected override object AuditOf(AssetDepreciation charge) => new { charge.AssetId, charge.PeriodDate, charge.Amount };

        private Result<AssetDepreciation> Write(PrimeDbContext db, AssetDepreciation charge)
        {
            var expense = AccountsOf.Setting(SettingKeys.Accounts.DepreciationExpense, "Str.Asset.AccountsMissing");
            if (expense.IsFailure) return expense.As<AssetDepreciation>();

            var asset = _assets.GetById(charge.AssetId, db);
            if (asset == null) return Fail<AssetDepreciation>("NotFound", ErrorCode.NotFound);
            var mirror = AccountOf.Required(asset.DepreciationAccountCode, "Str.Asset.MirrorMissing");
            if (mirror.IsFailure) return mirror.As<AssetDepreciation>();

            if (string.IsNullOrWhiteSpace(charge.Notes)) charge.Notes = Note(asset, charge.PeriodDate);
            charge.PeriodDate = AssetCalc.EndOfMonth(charge.PeriodDate);

            return Check.Valid(charge, ChargeFields).Then(() =>
            {
                _depreciation.Post(db, charge, asset, expense.Value, EntityName);
                return Result.Ok(charge);
            });
        }

        protected override Result Undo(PrimeDbContext db, AssetDepreciation charge)
        {
            Posting.Reverse(Journals, db, charge.JournalEntryId);
            _charges.Delete(charge.Id, db);
            _depreciation.Recalculate(db, charge.AssetId);
            return Result.Ok();
        }

        /// <summary>تشغيلةٌ في معاملةٍ واحدة</summary>
        public Result<int> RunFor(DateTime upTo)
        {
            if (!Can("Create")) return FailDenied<int>();

            var expense = AccountsOf.Setting(SettingKeys.Accounts.DepreciationExpense, "Str.Asset.AccountsMissing");
            if (expense.IsFailure) return expense.As<int>();

            var run = Commit(db => Result.Ok(_depreciation.Run(db, upTo, expense.Value, EntityName, Note)));
            if (run.IsFailure) return run;

            if (run.Value > 0) Audit.Log(EntityName, 0, AuditAction.Insert, details: Msg("DepreciationRunLog", run.Value));
            return run;
        }

        private string Note(Asset asset, DateTime period) => Msg("DepreciationNote", asset.Name, period);

        protected override AssetDepreciation ToDto(AssetDepreciation charge) => charge;
    }
}
