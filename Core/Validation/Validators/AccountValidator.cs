using PrimeERP.Database;
using PrimeERP.Models;

namespace PrimeERP.Core.Validation.Validators
{
    /// <summary>ينفّذ IValidator&lt;Account&gt; عبر ValidatorBase — فوق AccountRepository الجديد.</summary>
    public class AccountValidator : ValidatorBase, IValidator<Account>
    {
        private readonly bool _isEdit;
        private readonly bool _checkUniqueness;

        /// <summary>checkUniqueness=false يتخطّى قراءة AccountRepository.GetByCode — للاستدعاء من داخل معاملة (conn,tx) قائمة (AccountService.Create(conn,tx,...)) حيث اتصال منفصل يُعلِّق (deadlock)، ولا حاجة فعلية له أصلاً: الكود مولَّد برمجياً (GenerateChildCodeInternal) لا يمكن أن يتصادم بحكم طريقة توليده.</summary>
        public AccountValidator(bool isEdit = false, bool checkUniqueness = true)
        {
            _isEdit = isEdit;
            _checkUniqueness = checkUniqueness;
        }

        public ValidationResult Validate(Account account)
        {
            var result = new ValidationResult();

            Required(result, "Name", account.Name, "اسم الحساب");
            MaxLength(result, "Name", account.Name, 150, "اسم الحساب");
            AccountingRules.AccountCodeFormat(result, "Code", account.Code);

            if (!_isEdit && _checkUniqueness)
            {
                var exists = AccountRepository.GetByCode(account.Code) != null;
                Unique(result, "Code", exists, "كود الحساب");
            }

            return result;
        }
    }
}
