using PrimeERP.Application.Validation;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>تعديل حساب في الشجرة</summary>
    public sealed class EditTreeAccount
    {
        private readonly IAccountRepository _accounts;
        private readonly Guards _guards;

        public EditTreeAccount(IAccountRepository accounts, Guards guards)
        {
            _accounts = accounts;
            _guards = guards;
        }

        public Result Run(PrimeDbContext db, Account account, string name, string notes, bool isActive)
        {
            account.Name = name;
            account.Notes = notes;
            account.IsActive = isActive;
            account.IsLeaf = !_guards.HasChildren(account.Code, db);

            return Check.Valid(account, AddTreeAccount.AccountFields).Then(() =>
            {
                _accounts.Update(account, db);
                return Result.Ok();
            });
        }
    }
}
