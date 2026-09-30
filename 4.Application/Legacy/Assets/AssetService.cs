using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Legacy.Admin;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Accounting;
using System.Linq;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Legacy.Parties;
using PrimeERP.Application.Legacy.Treasury;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Assets
{
    /// <summary>الأصل: إنشاءً وتعديلاً وبذراً</summary>
    public class AssetService : AssetMovementServiceBase<Asset, AssetDto, AssetFilter>, IAssetService
    {
        protected override string EntityName => "Assets";

        private readonly IAssetRepository _assets;
        private readonly ICategoryRepository _categories;
        private readonly INumberSequenceService _numbers;
        private readonly SettingAccounts _settingAccounts;
        private readonly AccountCases _tree;

        public AssetService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, Entries journals, AccountOf accountsOf,
            IAssetRepository assets, ICategoryRepository categories, INumberSequenceService numbers,
            SettingAccounts settingAccounts, AccountCases tree)
            : base(permissions, settings, localization, audit, journals, accountsOf)
        {
            _settingAccounts = settingAccounts;
            _tree = tree;
            _assets = assets;
            _categories = categories;
            _numbers = numbers;
        }

        protected override Asset FindById(int id) => _assets.GetById(id);

        protected override (List<Asset> Items, int Total) FindPaged(int page, int pageSize, AssetFilter filter)
        {
            filter ??= new AssetFilter();
            return _assets.GetPaged(page, pageSize, filter.SearchText, filter.IsActive, filter.CategoryId, filter.SortBy, filter.SortDescending);
        }

        protected override List<Asset> FindSearch(string term, int maxResults) => _assets.Search(term, maxResults);

        public Result SeedDefaults()
        {
            _settingAccounts.Adopt(SettingKeys.Accounts.FixedAssets, "1101001");
            _settingAccounts.Adopt(SettingKeys.Accounts.AccumulatedDepreciation, "1101002");

            _settingAccounts.Ensure(SettingKeys.Accounts.DepreciationExpense, "51", Msg("DepreciationExpenseAccount"));
            _settingAccounts.Ensure(SettingKeys.Accounts.CapitalGains,        "42", Msg("CapitalGainsAccount"));
            _settingAccounts.Ensure(SettingKeys.Accounts.CapitalLosses,       "52", Msg("CapitalLossesAccount"));

            return Result.Ok();
        }

        public Result<AssetDto> Create(CreateAssetDto dto)
        {
            if (!Can("Create")) return FailDenied<AssetDto>();

            var asset = Rows.Copy(dto, new Asset(), to =>
            {
                to.Code = _numbers.Next("Asset");
                to.RevaluedValue = dto.PurchaseCost;
                to.CurrentValue = dto.PurchaseCost;
            });

            var check = Check.Valid(asset, AssetFields);
            if (check.IsFailure) return check.As<AssetDto>();

            var funding = FundingAccount(dto.AcquisitionMethod, dto.FundingId);
            if (funding.IsFailure) return Result.Fail<AssetDto>(funding.ErrorMessage, funding.ErrorCode);

            asset.FundingAccountCode = funding.Value;

            var category = CategoryOf(dto.CategoryId);
            if (category.IsFailure) return Result.Fail<AssetDto>(category.ErrorMessage, category.ErrorCode);

            var posted = Acquire(asset, category.Value);
            if (posted.IsFailure) return Result.Fail<AssetDto>(posted.ErrorMessage, posted.ErrorCode);

            Audit.Log(EntityName, asset.Id, AuditAction.Insert, newValue: new { asset.Code, asset.Name });
            return Result.Ok(ToDto(asset));
        }

        public Result Update(UpdateAssetDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var asset = _assets.GetById(dto.Id);
            if (asset == null) return Fail("NotFound", ErrorCode.NotFound);

            var names = AddEntityAccount.Names(asset, Ledger(null));

            var entryChanged = asset.PurchaseCost != dto.PurchaseCost
                            || asset.PurchaseDate != dto.PurchaseDate
                            || asset.FundingId != dto.FundingId
                            || asset.AcquisitionMethod != dto.AcquisitionMethod;

            Rows.Copy(dto, asset, a => a.CurrentValue = AssetCalc.BookValue(
                a.RevaluedValue > 0 ? a.RevaluedValue : dto.PurchaseCost, a.AccumulatedDepreciation));

            var check = Check.Valid(asset, AssetFields);
            if (check.IsFailure) return check;

            var funding = FundingAccount(dto.AcquisitionMethod, dto.FundingId);
            if (funding.IsFailure) return funding;
            asset.FundingAccountCode = funding.Value;

            var saved = Commit(db =>
            {
                _assets.Update(asset, db);
                _tree.Rename.Run(db, asset, Ledger(null), names);

                if (!entryChanged || asset.JournalEntryId == null) return Result.Ok();

                ReverseEntry(db, asset.JournalEntryId);
                _assets.SetJournalEntryId(db, asset.Id, AcquisitionEntry(db, asset));
                return Result.Ok();
            });
            if (saved.IsFailure) return saved;

            var posted = PostMissingAcquisition(asset);
            if (posted.IsFailure) return posted;

            Audit.Log(EntityName, asset.Id, AuditAction.Update, newValue: new { asset.Name });
            return Result.Ok();
        }

        protected override int? EntryOf(Asset asset) => asset.JournalEntryId;
        protected override object AuditOf(Asset asset) => new { asset.Code };

        protected override Result Guard(Asset asset) =>
            _tree.Guards.HasEntries(AddEntityAccount.Codes(asset, Ledger(null)), asset.JournalEntryId) ? Fail("HasTransactions", ErrorCode.ValidationFailed) : Result.Ok();

        protected override Result Undo(PrimeDbContext db, Asset asset)
        {
            ReverseEntry(db, asset.JournalEntryId);
            _assets.Delete(asset.Id, CurrentUser, db);
            _tree.Close.Run(db, asset, Ledger(null));
            return Result.Ok();
        }

        private Result<string> FundingAccount(AssetAcquisition method, int? fundingId) =>
            Check.Valid(fundingId, new Field<int?>(x => x, "", Name: "FundingId", Required: true, Message: "Str.Asset.FundingMissing")).Then(() =>
                method == AssetAcquisition.Supplier
                    ? AccountsOf.Party(PartyKind.Supplier, fundingId.Value, "Str.Asset.FundingAccountMissing")
                    : AccountsOf.Treasury(fundingId.Value, "Str.Asset.FundingAccountMissing"));

        public static readonly Field<Asset>[] AssetFields =
        {
            new(x => x.Code, "Str.Field.AssetCode", Required: true),
            new(x => x.Name, "Str.Field.AssetName", Required: true, Max: 200),
            new(x => x.PurchaseCost, "Str.PurchaseCost", From: 0),
        };

        /// <summary>حسابا الأصل تحت فئته</summary>
        private AccountSpec<Asset>[] Ledger(Domain.Entities.Category category) => AddMirroredAccount.Specs<Asset>(
            (db, _) => ParentOf(db, category?.AccountCode), (db, _) => ParentOf(db, category?.DepreciationAccountCode),
            a => a.Name, a => a.AccountCode, (a, code) => a.AccountCode = code,
            a => a.DepreciationAccountCode, (a, code) => a.DepreciationAccountCode = code);

        private Result<Account> ParentOf(PrimeDbContext db, string code) =>
            _tree.Root(db, code, "Str.Asset.CategoryAccountMissing");

        private int AcquisitionEntry(PrimeDbContext db, Asset asset)
        {
            var (debit, credit) = (asset.AccountCode, asset.FundingAccountCode);
            return PostEntry(db, asset.PurchaseDate ?? DateTime.Today, $"{Msg("AcquisitionEntry")} — {asset.Name}", debit, credit, asset.PurchaseCost);
        }

        private Result Acquire(Asset asset, Domain.Entities.Category category) =>
            Commit(db => _tree.Add.Run(db, asset, Ledger(category)).Then(() =>
            {
                asset.Id = _assets.Insert(asset, db);
                asset.JournalEntryId = AcquisitionEntry(db, asset);
                _assets.SetJournalEntryId(db, asset.Id, asset.JournalEntryId.Value);
                return Result.Ok();
            }));

        private Result PostMissingAcquisition(Asset asset)
        {
            if (asset.JournalEntryId != null) return Result.Ok();
            if (string.IsNullOrWhiteSpace(asset.FundingAccountCode))
                return Result.Fail(Msg("FundingMissing"), ErrorCode.ValidationFailed);

            return Commit(db =>
            {
                _assets.SetJournalEntryId(db, asset.Id, AcquisitionEntry(db, asset));
                return Result.Ok();
            });
        }

        private Result<Domain.Entities.Category> CategoryOf(int? categoryId)
        {
            var picked = Check.Valid(categoryId, new Field<int?>(x => x, "", Name: "CategoryId", Required: true, Message: "Str.Asset.CategoryRequired"));
            if (picked.IsFailure) return picked.As<Domain.Entities.Category>();

            var category = _categories.GetById(categoryId.Value);
            if (category == null || string.IsNullOrWhiteSpace(category.AccountCode)
                                 || string.IsNullOrWhiteSpace(category.DepreciationAccountCode))
                return Result.Fail<Domain.Entities.Category>(Msg("CategoryAccountMissing"), ErrorCode.ValidationFailed);

            return Result.Ok(category);
        }




        protected override AssetDto ToDto(Asset a)
        {
            var (variant, status) = Rows.Active(a.IsActive);
            return Rows.Copy(a, new AssetDto(), to =>
            {
                to.StatusVariant = variant;
                to.StatusText = status;
                to.CanEdit = Can("Edit");
                to.CanDelete = Can("Delete");
            });
        }
    }
}
