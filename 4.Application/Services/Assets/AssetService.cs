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
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Treasury;
using PrimeERP.Domain.Results;
using PrimeERP.Domain.Rules;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Application.Services.Admin;

namespace PrimeERP.Application.Services.Assets
{
    /// <summary>الأصل: إنشاءً وتعديلاً وبذراً</summary>
    public class AssetService : AssetMovementServiceBase<Asset, AssetDto, AssetFilter>, IAssetService
    {
        protected override string EntityName => "Assets";

        private readonly IAssetRepository _assets;
        private readonly ICategoryRepository _categories;
        private readonly INumberSequenceService _numbers;
        private readonly ITreasuryService _treasuries;
        private readonly ISupplierService _suppliers;
        private readonly IAssetRevaluationRepository _revaluations;
        private readonly IAssetDepreciationRepository _charges;
        private readonly IAccountService _accounts;
        private readonly IJournalRepository _journalRepository;

        public AssetService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IJournalService journals, ISettingsService settingsService,
            IAssetRepository assets, ICategoryRepository categories, INumberSequenceService numbers,
            ITreasuryService treasuries, ISupplierService suppliers,
            IAssetRevaluationRepository revaluations, IAssetDepreciationRepository charges,
            IAccountService accounts, IJournalRepository journalRepository)
            : base(permissions, settings, localization, audit, journals, settingsService)
        {
            _accounts = accounts;
            _journalRepository = journalRepository;
            _assets = assets;
            _categories = categories;
            _numbers = numbers;
            _treasuries = treasuries;
            _suppliers = suppliers;
            _revaluations = revaluations;
            _charges = charges;
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
            Adopt(SettingKeys.Accounts.FixedAssets, "1101001");
            Adopt(SettingKeys.Accounts.AccumulatedDepreciation, "1101002");

            Ensure(SettingKeys.Accounts.DepreciationExpense, "51", "مصروف إهلاك الأصول الثابتة");
            Ensure(SettingKeys.Accounts.CapitalGains,        "42", "أرباح رأسمالية");
            Ensure(SettingKeys.Accounts.CapitalLosses,       "52", "خسائر رأسمالية");

            return Result.Ok();
        }

        private void Adopt(string key, string code)
        {
            if (!string.IsNullOrWhiteSpace(Setting<string>(key, ""))) return;
            if (_accounts.GetByCode(code).IsSuccess) Settings.SetRaw(key, code);
        }

        private void Ensure(string key, string parentCode, string name)
        {
            if (!string.IsNullOrWhiteSpace(Setting<string>(key, ""))) return;

            var parent = _accounts.GetByCode(parentCode);
            if (parent.IsFailure) return;

            var existing = _accounts.GetLeaves().Value?
                .FirstOrDefault(leaf => leaf.Name == name && (leaf.Code ?? "").StartsWith(parentCode));

            var code = existing?.Code ?? _accounts.Create(new CreateAccountDto
            { ParentId = parent.Value.Id, Name = name, IsLeaf = true, SkipAutoLink = true }).Value?.Code;

            if (!string.IsNullOrWhiteSpace(code)) Settings.SetRaw(key, code);
        }

        public Result<AssetDto> Create(CreateAssetDto dto)
        {
            if (!Can("Create")) return FailDenied<AssetDto>();

            var asset = new Asset
            {
                Code = _numbers.Next("Asset"), Name = dto.Name, CategoryId = dto.CategoryId, PurchaseDate = dto.PurchaseDate,
                PurchaseCost = dto.PurchaseCost, RevaluedValue = dto.PurchaseCost, CurrentValue = dto.PurchaseCost,
                AcquisitionMethod = dto.AcquisitionMethod, FundingId = dto.FundingId, Location = dto.Location,
                UsefulLifeYears = dto.UsefulLifeYears, SalvageValue = dto.SalvageValue,
                Notes = dto.Notes, IsActive = dto.IsActive, CreatedBy = CurrentUser
            };

            var check = Check(new AssetValidator(), asset);
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

            var nameChanged = asset.Name != dto.Name;

            var entryChanged = asset.PurchaseCost != dto.PurchaseCost
                            || asset.PurchaseDate != dto.PurchaseDate
                            || asset.FundingId != dto.FundingId
                            || asset.AcquisitionMethod != dto.AcquisitionMethod;

            asset.Name = dto.Name; asset.CategoryId = dto.CategoryId; asset.PurchaseDate = dto.PurchaseDate;
            asset.PurchaseCost = dto.PurchaseCost; asset.Location = dto.Location;
            asset.CurrentValue = DepreciationRules.BookValue(
                asset.RevaluedValue > 0 ? asset.RevaluedValue : dto.PurchaseCost, asset.AccumulatedDepreciation);
            asset.UsefulLifeYears = dto.UsefulLifeYears; asset.SalvageValue = dto.SalvageValue;
            asset.Notes = dto.Notes; asset.IsActive = dto.IsActive; asset.UpdatedBy = CurrentUser;

            var check = Check(new AssetValidator(), asset);
            if (check.IsFailure) return check;

            var funding = FundingAccount(dto.AcquisitionMethod, dto.FundingId);
            if (funding.IsSuccess)
            {
                asset.AcquisitionMethod = dto.AcquisitionMethod;
                asset.FundingId = dto.FundingId;
                asset.FundingAccountCode = funding.Value;
            }

            try
            {
                Tx(db =>
                {
                _assets.Update(asset, db);

                if (nameChanged && !string.IsNullOrWhiteSpace(asset.AccountCode))
                    _accounts.UpdateName(db, asset.AccountCode, asset.Name);

                if (nameChanged && !string.IsNullOrWhiteSpace(asset.DepreciationAccountCode))
                    _accounts.UpdateName(db, asset.DepreciationAccountCode, Common.CategoryService.Mirror(asset.Name));

                if (!entryChanged || asset.JournalEntryId == null) return;

                ReverseEntry(db, asset.JournalEntryId);

                var rebuilt = PostEntry(db, asset.PurchaseDate ?? DateTime.Today,
                    $"{Msg("AcquisitionEntry")} — {asset.Name}",
                    asset.AccountCode, asset.FundingAccountCode, asset.PurchaseCost);

                _assets.SetJournalEntryId(db, asset.Id, rebuilt);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message, ErrorCode.ValidationFailed);
            }

            var posted = PostMissingAcquisition(asset);
            if (posted.IsFailure) return posted;

            Audit.Log(EntityName, asset.Id, AuditAction.Update, newValue: new { asset.Name });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var asset = _assets.GetById(id);
            if (asset == null) return Fail("NotFound", ErrorCode.NotFound);

            if (HasEntries(asset)) return Fail("HasTransactions", ErrorCode.ValidationFailed);

            var funds = EnsureReversible(asset.JournalEntryId);
            if (funds.IsFailure) return funds;

            Tx(db =>
            {
                ReverseEntry(db, asset.JournalEntryId);
                _assets.Delete(id, CurrentUser, db);

                if (!string.IsNullOrWhiteSpace(asset.AccountCode))
                    _accounts.Delete(db, asset.AccountCode);

                if (!string.IsNullOrWhiteSpace(asset.DepreciationAccountCode))
                    _accounts.Delete(db, asset.DepreciationAccountCode);
            });

            Audit.Log(EntityName, id, AuditAction.Delete, details: asset.Code);
            return Result.Ok();
        }

        private Result<string> FundingAccount(AssetAcquisition method, int? fundingId)
        {
            if (fundingId == null) return Result.Fail<string>(Msg("FundingMissing"), ErrorCode.ValidationFailed);

            if (method == AssetAcquisition.Supplier)
            {
                var supplier = _suppliers.GetById(fundingId.Value);
                return supplier.IsSuccess
                    ? Required(supplier.Value.AccountCode, "FundingAccountMissing")
                    : Result.Fail<string>(supplier.ErrorMessage, supplier.ErrorCode);
            }

            var treasury = _treasuries.GetById(fundingId.Value);
            return treasury.IsSuccess
                ? Required(treasury.Value.AccountCode, "FundingAccountMissing")
                : Result.Fail<string>(treasury.ErrorMessage, treasury.ErrorCode);
        }

        private Result Acquire(Asset asset, Domain.Entities.Category category)
        {
            try
            {
                Tx(db =>
                {
                    asset.AccountCode = Leaf(db, category.AccountCode, asset.Name);
                    asset.DepreciationAccountCode = Leaf(db, category.DepreciationAccountCode, Common.CategoryService.Mirror(asset.Name));

                    asset.Id = _assets.Insert(asset, db);

                    asset.JournalEntryId = PostEntry(db, asset.PurchaseDate ?? DateTime.Today,
                        $"{Msg("AcquisitionEntry")} — {asset.Name}",
                        asset.AccountCode, asset.FundingAccountCode, asset.PurchaseCost);

                    _assets.SetJournalEntryId(db, asset.Id, asset.JournalEntryId.Value);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message, ErrorCode.ValidationFailed);
            }

            return Result.Ok();
        }

        private Result PostMissingAcquisition(Asset asset)
        {
            if (asset.JournalEntryId != null) return Result.Ok();
            if (string.IsNullOrWhiteSpace(asset.FundingAccountCode))
                return Result.Fail(Msg("FundingMissing"), ErrorCode.ValidationFailed);

            try
            {
                Tx(db =>
                {
                    var entry = PostEntry(db, asset.PurchaseDate ?? DateTime.Today,
                        $"{Msg("AcquisitionEntry")} — {asset.Name}",
                        asset.AccountCode, asset.FundingAccountCode, asset.PurchaseCost);

                    _assets.SetJournalEntryId(db, asset.Id, entry);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message, ErrorCode.ValidationFailed);
            }

            return Result.Ok();
        }

        private Result<Domain.Entities.Category> CategoryOf(int? categoryId)
        {
            if (categoryId == null)
                return Result.Fail<Domain.Entities.Category>(Msg("CategoryRequired"), ErrorCode.ValidationFailed);

            var category = _categories.GetById(categoryId.Value);
            if (category == null || string.IsNullOrWhiteSpace(category.AccountCode)
                                 || string.IsNullOrWhiteSpace(category.DepreciationAccountCode))
                return Result.Fail<Domain.Entities.Category>(Msg("CategoryAccountMissing"), ErrorCode.ValidationFailed);

            return Result.Ok(category);
        }

        private string Leaf(PrimeDbContext db, string parentCode, string name)
        {
            var parent = _accounts.GetByCode(parentCode);
            if (parent.IsFailure) throw new InvalidOperationException(parent.ErrorMessage);

            var created = _accounts.Create(db, new DTOs.Accounting.CreateAccountDto
            { ParentId = parent.Value.Id, Name = name, IsLeaf = true, SkipAutoLink = true });

            if (created.IsFailure) throw new InvalidOperationException(created.ErrorMessage);

            return created.Value.Code;
        }

        private bool HasEntries(Asset asset) =>
            HasLines(asset.AccountCode, asset.JournalEntryId) ||
            HasLines(asset.DepreciationAccountCode, asset.JournalEntryId);

        private bool HasLines(string code, int? ownEntryId) =>
            !string.IsNullOrWhiteSpace(code) && _journalRepository.HasLinesForAccount(code, ownEntryId);

        protected override AssetDto ToDto(Asset a)
        {
            var (variant, statusKey) = (a.IsActive ? StatusVariant.Success : StatusVariant.Danger, a.IsActive ? "Active" : "Inactive");
            return new AssetDto
            {
                Id = a.Id, Code = a.Code, Name = a.Name,
                CategoryId = a.CategoryId, CategoryName = a.CategoryName,
                AcquisitionMethod = a.AcquisitionMethod, FundingId = a.FundingId,
                PurchaseDate = a.PurchaseDate, PurchaseCost = a.PurchaseCost, RevaluedValue = a.RevaluedValue,
                CurrentValue = a.CurrentValue, Location = a.Location, Notes = a.Notes,
                UsefulLifeYears = a.UsefulLifeYears, SalvageValue = a.SalvageValue, AccumulatedDepreciation = a.AccumulatedDepreciation,
                LastDepreciationDate = a.LastDepreciationDate,
                IsActive = a.IsActive, StatusVariant = variant, StatusText = LocalizationService.Get($"Str.{statusKey}"),
                CreatedAt = a.CreatedAt, UpdatedAt = a.UpdatedAt,
                CanEdit = Can("Edit"), CanDelete = Can("Delete")
            };
        }
    }
}
