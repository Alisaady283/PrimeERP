using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Reporting
{
    public interface IPartyReportService
    {
        Result<ReportData> CustomerBalances(DateTime from, DateTime to);
        Result<ReportData> SupplierBalances(DateTime from, DateTime to);
        Result<ReportData> CustomerStatement(int customerId, DateTime from, DateTime to);
        Result<ReportData> SupplierStatement(int supplierId, DateTime from, DateTime to);
        Result<ReportData> AccountStatement(int accountId, DateTime from, DateTime to);
    }

    /// <summary>
    /// أرصدة الأطراف وكشوفها. العملاء والموردون يختلفان في طبيعة الحساب وتسمية الحركتين فقط — فالحساب
    /// واحد بمعامل لا نسختان. كان في 8.Modules، والرصيد بإشارة طبيعة الحساب منطقٌ محاسبي لا تخطيط شاشة.
    /// </summary>
    public class PartyReportService : IPartyReportService
    {
        private readonly IJournalService _journal;
        private readonly ICustomerService _customers;
        private readonly ISupplierService _suppliers;
        private readonly IAccountService _accounts;

        public PartyReportService(IJournalService journal, ICustomerService customers,
                                   ISupplierService suppliers, IAccountService accounts)
        {
            _journal = journal;
            _customers = customers;
            _suppliers = suppliers;
            _accounts = accounts;
        }

        public Result<ReportData> CustomerBalances(DateTime from, DateTime to)
        {
            var result = _customers.GetPaged(1, 5000);
            if (!result.IsSuccess) return Result.Fail<ReportData>(result.ErrorMessage);

            return Balances(result.Value.Items.Select(c => (c.Code, c.Name, c.AccountCode)).ToList(),
                from, to, debitIsCharge: true, "المبيعات", "المقبوضات");
        }

        public Result<ReportData> SupplierBalances(DateTime from, DateTime to)
        {
            var result = _suppliers.GetPaged(1, 5000);
            if (!result.IsSuccess) return Result.Fail<ReportData>(result.ErrorMessage);

            return Balances(result.Value.Items.Select(s => (s.Code, s.Name, s.AccountCode)).ToList(),
                from, to, debitIsCharge: false, "المشتريات", "المدفوعات");
        }

        private Result<ReportData> Balances(IReadOnlyList<(string Code, string Name, string AccountCode)> parties,
            DateTime from, DateTime to, bool debitIsCharge, string chargeLabel, string settleLabel)
        {
            var balance = _journal.GetTrialBalance(from, to, includeZero: true);
            if (!balance.IsSuccess) return Result.Fail<ReportData>(balance.ErrorMessage);

            var byAccount = balance.Value.ToDictionary(l => l.Code);

            var rows = new List<PartyBalanceRow>();
            foreach (var (code, name, accountCode) in parties)
            {
                if (string.IsNullOrWhiteSpace(accountCode) || !byAccount.TryGetValue(accountCode, out var line)) continue;

                // الرصيد بإشارة طبيعة الحساب: موجب يعني مديونية الطرف للعملاء، والتزاماً علينا للموردين.
                var sign = debitIsCharge ? 1 : -1;
                var row = new PartyBalanceRow
                {
                    Code = code, Name = name,
                    Opening = sign * (line.OpeningDebit - line.OpeningCredit),
                    Charged = debitIsCharge ? line.PeriodDebit : line.PeriodCredit,
                    Settled = debitIsCharge ? line.PeriodCredit : line.PeriodDebit,
                    Closing = sign * (line.ClosingDebit - line.ClosingCredit),
                };

                if (row.Opening != 0 || row.Charged != 0 || row.Settled != 0 || row.Closing != 0) rows.Add(row);
            }

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new()
                {
                    ["Opening"] = $"أول المدة: {rows.Sum(r => r.Opening):N2}",
                    ["Charged"] = $"{chargeLabel}: {rows.Sum(r => r.Charged):N2}",
                    ["Settled"] = $"{settleLabel}: {rows.Sum(r => r.Settled):N2}",
                    ["Closing"] = $"آخر المدة: {rows.Sum(r => r.Closing):N2}",
                }
            });
        }

        public Result<ReportData> CustomerStatement(int customerId, DateTime from, DateTime to) =>
            Statement(customerId, from, to, () => _customers.GetStatement(customerId, from, to));

        public Result<ReportData> SupplierStatement(int supplierId, DateTime from, DateTime to) =>
            Statement(supplierId, from, to, () => _suppliers.GetStatement(supplierId, from, to));

        public Result<ReportData> AccountStatement(int accountId, DateTime from, DateTime to)
        {
            if (accountId == 0) return Result.Fail<ReportData>("اختر حساباً");

            var account = _accounts.GetById(accountId);
            if (!account.IsSuccess) return Result.Fail<ReportData>(account.ErrorMessage);

            return Statement(accountId, from, to, () => _accounts.GetStatement(account.Value.Code, from, to));
        }

        private static Result<ReportData> Statement(int id, DateTime from, DateTime to,
            Func<Result<List<AccountStatementLine>>> getStatement)
        {
            if (id == 0) return Result.Fail<ReportData>("اختر عميلاً أو مورداً");

            var result = getStatement();
            if (!result.IsSuccess) return Result.Fail<ReportData>(result.ErrorMessage);

            var rows = result.Value.Select(l => new StatementRow
            { Date = l.Date, EntryNo = l.EntryNo, Description = l.Description, Debit = l.Debit, Credit = l.Credit, RunningBalance = l.RunningBalance }).ToList();

            // رصيد آخر المدة سطراً ختامياً — الكشف بلا خلاصة يُجبر القارئ على تتبّع آخر رصيد جارٍ بعينه.
            if (rows.Count > 0)
                rows.Add(new StatementRow
                {
                    Date = to.ToString("yyyy-MM-dd"),
                    Description = "رصيد آخر المدة",
                    RunningBalance = rows[^1].RunningBalance
                });

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new()
                {
                    ["Debit"]   = $"إجمالي المدين: {rows.Sum(r => r.Debit):N2}",
                    ["Credit"]  = $"إجمالي الدائن: {rows.Sum(r => r.Credit):N2}",
                    ["Closing"] = $"الرصيد: {(rows.Count > 0 ? rows[^1].RunningBalance : 0):N2}",
                }
            });
        }
    }
}
