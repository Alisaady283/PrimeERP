using PrimeERP.Domain.Contracts;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    /// <summary>تحقّق المستخدم</summary>
    public class UserValidator : ValidatorBase, IValidator<User>
    {
        private readonly IPermissionStore _store;
        private readonly bool _isEdit;

        public UserValidator(IPermissionStore store, bool isEdit = false)
        {
            _store = store;
            _isEdit = isEdit;
        }

        public ValidationResult Validate(User user)
        {
            var result = new ValidationResult();

            Required(result, "DisplayName", user.DisplayName, "الاسم الظاهر");
            Custom(result, "RoleId", user.RoleId > 0, "يجب اختيار دور للمستخدم");

            // الاسم لا يُعدَّل بعد الإنشاء
            if (_isEdit) return result;

            Required(result, "Username", user.Username, "اسم المستخدم");
            MinLength(result, "Username", user.Username, 3, "اسم المستخدم");
            Unique(result, "Username", _store.FindByUsername(user.Username) != null, "اسم المستخدم");

            return result;
        }
    }
}
