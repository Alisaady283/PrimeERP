using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services.Common
{
    // تخدم عدة وحدات (ModuleKey مميّز) — بلا فحص صلاحية داخلي عمداً: الاستدعاء دائماً عبر أمر مُحمي مسبقاً
    // في طبقة الـVM/الحوار (نفس ثقة DialogRenderer بأي ServiceType آخر تستدعيه بالانعكاس).
    public class CategoryService : ServiceBase, ICategoryService
    {
        /// <summary>فئات الأصول وحدها تسكن شجرة الحسابات — الفئة تجميعيّ تحت جذر الأصول الثابتة،
        /// وأصولها أوراقٌ تحته. بقية الوحدات (الأصناف) تصنيفٌ بلا حساب كما كانت.</summary>
        private const string LinkedModule = "AssetCategories";

        private readonly ICategoryRepository _repo;
        private readonly Accounting.IAccountService _accounts;
        private readonly IJournalRepository _journals;
        private readonly IAssetRepository _assets;

        protected override string PermissionPrefix => "Categories";
        protected override string StringPrefix => "Str.Category";
        protected override string EntityName => "Category";

        public CategoryService(ICategoryRepository repo, IPermissionService permissions, ISettingsProvider settings,
                                ILocalizationService localization, IAuditLogger audit,
                                Accounting.IAccountService accounts, IJournalRepository journals,
                                IAssetRepository assets)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo;
            _accounts = accounts;
            _journals = journals;
            _assets = assets;
        }

        public Result<List<CategoryDto>> GetAll(string moduleKey, bool includeInactive = false)
        {
            var all = _repo.GetAll(moduleKey, includeInactive);
            var byId = all.ToDictionary(c => c.Id);
            return Result.Ok(all.Select(c => ToDto(c, byId)).ToList());
        }

        public Result<CategoryDto> GetById(int id)
        {
            var category = _repo.GetById(id);
            if (category == null) return Result.Fail<CategoryDto>("التصنيف غير موجود", ErrorCode.NotFound);

            var byId = _repo.GetAll(category.ModuleKey, includeInactive: true).ToDictionary(c => c.Id);
            return Result.Ok(ToDto(category, byId));
        }

        public Result<CategoryDto> Create(CreateCategoryDto dto)
        {
            var category = new Category { Name = dto.Name, ParentId = dto.ParentId, ModuleKey = dto.ModuleKey, Notes = dto.Notes, IsActive = true };

            var validation = new CategoryValidator(_repo).Validate(category);
            if (!validation.IsValid) return Result.Fail<CategoryDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            // الحسابان والسجل في **معاملة واحدة** كما يفعل العميل: لو فشل أيّهما تراجع الكل، فلا يبقى
            // حسابٌ يتيم في الشجرة. الاستثناء يُخرج المعاملة فتتراجع — نفس أسلوب CustomerService.Create.
            int id;
            try
            {
                id = Db.RunTransaction((conn, tx) =>
                {
                    if (category.ModuleKey == LinkedModule)
                    {
                        category.AccountCode = Account(conn, tx, SettingKeys.Accounts.FixedAssets, category.Name);
                        category.DepreciationAccountCode =
                            Account(conn, tx, SettingKeys.Accounts.AccumulatedDepreciation, Mirror(category.Name));
                    }

                    return _repo.Insert(category, conn, tx);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<CategoryDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            category.Id = id;
            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { category.Name, category.ModuleKey });

            var byId = _repo.GetAll(dto.ModuleKey, includeInactive: true).ToDictionary(c => c.Id);
            return Result.Ok(ToDto(category, byId));
        }

        public Result Update(UpdateCategoryDto dto)
        {
            var category = _repo.GetById(dto.Id);
            if (category == null) return Result.Fail("التصنيف غير موجود", ErrorCode.NotFound);

            category.Name = dto.Name;
            category.IsActive = dto.IsActive;
            category.Notes = dto.Notes;
            // ParentId متعمَّد بلا تعديل — IsReadOnlyOnEdit في CategoryDialogFactory (لا إعادة تأصيل بعد الإنشاء).

            var validation = new CategoryValidator(_repo).Validate(category);
            if (!validation.IsValid) return Result.Fail(validation.Errors.Values.ToList(), ErrorCode.ValidationFailed);

            Db.RunTransaction((conn, tx) =>
            {
                _repo.Update(category, conn, tx);

                // تغيير الاسم لا يضرّ ولو كانت عليها قيود — يُزامَن مع حسابَيها اتجاهاً واحداً.
                if (!string.IsNullOrWhiteSpace(category.AccountCode))
                    _accounts.UpdateName(conn, tx, category.AccountCode, category.Name);

                if (!string.IsNullOrWhiteSpace(category.DepreciationAccountCode))
                    _accounts.UpdateName(conn, tx, category.DepreciationAccountCode, Mirror(category.Name));
            });

            Audit.Log(EntityName, category.Id, AuditAction.Update, newValue: new { category.Name, category.IsActive });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            var category = _repo.GetById(id);
            if (category == null) return Result.Fail("التصنيف غير موجود", ErrorCode.NotFound);
            if (_repo.HasChildren(id)) return Result.Fail("لا يمكن حذف تصنيف له تصنيفات فرعية نشطة", ErrorCode.ValidationFailed);

            // حاجز الشجرة نفسه: لا يُحذَف حساب له أبناء. الفئة أبوها الأصول، فوجود أصلٍ واحد يمنع حذفها
            // — وهو الحاجز الذي يحمله AccountService.Delete العامّ، ومسار المالك يتخطّاه فيلزم هنا.
            if (_assets.GetPaged(1, 1, categoryId: id).Total > 0)
                return Result.Fail(Msg("HasAssets"), ErrorCode.ValidationFailed);

            // وحاجز القيود كما في العميل والمورد — على الحسابين.
            if (HasLines(category.AccountCode) || HasLines(category.DepreciationAccountCode))
                return Result.Fail(Msg("HasTransactions"), ErrorCode.ValidationFailed);

            Db.RunTransaction((conn, tx) =>
            {
                _repo.Delete(id, conn, tx);

                if (!string.IsNullOrWhiteSpace(category.AccountCode))
                    _accounts.Delete(conn, tx, category.AccountCode);

                if (!string.IsNullOrWhiteSpace(category.DepreciationAccountCode))
                    _accounts.Delete(conn, tx, category.DepreciationAccountCode);
            });
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        /// <summary>اسم المرآة — «مجمع» + اسم الفئة، فيُقرأ جانبا الشجرة متقابلَين.</summary>
        /// <summary>
        /// اسم حساب المجمّع المقابل. عامّ لأنه اصطلاح الشجرة لا تفصيلة فئة: يستورده AssetService لمجمّع
        /// الأصل وتسويةُ الإقلاع لما سبق الربط — فاسمٌ يُكتب في أربعة مواضع يتفرّق عند أول تغيير.
        /// </summary>
        public static string Mirror(string name) => $"مجمع {name}";

        private bool HasLines(string code) =>
            !string.IsNullOrWhiteSpace(code) && _journals.HasLinesForAccount(code);

        /// <summary>
        /// حساب تجميعيّ تحت الجذر المُعلَن، في معاملة المستدعي. SkipAutoLink إلزامي كما في العميل: يمنع
        /// AccountService من محاولة إنشاء كيانٍ من الحساب فتنشأ حلقة. يرمي عند الفشل ليتراجع الكل.
        /// </summary>
        private string Account(System.Data.Common.DbConnection conn, System.Data.Common.DbTransaction tx,
            string rootKey, string name)
        {
            var root = Setting(rootKey, "");
            if (string.IsNullOrWhiteSpace(root)) throw new InvalidOperationException(Msg("AccountsMissing"));

            var parent = _accounts.GetByCode(root);
            if (parent.IsFailure) throw new InvalidOperationException(parent.ErrorMessage);

            var created = _accounts.Create(conn, tx, new DTOs.Accounting.CreateAccountDto
            { ParentId = parent.Value.Id, Name = name, IsLeaf = false, SkipAutoLink = true });

            if (created.IsFailure) throw new InvalidOperationException(created.ErrorMessage);

            return created.Value.Code;
        }

        private static CategoryDto ToDto(Category c, Dictionary<int, Category> byId)
        {
            var parent = c.ParentId != null && byId.TryGetValue(c.ParentId.Value, out var p) ? p : null;
            return new CategoryDto
            {
                Id = c.Id, Name = c.Name, ParentId = c.ParentId, ParentName = parent?.Name,
                ModuleKey = c.ModuleKey, IsActive = c.IsActive, Notes = c.Notes,
                AccountCode = c.AccountCode, DepreciationAccountCode = c.DepreciationAccountCode,
                HasChildren = byId.Values.Any(x => x.ParentId == c.Id)
            };
        }
    }
}
