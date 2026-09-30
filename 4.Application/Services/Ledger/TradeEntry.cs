using PrimeERP.Domain.Calculations;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Domain.Helpers;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>قيد البيع والشراء وعكسهما</summary>
    public static class TradeEntry
    {
        /// <summary>partyDebit: بيعٌ أو مرتجع شراء</summary>
        public static List<CreateJournalLineDto> Lines(bool partyDebit, string party, string main, string vat, string withholding,
            LineAmounts totals, string cogs = null, string inventory = null, decimal cost = 0)
        {
            var entry = new JournalLines();
            Side(entry, partyDebit, party, totals.Net);
            Side(entry, !partyDebit, main, totals.Taxable);
            Side(entry, !partyDebit, vat, totals.Vat);
            Side(entry, partyDebit, withholding, totals.Withholding);
            Side(entry, partyDebit, cogs, cost);
            Side(entry, !partyDebit, inventory, cost);
            return entry.ToList();
        }

        private static void Side(JournalLines entry, bool debit, string account, decimal amount)
        {
            if (debit) entry.Debit(account, amount);
            else entry.Credit(account, amount);
        }
    }
}
