using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Common
{
    /// <summary>فئات الوحدات وحساباتها</summary>
    public class CategoryService : ServiceBase, ICategoryService
    {
        private const string LinkedModule = "AssetCategories";

        private readonly ICategoryRepository _repo;
        private readonly IAssetRepository _assets;
        private readonly AccountCases _tree;
        private readonly AccountSpec<Category>[] _accounts;

        protected override string PermissionPrefix => "Categories";
        protected override string StringPrefix => "Str.Category";
        protected override string EntityName => "Category";

        public CategoryService(ICategoryRepository repo, IPermissionService permissions, ISettingsProvider settings,
                                ILocalizationService localization, IAuditLogger audit, IAssetRepository assets, AccountCases tree)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo;
            _assets = assets;
            _tree = tree;
            _accounts = AddMirroredAccount.Specs<Category>(
                (db, _) => Root(db, SettingKeys.Accounts.FixedAssets), (db, _) => Root(db, SettingKeys.Accounts.AccumulatedDepreciation),
                c => c.Name, c => c.AccountCode, (c, code) => c.AccountCode = code,
                c => c.DepreciationAccountCode, (c, code) => c.DepreciationAccountCode = code, leaf: false);
        }

        private Field<Category>[] CategoryFields() => new Field<Category>[]
        {
            new(x => x.Name, "Str.Field.CategoryName", Required: true, Max: 200),
            new(x => x.ParentId, "", Must: c => c.ParentId != c.Id, Message: "Str.Category.SelfParent"),
            new(x => x.ParentId, "", Must: c => c.ParentId == null || _repo.GetById(c.ParentId.Value) != null, Message: "Str.Category.ParentNotFound"),
            new(x => x.ParentId, "", Must: c => c.ParentId == null || _repo.GetById(c.ParentId.Value)?.ModuleKey is not { } key || key == c.ModuleKey,
                Message: "Str.Category.ParentOtherModule"),
        };

        /// <summary>حسابا الفئة المرتبطة</summary>
        private IReadOnlyList<AccountSpec<Category>> AccountsOf(Category c) =>
            c.ModuleKey == LinkedModule ? _accounts : System.Array.Empty<AccountSpec<Category>>();

        private Result<Account> Root(PrimeDbContext db, string key) =>
            _tree.Root(db, Setting(key, ""), "Str.Category.AccountsMissing");

        private bool CanIn(string moduleKey, string action) => Permissions.Can($"{moduleKey}.{action}");

        public Result<List<CategoryDto>> GetAll(string moduleKey, bool includeInactive = false)
        {
            var all = _repo.GetAll(moduleKey, includeInactive);
            var byId = all.ToDictionary(c => c.Id);
            return Result.Ok(all.Select(c => ToDto(c, byId)).ToList());
        }

        public Result<CategoryDto> GetById(int id)
        {
            var category = _repo.GetById(id);
            if (category == null) return Result.Fail<CategoryDto>(Msg("NotFound"), ErrorCode.NotFound);

            var byId = _repo.GetAll(category.ModuleKey, includeInactive: true).ToDictionary(c => c.Id);
            return Result.Ok(ToDto(category, byId));
        }

        public Result<CategoryDto> Create(CreateCategoryDto dto)
        {
            if (!CanIn(dto.ModuleKey, "Create")) return FailDenied<CategoryDto>();

            var category = Rows.Copy(dto, new Category(), to =>
            {
                to.IsActive = true;
            });

            var saved = Check.Valid(category, CategoryFields()).Then(() => Commit(db =>
                _tree.Add.Run(db, category, AccountsOf(category)).Then(() =>
                {
                    category.Id = _repo.Insert(category, db);
                    return Result.Ok();
                })));
            if (saved.IsFailure) return saved.As<CategoryDto>();

            Audit.Log(EntityName, category.Id, AuditAction.Insert, newValue: new { category.Name, category.ModuleKey });

            var byId = _repo.GetAll(dto.ModuleKey, includeInactive: true).ToDictionary(c => c.Id);
            return Result.Ok(ToDto(category, byId));
        }

        public Result Update(UpdateCategoryDto dto)
        {
            var category = _repo.GetById(dto.Id);
            if (category == null) return Result.Fail(Msg("NotFound"), ErrorCode.NotFound);
            if (!CanIn(category.ModuleKey, "Edit")) return FailDenied();

            var accounts = AccountsOf(category);
            var names = AddEntityAccount.Names(category, accounts);

            Rows.Copy(dto, category);

            var saved = Check.Valid(category, CategoryFields()).Then(() => Commit(db =>
            {
                _repo.Update(category, db);
                _tree.Rename.Run(db, category, accounts, names);
                return Result.Ok();
            }));
            if (saved.IsFailure) return saved;

            Audit.Log(EntityName, category.Id, AuditAction.Update, newValue: new { category.Name, category.IsActive });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            var category = _repo.GetById(id);
            if (category == null) return Result.Fail(Msg("NotFound"), ErrorCode.NotFound);
            if (!CanIn(category.ModuleKey, "Delete")) return FailDenied();
            if (_repo.HasChildren(id)) return Result.Fail(Msg("HasChildren"), ErrorCode.ValidationFailed);
            if (_assets.AnyInCategory(id)) return Result.Fail(Msg("HasAssets"), ErrorCode.ValidationFailed);

            var accounts = AccountsOf(category);
            if (_tree.Guards.HasEntries(AddEntityAccount.Codes(category, accounts))) return Result.Fail(Msg("HasTransactions"), ErrorCode.ValidationFailed);

            Commit(db =>
            {
                _repo.Delete(id, db);
                _tree.Close.Run(db, category, accounts);
                return Result.Ok();
            });

            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }



        private static CategoryDto ToDto(Category c, Dictionary<int, Category> byId)
        {
            var parent = c.ParentId != null && byId.TryGetValue(c.ParentId.Value, out var p) ? p : null;
            return Rows.Copy(c, new CategoryDto(), to =>
            {
                to.ParentName = parent?.Name;
                to.HasChildren = byId.Values.Any(x => x.ParentId == c.Id);
            });
        }
    }
}
