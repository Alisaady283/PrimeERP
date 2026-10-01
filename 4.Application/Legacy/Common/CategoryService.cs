using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Common
{
    /// <summary>فئات الوحدات وحساباتها</summary>
    public class CategoryService : EntityService<Category, Category, Category, Category, CategoryFilter>, ICategoryService
    {
        private const string LinkedModule = "AssetCategories";

        private readonly ICategoryRepository _repo;
        private readonly IAssetRepository _assets;
        private readonly AccountSpec<Category>[] _accounts;

        protected override string PermissionPrefix => "Categories";
        protected override string StringPrefix => "Str.Category";
        protected override string EntityName => "Category";

        public CategoryService(ICategoryRepository repo, IPermissionService permissions, ISettingsProvider settings,
                                ILocalizationService localization, IAuditLogger audit, IAssetRepository assets, AccountCases tree)
            : base(permissions, settings, localization, audit, tree: tree)
        {
            _repo = repo;
            _assets = assets;
            _accounts = AddMirroredAccount.Specs<Category>(
                (db, _) => Root(db, SettingKeys.Accounts.FixedAssets), (db, _) => Root(db, SettingKeys.Accounts.AccumulatedDepreciation),
                c => c.Name, c => c.AccountCode, (c, code) => c.AccountCode = code,
                c => c.DepreciationAccountCode, (c, code) => c.DepreciationAccountCode = code, leaf: false);
        }

        private static readonly Field<Category>[] CategoryFields =
        {
            new(x => x.Name, "Str.Field.CategoryName", Required: true, Max: 200),
            new(x => x.ParentId, "", Must: c => c.ParentId != c.Id, Message: "Str.Category.SelfParent"),
        };

        protected override Field<Category>[] Fields => CategoryFields;

        /// <summary>الأب قائمٌ في وحدته</summary>
        protected override Result Prepare(Category c, Category stored)
        {
            if (c.ParentId == null) return Result.Ok();

            var parent = _repo.GetById(c.ParentId.Value);
            if (parent == null) return Fail("ParentNotFound", ErrorCode.ValidationFailed);
            return parent.ModuleKey != null && parent.ModuleKey != c.ModuleKey ? Fail("ParentOtherModule", ErrorCode.ValidationFailed) : Result.Ok();
        }

        /// <summary>حسابا الفئة المرتبطة</summary>
        protected override IReadOnlyList<AccountSpec<Category>> AccountsOf(Category c) =>
            c.ModuleKey == LinkedModule ? _accounts : Array.Empty<AccountSpec<Category>>();

        protected override bool CanOn(Category c, string action) => Permissions.Can($"{c.ModuleKey}.{action}");

        private Result<Account> Root(PrimeDbContext db, string key) =>
            Tree.Root(db, Setting(key, ""), "Str.Category.AccountsMissing");

        public Result<List<Category>> GetAll(string moduleKey, bool includeInactive = false) =>
            Result.Ok(_repo.GetAll(moduleKey, includeInactive));

        protected override Category FindById(int id) => _repo.GetById(id);

        protected override (List<Category> Items, int Total) FindPaged(int page, int pageSize, CategoryFilter filter)
        {
            var rows = _repo.GetAll(filter?.ModuleKey, filter?.IncludeInactive ?? false);
            return (rows, rows.Count);
        }

        protected override List<Category> FindSearch(string term, int maxResults) => _repo.Search(term, maxResults);

        protected override Category ToDto(Category c) => c;

        protected override Category New(Category c)
        {
            c.IsActive = true;
            return c;
        }

        protected override int Insert(PrimeDbContext db, Category c) => _repo.Insert(c, db);

        protected override void Save(PrimeDbContext db, Category c) => _repo.Update(c, db);

        protected override void Erase(PrimeDbContext db, Category c) => _repo.Delete(c.Id, db);

        protected override Result CanErase(Category c)
        {
            if (_repo.HasChildren(c.Id)) return Fail("HasChildren", ErrorCode.ValidationFailed);
            return _assets.AnyInCategory(c.Id) ? Fail("HasAssets", ErrorCode.ValidationFailed) : Result.Ok();
        }

        protected override object AuditValue(Category c) => new { c.Name, c.ModuleKey, c.IsActive };
    }
}
