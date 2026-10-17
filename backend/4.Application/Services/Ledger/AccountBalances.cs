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
        private readonly IPartyRepository<Customer> _customers;
        private readonly IPartyRepository<Supplier> _suppliers;

        public AccountBalances(IAccountRepository accounts, IJournalRepository journal,
            IPartyRepository<Customer> customers, IPartyRepository<Supplier> suppliers)
        {
            _accounts = accounts;
            _journal = journal;
            _customers = customers;
            _suppliers = suppliers;
        }

        public decimal Refresh(PrimeDbContext db, string code)
        {
            var balance = _journal.SumPosted(code, null, null, db);
            Write(db, code, balance);
            return balance;
        }

        /// <summary>الشجرة كلها، والأب من أبنائه</summary>
        public void RefreshAll(PrimeDbContext db, IReadOnlyCollection<Account> accounts)
        {
            var sums = _journal.GetAccountSums(null, null, postedOnly: true, db)
                               .ToDictionary(s => s.AccountCode, s => s.SumDebit - s.SumCredit);
            var balances = StatementCalc.Rollup(accounts, a => a.Code, a => a.ParentCode,
                a => a.IsLeaf ? sums.GetValueOrDefault(a.Code) : 0m);
            foreach (var pair in balances) Write(db, pair.Key, pair.Value);
        }

        /// <summary>رصيد الحساب وطرفه</summary>
        private void Write(PrimeDbContext db, string code, decimal balance)
        {
            _accounts.UpdateBalance(code, balance, db);
            _customers.SetBalanceByAccount(code, balance, db);
            _suppliers.SetBalanceByAccount(code, balance, db);
        }
    }
}
