using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Security;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Security
{
    /// <summary>الأدوار وصلاحياتها</summary>
    public class RoleService : ServiceBase, IRoleService
    {
        protected override string PermissionPrefix => "Users";
        protected override string StringPrefix => "Str.Role";
        protected override string EntityName => "Roles";

        private readonly IPermissionStore _store;

        public RoleService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IPermissionStore store)
            : base(permissions, settings, localization, audit) => _store = store;

        public Result<List<RoleDto>> GetAll() =>
            Result.Ok(_store.GetAllRoles().Select(r => new RoleDto { Id = r.Id, Name = r.Name, NameAr = r.NameAr, IsSystem = r.IsSystem }).ToList());

        public Result<RoleDto> Create(CreateRoleDto dto)
        {
            if (!Can("ManageRoles")) return FailDenied<RoleDto>();
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.NameAr))
                return Result.Fail<RoleDto>("اسم الدور مطلوب", ErrorCode.ValidationFailed);

            var id = _store.InsertRole(dto.Name, dto.NameAr);
            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { dto.Name });
            return Result.Ok(new RoleDto { Id = id, Name = dto.Name, NameAr = dto.NameAr, IsSystem = false });
        }

        public Result Update(UpdateRoleDto dto)
        {
            if (!Can("ManageRoles")) return FailDenied();
            if (_store.IsSystemRole(dto.Id)) return Fail("لا يمكن تعديل دور نظامي", ErrorCode.ValidationFailed);
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.NameAr))
                return Fail("اسم الدور مطلوب", ErrorCode.ValidationFailed);

            _store.UpdateRole(dto.Id, dto.Name, dto.NameAr);
            Audit.Log(EntityName, dto.Id, AuditAction.Update, newValue: new { dto.Name });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("ManageRoles")) return FailDenied();
            if (_store.IsSystemRole(id)) return Fail("لا يمكن حذف دور نظامي", ErrorCode.ValidationFailed);
            if (_store.RoleHasUsers(id)) return Fail("لا يمكن حذف دور له مستخدمون", ErrorCode.ValidationFailed);

            _store.DeleteRole(id);
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }
    }
}
