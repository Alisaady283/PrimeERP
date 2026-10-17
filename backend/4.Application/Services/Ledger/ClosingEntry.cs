using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>قيد إقفال السنة</summary>
    public sealed class ClosingEntry
    {
        private readonly IAccountRepository _accounts;
        private readonly IJournalRepository _journal;

        public ClosingEntry(IAccountRepository accounts, IJournalRepository journal)
        {
            _accounts = accounts;
            _journal = journal;
        }

        /// <summary>سطور الإقفال وصافي الربح</summary>
        public (List<CreateJournalLineDto> Lines, decimal NetIncome) Of(DateTime start, DateTime end, string retainedCode)
        {
            var nominal = _accounts.Find(null, null, (int)AccountType.Revenue, leafOnly: true, includeInactive: false)
                .Concat(_accounts.Find(null, null, (int)AccountType.Expense, leafOnly: true, includeInactive: false));

            var sums = _journal.GetAccountSums(start, end, postedOnly: true)
                               .ToDictionary(s => s.AccountCode, s => s.SumDebit - s.SumCredit);
            var lines = new JournalLines()
                .Close(nominal.Select(a => (a.Code, sums.GetValueOrDefault(a.Code))), retainedCode)
                .ToList();

            var retained = lines.Where(l => l.AccountCode == retainedCode).ToList();
            return (lines, retained.Sum(l => l.Credit) - retained.Sum(l => l.Debit));
        }
    }
}
