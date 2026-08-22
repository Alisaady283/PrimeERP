using PrimeERP.Domain.Rules;
using PrimeERP.Domain.Contracts;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    /// <summary>ينفّذ IValidator&lt;Account&gt; عبر ValidatorBase — فوق AccountRepository الجديد.</summary>
    public class AccountValidator : ValidatorBase, IValidator<Account>
    {
        private readonly IAccountRepository _repo;
        private readonly bool _isEdit;
        private readonly bool _checkUniqueness;

        /// <summary>checkUniqueness=false يتخطّى قراءة IAccountRepository.GetByCode — للاستدعاء من داخل معاملة (conn,tx) قائمة (AccountService.Create(conn,tx,...)) حيث اتصال منفصل يُعلِّق (deadlock)، ولا حاجة فعلية له أصلاً: الكود مولَّد برمجياً (GenerateChildCodeInternal) لا يمكن أن يتصادم بحكم طريقة توليده.</summary>
        public AccountValidator(IAccountRepository repo, bool isEdit = false, bool checkUniqueness = true)
        {
            _repo = repo;
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
                var exists = _repo.GetByCode(account.Code) != null;
                Unique(result, "Code", exists, "كود الحساب");
            }

            return result;
        }
    }
}
