using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.HR
{
    // بنفس بنية CategoryService — بيانات هيكلية بسيطة، بلا صفحات (GetAll فقط)، Delete = تعطيل منطقي.
    public class DepartmentService : ServiceBase, IDepartmentService
    {
        protected override string PermissionPrefix => "Departments";
        protected override string StringPrefix => "Str.Department";
        protected override string EntityName => "Departments";

        private readonly IDepartmentRepository _repo;

        public DepartmentService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IDepartmentRepository repo) : base(permissions, settings, localization, audit) => _repo = repo;

        public Result<List<DepartmentDto>> GetAll(bool includeInactive = false) =>
            Result.Ok(_repo.GetAll(includeInactive).Select(ToDto).ToList());

        public Result<DepartmentDto> Create(CreateDepartmentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return Result.Fail<DepartmentDto>("اسم القسم مطلوب", ErrorCode.ValidationFailed);

            var department = new Department { Name = dto.Name, NameEn = dto.NameEn, ManagerId = dto.ManagerId, IsActive = dto.IsActive };
            var id = _repo.Insert(department);
            department.Id = id;

            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { department.Name });
            return Result.Ok(ToDto(department));
        }

        public Result Update(UpdateDepartmentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return Result.Fail("اسم القسم مطلوب", ErrorCode.ValidationFailed);

            var department = _repo.GetById(dto.Id);
            if (department == null) return Result.Fail("القسم غير موجود", ErrorCode.NotFound);

            department.Name = dto.Name; department.NameEn = dto.NameEn; department.ManagerId = dto.ManagerId; department.IsActive = dto.IsActive;
            _repo.Update(department);
            Audit.Log(EntityName, department.Id, AuditAction.Update, newValue: new { department.Name });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            var department = _repo.GetById(id);
            if (department == null) return Result.Fail("القسم غير موجود", ErrorCode.NotFound);

            _repo.Delete(id);
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private static DepartmentDto ToDto(Department d) => new()
        { Id = d.Id, Name = d.Name, NameEn = d.NameEn, ManagerId = d.ManagerId, IsActive = d.IsActive };
    }
}
