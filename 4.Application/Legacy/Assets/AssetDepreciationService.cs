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

        public AssetDepreciationService(IAssetRepository assets, IAssetDepreciationRepository charges,
            Entries journals, AccountOf accountsOf,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit, journals, accountsOf)
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

            if (string.IsNullOrWhiteSpace(charge.Notes)) charge.Notes = Msg("DepreciationNote", asset.Name, charge.PeriodDate);
            charge.PeriodDate = AssetCalc.EndOfMonth(charge.PeriodDate);

            return Check.Valid(charge, ChargeFields).Then(() =>
            {
                Post(db, charge, asset, expense.Value);
                return Result.Ok(charge);
            });
        }

        protected override Result Undo(PrimeDbContext db, AssetDepreciation charge)
        {
            Posting.Reverse(Journals, db, charge.JournalEntryId);
            _charges.Delete(charge.Id, db);
            Recalculate(db, charge.AssetId);
            return Result.Ok();
        }

        /// <summary>تشغيلةٌ في معاملةٍ واحدة</summary>
        public Result<int> RunFor(DateTime upTo)
        {
            if (!Can("Create")) return FailDenied<int>();

            var expense = AccountsOf.Setting(SettingKeys.Accounts.DepreciationExpense, "Str.Asset.AccountsMissing");
            if (expense.IsFailure) return expense.As<int>();

            var run = Commit(db =>
            {
                var created = 0;
                foreach (var asset in _assets.Depreciable(db))
                {
                    var schedule = AssetCalc.Schedule(
                        AssetCalc.Basis(asset.RevaluedValue, asset.PurchaseCost), asset.SalvageValue, asset.UsefulLifeYears, asset.AccumulatedDepreciation,
                        AssetCalc.FirstUndepreciatedMonth(asset.LastDepreciationDate, asset.PurchaseDate), upTo).ToList();

                    foreach (var (period, amount) in schedule)
                    {
                        Post(db, new AssetDepreciation
                        {
                            AssetId = asset.Id, PeriodDate = period, Amount = amount,
                            Notes = Msg("DepreciationNote", asset.Name, period)
                        }, asset, expense.Value);
                        created++;
                    }
                }
                return Result.Ok(created);
            });
            if (run.IsFailure) return run;

            if (run.Value > 0) Audit.Log(EntityName, 0, AuditAction.Insert, details: Msg("DepreciationRunLog", run.Value));
            return run;
        }

        private void Post(PrimeDbContext db, AssetDepreciation charge, Asset asset, string expenseAccount)
        {
            charge.Id = _charges.Insert(charge, db);

            var (debit, credit) = (expenseAccount, asset.DepreciationAccountCode);
            charge.JournalEntryId = Posting.Entry(Journals, db, charge.PeriodDate, charge.Notes, EntityName, debit, credit, charge.Amount, asset.Name);

            _charges.SetJournalEntryId(db, charge.Id, charge.JournalEntryId.Value);
            Recalculate(db, charge.AssetId);
        }

        private void Recalculate(PrimeDbContext db, int assetId)
        {
            var asset = _assets.GetById(assetId, db);
            if (asset == null) return;

            (asset.AccumulatedDepreciation, asset.LastDepreciationDate) = _charges.TotalOf(assetId, db);
            asset.CurrentValue = AssetCalc.CurrentValue(asset.RevaluedValue, asset.PurchaseCost, asset.AccumulatedDepreciation);

            _assets.Update(asset, db);
        }

        protected override AssetDepreciation ToDto(AssetDepreciation charge) => charge;
    }
}
