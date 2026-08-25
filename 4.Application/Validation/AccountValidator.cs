using PrimeERP.Domain.Rules;
using PrimeERP.Domain.Contracts;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    /// <summary>ينفّذ IValidator&lt;Account&gt; عبر RuleSet — فوق AccountRepository.</summary>
    public class AccountValidator : IValidator<Account>
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

        public ValidationResult Validate(Account account) =>
            Rules.For<Account>()
                .Required(x => x.Name, "اسم الحساب")
                .MaxLength(x => x.Name, 150, "اسم الحساب")
                .Custom((a, r) => AccountingRules.AccountCodeFormat(r, "Code", a.Code))
                .Unique(x => x.Code, a => !_isEdit && _checkUniqueness && _repo.GetByCode(a.Code) != null, "كود الحساب")
                .Validate(account);
    }
}
