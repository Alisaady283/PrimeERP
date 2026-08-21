// ═══════════════════════════════════════════════════════
// LEGACY — لا تستخدمه في كود جديد
// البديل: Core/Validation/Validators/AccountValidator.cs
// يُحذف في: المرحلة G (التنظيف)
// ═══════════════════════════════════════════════════════
using PrimeERP.Core;
using PrimeERP.Database;
using PrimeERP.Models;

namespace PrimeERP.Validators
{
    public static class AccountValidator
    {
        public static ValidationResult Validate(Account account, bool isEdit)
        {
            var result = new ValidationResult();

            Validator.Required(result, "Name", account.Name, "اسم الحساب");
            Validator.MaxLength(result, "Name", account.Name, 150, "اسم الحساب");

            if (!isEdit)
            {
                var exists = AccountDb.GetByCode(account.Code) != null;
                Validator.Unique(result, "Code", exists, "كود الحساب");
            }

            return result;
        }
    }
}
