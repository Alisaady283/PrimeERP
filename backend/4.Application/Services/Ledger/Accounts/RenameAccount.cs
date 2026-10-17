using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>تسمية الحساب</summary>
    public sealed class RenameAccount
    {
        private readonly IAccountRepository _accounts;

        public RenameAccount(IAccountRepository accounts) => _accounts = accounts;

        public void Run(PrimeDbContext db, string code, string name) => _accounts.UpdateName(db, code, name);

        /// <summary>حسابات الكيان التي تغيّر اسمها</summary>
        public void Run<T>(PrimeDbContext db, T entity, IReadOnlyList<AccountSpec<T>> specs, IReadOnlyList<string> before)
        {
            for (var i = 0; i < specs.Count; i++)
            {
                var code = specs[i].Get(entity);
                var name = specs[i].Name(entity);
                if (!string.IsNullOrWhiteSpace(code) && name != before[i]) Run(db, code, name);
            }
        }
    }
}
