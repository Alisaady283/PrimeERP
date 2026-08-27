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
    // تخدم عدة وحدات (ModuleKey مميّز) — بلا فحص صلاحية داخلي عمداً: الاستدعاء دائماً عبر أمر مُحمي مسبقاً
    // في طبقة الـVM/الحوار (نفس ثقة DialogRenderer بأي ServiceType آخر تستدعيه بالانعكاس).
    public class CategoryService : ServiceBase, ICategoryService
    {
        private readonly ICategoryRepository _repo;

        protected override string PermissionPrefix => "Categories";
        protected override string StringPrefix => "Str.Category";
        protected override string EntityName => "Category";

        public CategoryService(ICategoryRepository repo, IPermissionService permissions, ISettingsProvider settings,
                                ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) => _repo = repo;

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

            var id = _repo.Insert(category);
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

            _repo.Update(category);
            Audit.Log(EntityName, category.Id, AuditAction.Update, newValue: new { category.Name, category.IsActive });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            var category = _repo.GetById(id);
            if (category == null) return Result.Fail("التصنيف غير موجود", ErrorCode.NotFound);
            if (_repo.HasChildren(id)) return Result.Fail("لا يمكن حذف تصنيف له تصنيفات فرعية نشطة", ErrorCode.ValidationFailed);

            _repo.Delete(id);
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private static CategoryDto ToDto(Category c, Dictionary<int, Category> byId)
        {
            var parent = c.ParentId != null && byId.TryGetValue(c.ParentId.Value, out var p) ? p : null;
            return new CategoryDto
            {
                Id = c.Id, Name = c.Name, ParentId = c.ParentId, ParentName = parent?.Name,
                ModuleKey = c.ModuleKey, IsActive = c.IsActive, Notes = c.Notes,
                HasChildren = byId.Values.Any(x => x.ParentId == c.Id)
            };
        }
    }
}
