using PrimeERP.Application.DTOs.Cheques;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Calculations;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>كشف الحساب برصيده الجاري</summary>
    public sealed class Statement
    {
        private readonly IJournalRepository _journal;

        public Statement(IJournalRepository journal) => _journal = journal;

        public List<AccountStatementLine> Of(string code, DateTime from, DateTime to)
        {
            var opening = _journal.SumPosted(code, null, from.AddDays(-1));
            var lines = _journal.GetPostedLinesForAccount(code, from, to);
            var running = StatementCalc.Running(opening, lines, l => l.Debit - l.Credit);

            var result = new List<AccountStatementLine>
            {
                new()
                {
                    Date = from.ToString("yyyy-MM-dd"), EntryNo = "", Description = LocalizationService.Get("Str.Accounts.OpeningRow"),
                    RunningBalance = opening, SourceType = "Opening"
                }
            };
            result.AddRange(lines.Select((l, i) => new AccountStatementLine
            {
                Date = l.EntryDate, EntryNo = l.EntryNo, Description = l.Description,
                Debit = l.Debit, Credit = l.Credit, RunningBalance = running[i], SourceType = "Journal"
            }));
            return result;
        }

        /// <summary>الشيكات المفتوحة سطوراً</summary>
        public static List<AccountStatementLine> WithCheques(List<AccountStatementLine> lines, IEnumerable<ChequeDto> cheques)
        {
            var running = lines.Count > 0 ? lines[^1].RunningBalance : 0m;
            lines.AddRange(cheques.Select(cheque => new AccountStatementLine
            {
                Date = cheque.DueDate.ToString("yyyy-MM-dd"), EntryNo = cheque.ChequeNo,
                Description = LocalizationService.Get("Str.Cheque.StatementLine", cheque.ChequeNo, cheque.BankName, cheque.StatusName),
                MemoAmount = cheque.Amount, RunningBalance = running, SourceType = "Cheque"
            }));
            return lines;
        }
    }
}
