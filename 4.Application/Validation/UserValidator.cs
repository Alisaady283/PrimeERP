using PrimeERP.Domain.Contracts;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    public class UserValidator : ValidatorBase, IValidator<User>
    {
        private readonly bool _isEdit;

        public UserValidator(bool isEdit = false)
        {
            _isEdit = isEdit;
        }

        public ValidationResult Validate(User user)
        {
            var result = new ValidationResult();

            Required(result, "Username", user.Username, "اسم المستخدم");
            MinLength(result, "Username", user.Username, 3, "اسم المستخدم");
            Required(result, "DisplayName", user.DisplayName, "الاسم الظاهر");
            Custom(result, "RoleId", user.RoleId > 0, "يجب اختيار دور للمستخدم");

            if (!_isEdit)
            {
                var exists = PermissionDb.FindByUsername(user.Username) != null;
                Unique(result, "Username", exists, "اسم المستخدم");
            }

            return result;
        }
    }
}
