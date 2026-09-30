using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>حذف الحساب وإعادة أبيه ورقياً</summary>
    public sealed class CloseAccount
    {
        private readonly IAccountRepository _accounts;
        private readonly Guards _guards;

        public CloseAccount(IAccountRepository accounts, Guards guards)
        {
            _accounts = accounts;
            _guards = guards;
        }

        public void Run(PrimeDbContext db, string code)
        {
            var parentCode = _accounts.GetByCode(code, db)?.ParentCode;
            _accounts.Delete(code, db);

            if (!string.IsNullOrWhiteSpace(parentCode) && !_guards.HasChildren(parentCode, db))
                _accounts.SetIsLeaf(parentCode, true, db);
        }

        public void Run<T>(PrimeDbContext db, T entity, IEnumerable<AccountSpec<T>> specs)
        {
            foreach (var code in AddEntityAccount.Codes(entity, specs)) Run(db, code);
        }
    }
}
