using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Core;
using System.Collections.Generic;
using PrimeERP.Domain.Entities;
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

        public Result<List<Role>> GetAll() => Result.Ok(_store.GetAllRoles());

        public Result<Role> Create(Role role)
        {
            if (!Can("ManageRoles")) return FailDenied<Role>();
            var input = Check.Valid(role, RoleFields);
            if (input.IsFailure) return input.As<Role>();

            role.Id = _store.InsertRole(role.Name, role.NameAr);
            role.IsSystem = false;
            Audit.Log(EntityName, role.Id, AuditAction.Insert, newValue: new { role.Name });
            return Result.Ok(role);
        }

        public Result Update(Role role)
        {
            if (!Can("ManageRoles")) return FailDenied();
            if (_store.IsSystemRole(role.Id)) return Fail("SystemCannotEdit", ErrorCode.ValidationFailed);
            var input = Check.Valid(role, RoleFields);
            if (input.IsFailure) return input;

            _store.UpdateRole(role.Id, role.Name, role.NameAr);
            Audit.Log(EntityName, role.Id, AuditAction.Update, newValue: new { role.Name });
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

        private static readonly Field<Role>[] RoleFields =
        {
            new(x => x.Name, "", Required: true, Message: "Str.Role.NameRequired"),
            new(x => x.NameAr, "", Required: true, Message: "Str.Role.NameRequired"),
        };
    }
}
