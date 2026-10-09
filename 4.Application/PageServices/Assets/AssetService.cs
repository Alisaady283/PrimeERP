using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.PageServices.Assets
{
    /// <summary>الأصل: إنشاءً وتعديلاً وبذراً</summary>
    public class AssetService : EntityService<Asset, Asset, Asset, Asset, AssetFilter>, IAssetService
    {
        protected override string PermissionPrefix => "Assets";
        protected override string StringPrefix => "Str.Asset";
        protected override string EntityName => "Assets";

        private readonly IAssetRepository _assets;
        private readonly ICategoryRepository _categories;
        private readonly SettingAccounts _settingAccounts;
        private readonly Entries _journals;
        private readonly AccountOf _accountsOf;
        private readonly AccountSpec<Asset>[] _ledger;
        private readonly IAssetDepreciationRepository _charges;
        private readonly IAssetRevaluationRepository _revaluations;
        private readonly IAssetDisposalRepository _disposals;

        public AssetService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, Entries journals, AccountOf accountsOf,
            IAssetRepository assets, ICategoryRepository categories, INumberSequenceService numbers,
            SettingAccounts settingAccounts, AccountCases tree, IAssetDepreciationRepository charges,
            IAssetRevaluationRepository revaluations, IAssetDisposalRepository disposals)
            : base(permissions, settings, localization, audit, numbers, tree)
        {
            _charges = charges;
            _revaluations = revaluations;
            _disposals = disposals;
            _journals = journals;
            _accountsOf = accountsOf;
            _settingAccounts = settingAccounts;
            _assets = assets;
            _categories = categories;
            _ledger = AddMirroredAccount.Specs<Asset>(
                (db, a) => ParentOf(db, _categories.GetById(a.CategoryId ?? 0, db)?.AccountCode),
                (db, a) => ParentOf(db, _categories.GetById(a.CategoryId ?? 0, db)?.DepreciationAccountCode),
                a => a.Name, a => a.AccountCode, (a, code) => a.AccountCode = code,
                a => a.DepreciationAccountCode, (a, code) => a.DepreciationAccountCode = code);
        }

        public static readonly Field<Asset>[] AssetFields =
        {
            new(x => x.Code, "Str.Field.AssetCode", Required: true),
            new(x => x.Name, "Str.Field.AssetName", Required: true, Max: 200),
            new(x => x.PurchaseCost, "Str.PurchaseCost", From: 0),
        };

        protected override Field<Asset>[] Fields => AssetFields;
        protected override string SequenceKey => "Asset";
        protected override IReadOnlyList<AccountSpec<Asset>> Accounts => _ledger;

        protected override Asset FindById(int id) => _assets.GetById(id);

        protected override (List<Asset> Items, int Total) FindPaged(int page, int pageSize, AssetFilter filter)
        {
            filter ??= new AssetFilter();
            return _assets.GetPaged(page, pageSize, filter.SearchText, filter.IsActive, filter.CategoryId, filter.SortBy, filter.SortDescending);
        }

        protected override List<Asset> FindSearch(string term, int maxResults) => _assets.Search(term, maxResults);

        protected override Asset ToDto(Asset a) => a;

        public Result SeedDefaults()
        {
            _settingAccounts.Adopt(SettingKeys.Accounts.FixedAssets, "1101001");
            _settingAccounts.Adopt(SettingKeys.Accounts.AccumulatedDepreciation, "1101002");
            _settingAccounts.Adopt(SettingKeys.Accounts.DepreciationExpense, "54");
            _settingAccounts.Adopt(SettingKeys.Accounts.CapitalGains, "4201");
            _settingAccounts.Adopt(SettingKeys.Accounts.CapitalLosses, "5601");
            return Result.Ok();
        }

        protected override Asset New(Asset asset)
        {
            asset.RevaluedValue = asset.PurchaseCost;
            asset.CurrentValue = asset.PurchaseCost;
            return asset;
        }

        protected override void Number(Asset asset, string code) => asset.Code = code;

        protected override Result Prepare(Asset asset, Asset stored)
        {
            if (stored != null && asset.CategoryId != stored.CategoryId) return Fail("CategoryLocked", ErrorCode.ValidationFailed);
            if (stored != null && HasMovements(stored.Id) && TermsChanged(asset, stored)) return Fail("AssetLocked", ErrorCode.ValidationFailed);

            if (stored?.JournalEntryId != null && AcquisitionChanged(asset, stored))
            {
                var reversible = Posting.EnsureReversible(_journals, stored.JournalEntryId.Value);
                if (reversible.IsFailure) return reversible;
            }

            if (stored != null)
                asset.CurrentValue = AssetCalc.CurrentValue(stored.RevaluedValue, asset.PurchaseCost, stored.AccumulatedDepreciation);

            var funding = FundingAccount(asset.AcquisitionMethod, asset.FundingId);
            if (funding.IsFailure) return funding;
            asset.FundingAccountCode = funding.Value;

            return stored == null
                ? Check.Valid(asset.CategoryId, new Field<int?>(x => x, "", Name: "CategoryId", Required: true, Message: "Str.Asset.CategoryRequired"))
                : Result.Ok();
        }

        protected override int Insert(PrimeDbContext db, Asset asset) => _assets.Insert(asset, db);

        protected override void Save(PrimeDbContext db, Asset asset) => _assets.Edit(asset, db);

        /// <summary>قيد الاقتناء بعد الحفظ</summary>
        protected override Result OnSaved(PrimeDbContext db, Asset asset, Asset stored)
        {
            if (stored?.JournalEntryId != null && !AcquisitionChanged(asset, stored)) return Result.Ok();

            Posting.Reverse(_journals, db, stored?.JournalEntryId);
            asset.JournalEntryId = AcquisitionEntry(db, asset);
            _assets.SetJournalEntryId(db, asset.Id, asset.JournalEntryId.Value);
            return Result.Ok();
        }

        protected override int? OwnEntry(Asset asset) => asset.JournalEntryId;

        protected override Result CanErase(Asset asset) => Posting.EnsureReversible(_journals, asset.JournalEntryId);

        protected override void Erase(PrimeDbContext db, Asset asset)
        {
            Posting.Reverse(_journals, db, asset.JournalEntryId);
            _assets.Delete(asset.Id, CurrentUser, db);
        }

        protected override object AuditValue(Asset asset) => new { asset.Code, asset.Name };

        protected override string DeleteDetails(Asset asset) => asset.Code;

        private bool HasMovements(int assetId) =>
            _charges.AnyAfter(assetId, DateTime.MinValue) || _revaluations.AnyAfter(assetId, DateTime.MinValue) || _disposals.AnyForAsset(assetId);

        private static bool TermsChanged(Asset asset, Asset stored) =>
            AcquisitionChanged(asset, stored)
            || stored.UsefulLifeYears != asset.UsefulLifeYears
            || stored.SalvageValue != asset.SalvageValue
            || stored.Location != asset.Location
            || stored.Notes != asset.Notes;

        private static bool AcquisitionChanged(Asset asset, Asset stored) =>
            stored.PurchaseCost != asset.PurchaseCost
            || stored.PurchaseDate != asset.PurchaseDate
            || stored.FundingId != asset.FundingId
            || stored.AcquisitionMethod != asset.AcquisitionMethod;

        private Result<string> FundingAccount(AssetAcquisition method, int? fundingId) =>
            Check.Valid(fundingId, new Field<int?>(x => x, "", Name: "FundingId", Required: true, Message: "Str.Asset.FundingMissing")).Then(() =>
                method == AssetAcquisition.Supplier
                    ? _accountsOf.Party(PartyKind.Supplier, fundingId.Value, "Str.Asset.FundingAccountMissing")
                    : _accountsOf.Treasury(fundingId.Value, "Str.Asset.FundingAccountMissing"));

        private Result<Account> ParentOf(PrimeDbContext db, string code) =>
            Tree.Root(db, code, "Str.Asset.CategoryAccountMissing");

        private int AcquisitionEntry(PrimeDbContext db, Asset asset) =>
            Posting.Entry(_journals, db, asset.PurchaseDate ?? DateTime.Today, $"{Msg("AcquisitionEntry")} — {asset.Name}", EntityName,
                asset.AccountCode, asset.FundingAccountCode, asset.PurchaseCost);
    }
}
