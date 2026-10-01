using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>قيد استحقاق الرواتب</summary>
    public static class PayrollEntry
    {
        public static Result<List<CreateJournalLineDto>> Lines(List<PayrollLine> lines, IReadOnlyDictionary<int, string> advanceAccounts,
            (string SalaryExpense, string AllowanceExpense, string SalariesPayable, string InsurancePayable, string TaxPayable) accounts)
        {
            var totals = PayrollCalc.Totals(lines);
            var entry = new JournalLines()
                .Debit(accounts.SalaryExpense, totals.Basic)
                .Debit(accounts.AllowanceExpense, totals.Allowances)
                .Credit(accounts.SalariesPayable, totals.Net)
                .Credit(accounts.InsurancePayable, totals.Insurance)
                .Credit(accounts.TaxPayable, totals.Tax);

            foreach (var line in lines.Where(l => l.Advances > 0))
            {
                var account = AccountOf.Required(advanceAccounts.GetValueOrDefault(line.EmployeeId), "Str.Payroll.AdvanceAccountMissing", line.EmployeeName);
                if (account.IsFailure) return account.As<List<CreateJournalLineDto>>();

                entry.Credit(account.Value, line.Advances);
            }

            return Result.Ok(entry.Credit(accounts.SalaryExpense, totals.Deductions).ToList());
        }
    }
}
