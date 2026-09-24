using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Security;
using PrimeERP.Application.Validation;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Security;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Security
{
    /// <summary>المستخدمون وكلمات مرورهم</summary>
    public class UserService : ServiceBase, IUserService
    {
        protected override string PermissionPrefix => "Users";
        protected override string StringPrefix => "Str.User";
        protected override string EntityName => "Users";

        private readonly IPermissionStore _store;

        public UserService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IPermissionStore store)
            : base(permissions, settings, localization, audit) => _store = store;

        public Result<List<UserDto>> GetAll() => Result.Ok(_store.GetAllUsers().Select(ToDto).ToList());

        public Result<UserDto> Create(CreateUserDto dto)
        {
            if (!Can("Create")) return FailDenied<UserDto>();
            if (string.IsNullOrWhiteSpace(dto.Password)) return Fail<UserDto>("كلمة المرور مطلوبة", ErrorCode.ValidationFailed);

            var check = Check(new UserValidator(_store),
                new User { Username = dto.Username, DisplayName = dto.DisplayName, RoleId = dto.RoleId });
            if (check.IsFailure) return check.As<UserDto>();

            var (hash, salt) = PasswordHasher.Hash(dto.Password);
            var id = _store.InsertUser(new User
            {
                Username = dto.Username, PasswordHash = hash, Salt = salt,
                DisplayName = dto.DisplayName, RoleId = dto.RoleId, IsActive = dto.IsActive
            });
            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { dto.Username });

            return Result.Ok(_store.GetAllUsers().Where(u => u.Id == id).Select(ToDto).First());
        }

        public Result Update(UpdateUserDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var check = Check(new UserValidator(_store, isEdit: true),
                new User { Id = dto.Id, DisplayName = dto.DisplayName, RoleId = dto.RoleId });
            if (check.IsFailure) return check;

            _store.UpdateUser(dto.Id, dto.DisplayName, dto.RoleId, dto.IsActive);
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                var (hash, salt) = PasswordHasher.Hash(dto.Password);
                _store.UpdateUserPassword(dto.Id, hash, salt);
            }

            Audit.Log(EntityName, dto.Id, AuditAction.Update, newValue: new { dto.DisplayName });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            _store.DeleteUser(id);
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private static UserDto ToDto(User u)
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
