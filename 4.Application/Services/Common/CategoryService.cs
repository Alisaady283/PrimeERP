using PrimeERP.Data.Core;
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

namespace PrimeERP.Application.Services.Common
{
    /// <summary>فئات الوحدات وحساباتها</summary>
    public class CategoryService : ServiceBase, ICategoryService
    {
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

            var check = Check(new CategoryValidator(_repo), category);
            if (check.IsFailure) return check.As<CategoryDto>();

            int id;
            try
            {
                id = Tx(db =>
                {
                    if (category.ModuleKey == LinkedModule)
                    {
                        category.AccountCode = Account(db, SettingKeys.Accounts.FixedAssets, category.Name);
                        category.DepreciationAccountCode =
                            Account(db, SettingKeys.Accounts.AccumulatedDepreciation, Mirror(category.Name));
                    }

                    return _repo.Insert(category, db);
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

            var validation = new CategoryValidator(_repo).Validate(category);
            if (!validation.IsValid) return Result.Fail(validation.Errors.Values.ToList(), ErrorCode.ValidationFailed);

            Tx(db =>
            {
                _repo.Update(category, db);

                if (!string.IsNullOrWhiteSpace(category.AccountCode))
                    _accounts.UpdateName(db, category.AccountCode, category.Name);

                if (!string.IsNullOrWhiteSpace(category.DepreciationAccountCode))
                    _accounts.UpdateName(db, category.DepreciationAccountCode, Mirror(category.Name));
            });

            Audit.Log(EntityName, category.Id, AuditAction.Update, newValue: new { category.Name, category.IsActive });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            var category = _repo.GetById(id);
            if (category == null) return Result.Fail("التصنيف غير موجود", ErrorCode.NotFound);
            if (_repo.HasChildren(id)) return Result.Fail("لا يمكن حذف تصنيف له تصنيفات فرعية نشطة", ErrorCode.ValidationFailed);

            if (_assets.GetPaged(1, 1, categoryId: id).Total > 0)
                return Result.Fail(Msg("HasAssets"), ErrorCode.ValidationFailed);

            if (HasLines(category.AccountCode) || HasLines(category.DepreciationAccountCode))
                return Result.Fail(Msg("HasTransactions"), ErrorCode.ValidationFailed);

            Tx(db =>
            {
                _repo.Delete(id, db);

                if (!string.IsNullOrWhiteSpace(category.AccountCode))
                    _accounts.Delete(db, category.AccountCode);

                if (!string.IsNullOrWhiteSpace(category.DepreciationAccountCode))
                    _accounts.Delete(db, category.DepreciationAccountCode);
            });
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        public static string Mirror(string name) => $"مجمع {name}";

        private bool HasLines(string code) =>
            !string.IsNullOrWhiteSpace(code) && _journals.HasLinesForAccount(code);

        private string Account(PrimeDbContext db, string rootKey, string name)
        {
            var root = Setting(rootKey, "");
            if (string.IsNullOrWhiteSpace(root)) throw new InvalidOperationException(Msg("AccountsMissing"));

            var parent = _accounts.GetByCode(root);
            if (parent.IsFailure) throw new InvalidOperationException(parent.ErrorMessage);

            var created = _accounts.Create(db, new DTOs.Accounting.CreateAccountDto
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
