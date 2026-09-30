using PrimeERP.Application.Services.Entities;
using System;
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Core;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Security;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Security
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
            Result.Ok(_store.GetAllRoles().Select(r => Rows.Copy(r, new RoleDto())).ToList());

        public Result<RoleDto> Create(CreateRoleDto dto)
        {
            if (!Can("ManageRoles")) return FailDenied<RoleDto>();
            var input = Check.Valid(dto, RoleFields<CreateRoleDto>(x => x.Name, x => x.NameAr));
            if (input.IsFailure) return input.As<RoleDto>();

            var id = _store.InsertRole(dto.Name, dto.NameAr);
            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { dto.Name });
            return Result.Ok(Rows.Copy(dto, new RoleDto(), to =>
            {
                to.Id = id;
                to.IsSystem = false;
            }));
        }

        public Result Update(UpdateRoleDto dto)
        {
            if (!Can("ManageRoles")) return FailDenied();
            if (_store.IsSystemRole(dto.Id)) return Fail("SystemCannotEdit", ErrorCode.ValidationFailed);
            var input = Check.Valid(dto, RoleFields<UpdateRoleDto>(x => x.Name, x => x.NameAr));
            if (input.IsFailure) return input;

            _store.UpdateRole(dto.Id, dto.Name, dto.NameAr);
            Audit.Log(EntityName, dto.Id, AuditAction.Update, newValue: new { dto.Name });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("ManageRoles")) return FailDenied();
            if (_store.IsSystemRole(id)) return Fail("SystemCannotDelete", ErrorCode.ValidationFailed);
            if (_store.RoleHasUsers(id)) return Fail("HasUsers", ErrorCode.ValidationFailed);

            _store.DeleteRole(id);
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private static Field<T>[] RoleFields<T>(System.Linq.Expressions.Expression<Func<T, object>> name,
            System.Linq.Expressions.Expression<Func<T, object>> nameAr) => new Field<T>[]
        {
            new(name, "", Required: true, Message: "Str.Role.NameRequired"),
            new(nameAr, "", Required: true, Message: "Str.Role.NameRequired"),
        };
    }
}
