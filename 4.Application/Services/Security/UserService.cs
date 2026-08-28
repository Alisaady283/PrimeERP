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
    // فوق جدول Users المُدار أصلاً عبر PermissionDb — راجع تعليق RoleService لنفس الاستثناء.
    public class UserService : ServiceBase, IUserService
    {
        protected override string PermissionPrefix => "Users";
        protected override string StringPrefix => "Str.User";
        protected override string EntityName => "Users";

        public UserService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) { }

        public Result<List<UserDto>> GetAll() => Result.Ok(PermissionDb.GetAllUsers().Select(ToDto).ToList());

        public Result<UserDto> Create(CreateUserDto dto)
        {
            if (!Can("Create")) return FailDenied<UserDto>();
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.DisplayName))
                return Result.Fail<UserDto>("اسم المستخدم وكلمة المرور والاسم مطلوبون", ErrorCode.ValidationFailed);
            if (PermissionDb.UsernameExists(dto.Username))
                return Result.Fail<UserDto>("اسم المستخدم مستخدَم بالفعل", ErrorCode.ValidationFailed);

            var id = PermissionDb.InsertUser(dto.Username, dto.Password, dto.DisplayName, dto.RoleId, dto.IsActive);
            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { dto.Username });

            return Result.Ok(PermissionDb.GetAllUsers().Where(u => u.Id == id).Select(ToDto).First());
        }

        public Result Update(UpdateUserDto dto)
        {
            if (!Can("Edit")) return FailDenied();
            if (string.IsNullOrWhiteSpace(dto.DisplayName)) return Fail("الاسم مطلوب", ErrorCode.ValidationFailed);

            PermissionDb.UpdateUser(dto.Id, dto.DisplayName, dto.RoleId, dto.IsActive);
            if (!string.IsNullOrWhiteSpace(dto.Password))
                PermissionDb.UpdateUserPassword(dto.Id, dto.Password);

            Audit.Log(EntityName, dto.Id, AuditAction.Update, newValue: new { dto.DisplayName });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            PermissionDb.DeleteUser(id);
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private static UserDto ToDto(PermissionDb.UserListRecord u)
        {
            var (variant, statusKey) = (u.IsActive ? StatusVariant.Success : StatusVariant.Danger, u.IsActive ? "Active" : "Inactive");
            return new UserDto
            {
                Id = u.Id, Username = u.Username, DisplayName = u.DisplayName, RoleId = u.RoleId, RoleName = u.RoleName,
                IsActive = u.IsActive, StatusVariant = variant, StatusText = LocalizationService.Get($"Str.{statusKey}"), LastLoginAt = u.LastLoginAt
            };
        }
    }
}
