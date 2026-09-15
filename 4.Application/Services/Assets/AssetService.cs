using System;
using System.Collections.Generic;
using Db = PrimeERP.Data.Core.DbHelper;
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

namespace PrimeERP.Application.Services.Assets
{
    // بنفس بنية ProductService حرفياً — CrudServiceBase + Create/Update/Delete خاصة بالكيان.
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

        public Result<AssetDto> Create(CreateAssetDto dto)
        {
            if (!Can("Create")) return FailDenied<AssetDto>();

            var asset = new Asset
            {
                Code = _numbers.Next("Asset"), Name = dto.Name, CategoryId = dto.CategoryId, PurchaseDate = dto.PurchaseDate,
                // القيمة الدفترية والمُعاد تقييمها محسوبتان لا مُدخَلتين: أصلٌ جديد لم يُهلَك ولم يُعَد
                // تقييمه، فالثلاثة تبدأ بالتكلفة.
                PurchaseCost = dto.PurchaseCost, RevaluedValue = dto.PurchaseCost, CurrentValue = dto.PurchaseCost,
                AcquisitionMethod = dto.AcquisitionMethod, FundingId = dto.FundingId, Location = dto.Location,
                UsefulLifeYears = dto.UsefulLifeYears, SalvageValue = dto.SalvageValue,
                Notes = dto.Notes, IsActive = dto.IsActive, CreatedBy = CurrentUser
            };

            var validation = new AssetValidator().Validate(asset);
            if (!validation.IsValid) return Result.Fail<AssetDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            // الطرف الدائن يُحلّ قبل الحفظ: أصلٌ بلا مصدر تمويل لا يُرحَّل قيده فلا يُحفَظ ناقصاً.
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

            // نفس ما يفعله العميل: الاسم يُقارَن قبل تغييره ليُزامَن مع حسابيه إن تغيّر وحده.
            var nameChanged = asset.Name != dto.Name;

            // وما يمسّ قيد الاقتناء يُقارَن كذلك: تغيُّره يعني قيداً بمبلغٍ أو تاريخٍ أو طرفٍ خاطئ.
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

            var validation = new AssetValidator().Validate(asset);
            if (!validation.IsValid) return Result.Fail(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            // مموّل الأصل يُعاد حلّه في كل تعديل: به يُرحَّل قيد اقتناء أصلٍ أُنشئ قبل وجود الترحيل.
            var funding = FundingAccount(dto.AcquisitionMethod, dto.FundingId);
            if (funding.IsSuccess)
            {
                asset.AcquisitionMethod = dto.AcquisitionMethod;
                asset.FundingId = dto.FundingId;
                asset.FundingAccountCode = funding.Value;
            }

            try
            {
                Db.RunTransaction((conn, tx) =>
                {
                _assets.Update(asset, conn, tx);

                // الاسم يُزامَن مع الحسابين ولو كانت عليهما قيود — التسمية لا تضرّ.
                if (nameChanged && !string.IsNullOrWhiteSpace(asset.AccountCode))
                    _accounts.UpdateName(conn, tx, asset.AccountCode, asset.Name);

                if (nameChanged && !string.IsNullOrWhiteSpace(asset.DepreciationAccountCode))
                    _accounts.UpdateName(conn, tx, asset.DepreciationAccountCode, Common.CategoryService.Mirror(asset.Name));

                // قيد الاقتناء يُعكَس ويُعاد بناؤه — لا يُعدَّل في مكانه، تماماً كما يفعل السند عند
                // تعديله (حذفٌ ثم إنشاء). وإلا بقي القيد بالمبلغ القديم والأصل بالجديد.
                if (!entryChanged || asset.JournalEntryId == null) return;

                ReverseEntry(conn, tx, asset.JournalEntryId);

                var rebuilt = PostEntry(conn, tx, asset.PurchaseDate ?? DateTime.Today,
                    $"{Msg("AcquisitionEntry")} — {asset.Name}",
                    asset.AccountCode, asset.FundingAccountCode, asset.PurchaseCost);

                _assets.SetJournalEntryId(conn, tx, asset.Id, rebuilt);
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

            // أصلٌ عليه قيود لا يُحذَف — إهلاكٌ أو إعادة تقييمٍ أو بيع، كلّها أثرٌ في الدفاتر يسبقه.
            if (HasEntries(asset)) return Fail("HasTransactions", ErrorCode.ValidationFailed);

            var funds = EnsureReversible(asset.JournalEntryId);
            if (funds.IsFailure) return funds;

            // الحذف يعكس قيد الاقتناء كما تفعل كل مستندات النظام — في معاملة واحدة.
            Db.RunTransaction((conn, tx) =>
            {
                ReverseEntry(conn, tx, asset.JournalEntryId);
                _assets.Delete(id, CurrentUser, conn, tx);

                if (!string.IsNullOrWhiteSpace(asset.AccountCode))
                    _accounts.Delete(conn, tx, asset.AccountCode);

                if (!string.IsNullOrWhiteSpace(asset.DepreciationAccountCode))
                    _accounts.Delete(conn, tx, asset.DepreciationAccountCode);
            });

            Audit.Log(EntityName, id, AuditAction.Delete, details: asset.Code);
            return Result.Ok();
        }

        /// <summary>كود الحساب الدائن في قيد الاقتناء — حساب المموّل المختار: خزينة أو بنك أو مورد.</summary>
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

        /// <summary>يحفظ الأصل ويُرحّل قيد اقتنائه في معاملة واحدة.</summary>
        private Result Acquire(Asset asset, Domain.Entities.Category category)
        {
            try
            {
                Db.RunTransaction((conn, tx) =>
                {
                    // الحسابان والسجل والقيد في **معاملة واحدة** كما يفعل العميل: فشلُ أيّها يتراجع
                    // بالكل، فلا يبقى حسابٌ يتيم في الشجرة ولا أصلٌ بلا قيد.
                    asset.AccountCode = Leaf(conn, tx, category.AccountCode, asset.Name);
                    asset.DepreciationAccountCode = Leaf(conn, tx, category.DepreciationAccountCode, Common.CategoryService.Mirror(asset.Name));

                    asset.Id = _assets.Insert(asset, conn, tx);

                    asset.JournalEntryId = PostEntry(conn, tx, asset.PurchaseDate ?? DateTime.Today,
                        $"{Msg("AcquisitionEntry")} — {asset.Name}",
                        asset.AccountCode, asset.FundingAccountCode, asset.PurchaseCost);

                    _assets.SetJournalEntryId(conn, tx, asset.Id, asset.JournalEntryId.Value);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message, ErrorCode.ValidationFailed);
            }

            return Result.Ok();
        }

        /// <summary>يُرحّل قيد اقتناءٍ غائب لأصلٍ سبق وجود الترحيل — بعد أن حُدِّد مموّله.</summary>
        public Result PostMissingAcquisition(Asset asset)
        {
            if (asset.JournalEntryId != null) return Result.Ok();
            if (string.IsNullOrWhiteSpace(asset.FundingAccountCode))
                return Result.Fail(Msg("FundingMissing"), ErrorCode.ValidationFailed);

            try
            {
                Db.RunTransaction((conn, tx) =>
                {
                    var entry = PostEntry(conn, tx, asset.PurchaseDate ?? DateTime.Today,
                        $"{Msg("AcquisitionEntry")} — {asset.Name}",
                        asset.AccountCode, asset.FundingAccountCode, asset.PurchaseCost);

                    _assets.SetJournalEntryId(conn, tx, asset.Id, entry);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message, ErrorCode.ValidationFailed);
            }

            return Result.Ok();
        }

        /// <summary>
        /// حساب الأصل: ورقيّ تحت حساب فئته. بلا فئةٍ لا موضع له في الشجرة — والفئة تُنشئ حسابها عند
        /// إنشائها، فبقاؤه فارغاً يعني فئةً سبقت الربط تُصلحها تسوية الإقلاع.
        /// </summary>
        /// <summary>فئة الأصل بحسابيها — بلا فئةٍ لا موضع للأصل في الشجرة.</summary>
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

        /// <summary>ورقةٌ تحت حسابٍ أبٍ في معاملة المستدعي — SkipAutoLink إلزامي كما في العميل.</summary>
        private string Leaf(System.Data.Common.DbConnection conn, System.Data.Common.DbTransaction tx,
            string parentCode, string name)
        {
            var parent = _accounts.GetByCode(parentCode);
            if (parent.IsFailure) throw new InvalidOperationException(parent.ErrorMessage);

            var created = _accounts.Create(conn, tx, new DTOs.Accounting.CreateAccountDto
            { ParentId = parent.Value.Id, Name = name, IsLeaf = true, SkipAutoLink = true });

            if (created.IsFailure) throw new InvalidOperationException(created.ErrorMessage);

            return created.Value.Code;
        }

        /// <summary>
        /// أصلٌ عليه قيودٌ **غير قيد اقتنائه** لا يُحذَف — إهلاكٌ أو إعادة تقييمٍ أو بيع، كلّها أثرٌ في
        /// الدفاتر يسبقه. أمّا قيد الاقتناء فقيد الأصل نفسه: يُعكَس مع الحذف كما يعكس السند قيده، فلا
        /// يصحّ أن يمنعه.
        /// </summary>
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
                CategoryId = a.CategoryId, CategoryName = a.CategoryId != null ? _categories.GetById(a.CategoryId.Value)?.Name : null,
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
