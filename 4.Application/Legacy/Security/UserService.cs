using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Core;
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

namespace PrimeERP.Application.Legacy.Security
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
            var password = Check.Valid(dto, new Field<CreateUserDto>(x => x.Password, "", Required: true, Message: "Str.User.PasswordRequired"));
            if (password.IsFailure) return password.As<UserDto>();

            var check = Check.Valid(Rows.Copy(dto, new User()),
                UserFields(_store, isEdit: false));
            if (check.IsFailure) return check.As<UserDto>();

            var (hash, salt) = PasswordHasher.Hash(dto.Password);
            var id = _store.InsertUser(Rows.Copy(dto, new User(), to =>
            {
                to.PasswordHash = hash;
                to.Salt = salt;
            }));
            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { dto.Username });

            return Result.Ok(_store.GetAllUsers().Where(u => u.Id == id).Select(ToDto).First());
        }

        public Result Update(UpdateUserDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var check = Check.Valid(Rows.Copy(dto, new User()),
                UserFields(_store, isEdit: true));
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
            var (variant, status) = Rows.Active(u.IsActive);
            return Rows.Copy(u, new UserDto(), to =>
            {
                to.StatusVariant = variant;
                to.StatusText = status;
            });
        }

        /// <summary>شروط المستخدم</summary>
        public static Field<User>[] UserFields(IPermissionStore store, bool isEdit) => isEdit
            ? new Field<User>[] { Display, Role }
            : new Field<User>[]
            {
                Display, Role,
                new(x => x.Username, "Str.Username", Required: true, Min: 3),
                new(x => x.Username, "", Must: u => store.FindByUsername(u.Username) == null, Message: "Str.Rule.Duplicate",
                    Args: _ => new object[] { LocalizationService.Get("Str.Username") }),
            };

        private static readonly Field<User> Display = new(x => x.DisplayName, "Str.DisplayName", Required: true);
        private static readonly Field<User> Role = new(x => x.RoleId, "", Required: true, Message: "Str.User.RoleRequired");
    }
}
