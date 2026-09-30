using System.Collections.Generic;
using PrimeERP.Application.DTOs.Accounting;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>سطور قيدٍ بطرفيها</summary>
    public sealed class JournalLines
    {
        private readonly List<CreateJournalLineDto> _lines = new();

        public JournalLines Debit(string account, decimal amount, string note = null) => Add(account, amount, 0, note);

        public JournalLines Credit(string account, decimal amount, string note = null) => Add(account, 0, amount, note);

        public JournalLines Add(string account, decimal debit, decimal credit, string note = null)
        {
            if (debit == 0 && credit == 0) return this;

            _lines.Add(new CreateJournalLineDto
            { LineNo = _lines.Count + 1, AccountCode = account, Debit = debit, Credit = credit, Notes = note });
            return this;
        }

        /// <summary>الصافي في جهته</summary>
        public JournalLines Signed(string account, decimal net, string note = null) =>
            net >= 0 ? Debit(account, net, note) : Credit(account, -net, note);

        /// <summary>تصفير الأرصدة في حساب</summary>
        public JournalLines Close(IEnumerable<(string Account, decimal Net)> balances, string into)
        {
            foreach (var (account, net) in balances) Signed(account, -net);
            return Signed(into, _lines.Sum(l => l.Credit) - _lines.Sum(l => l.Debit));
        }

        public List<CreateJournalLineDto> ToList() => _lines;
    }
}
