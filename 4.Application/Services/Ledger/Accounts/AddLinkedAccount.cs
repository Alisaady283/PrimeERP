using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>حسابٌ في الشجرة ينشئ كيانه</summary>
    public sealed class AddLinkedAccount
    {
        private readonly AddTreeAccount _add;

        public AddLinkedAccount(AddTreeAccount add) => _add = add;

        public Result<Account> Run(PrimeDbContext db, Account parent, string name, IAccountLinkedService linked, string rootCode,
            bool leaf = true, string notes = null, bool isActive = true) =>
            _add.Run(db, parent, name, leaf, notes, isActive).Then(account =>
                linked == null
                    ? Result.Ok(account)
                    : linked.CreateFromAccount(db, account.Code, account.Name, rootCode).Then(() => Result.Ok(account)));
    }
}
