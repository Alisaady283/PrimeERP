using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>أرصدة الحسابات من قيودها</summary>
    public sealed class AccountBalances
    {
        private readonly IAccountRepository _accounts;
        private readonly IJournalRepository _journal;

        public AccountBalances(IAccountRepository accounts, IJournalRepository journal)
        {
            _accounts = accounts;
            _journal = journal;
        }

        public decimal Refresh(PrimeDbContext db, string code)
        {
            var balance = _journal.SumPosted(code, null, null, db);
            _accounts.UpdateBalance(code, balance, db);
            return balance;
        }

        /// <summary>الشجرة كلها، والأب من أبنائه</summary>
        public void RefreshAll(PrimeDbContext db, IReadOnlyCollection<Account> accounts)
        {
            var sums = _journal.GetAccountSums(null, null, postedOnly: true, db)
                               .ToDictionary(s => s.AccountCode, s => s.SumDebit - s.SumCredit);
            var balances = StatementCalc.Rollup(accounts, a => a.Code, a => a.ParentCode,
                a => a.IsLeaf ? sums.GetValueOrDefault(a.Code) : 0m);
            foreach (var pair in balances) _accounts.UpdateBalance(pair.Key, pair.Value, db);
        }
    }
}
