using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Domain.Calculations
{
    /// <summary>حساب الراتب</summary>
    public static class PayrollCalc
    {
        public static decimal Gross(decimal basic, decimal allowances, decimal overtime) => basic + allowances + overtime;

        public static decimal Withheld(decimal deductions, decimal advances, decimal insurance, decimal tax) =>
            deductions + advances + insurance + tax;

        public static decimal Net(decimal basic, decimal allowances, decimal overtime,
            decimal deductions, decimal advances, decimal insurance, decimal tax) =>
            Gross(basic, allowances, overtime) - Withheld(deductions, advances, insurance, tax);

        public static decimal Gross(PayrollLine l) => Gross(l.BasicSalary, l.Allowances, l.Overtime);

        public static decimal Withheld(PayrollLine l) => Withheld(l.Deductions, l.Advances, l.Insurance, l.Tax);

        public static decimal Net(PayrollLine l) => Gross(l) - Withheld(l);

        public static decimal DailyRate(decimal basic) => basic / 30m;

        public static decimal HourlyRate(decimal basic) => DailyRate(basic) / 8m;

        public static decimal Overtime(decimal hours, decimal basic) => Math.Round(hours * HourlyRate(basic), 2);

        public static decimal Absence(decimal days, decimal basic) => Math.Round(days * DailyRate(basic), 2);

        /// <summary>مجاميع المسير</summary>
        public static PayrollTotals Totals(IReadOnlyCollection<PayrollLine> lines) => new(
            lines.Sum(l => l.BasicSalary),
            lines.Sum(l => l.Allowances + l.Overtime),
            lines.Sum(Withheld),
            lines.Sum(l => l.NetSalary),
            lines.Sum(l => l.Insurance),
            lines.Sum(l => l.Tax),
            lines.Sum(l => l.Deductions));
    }

    public readonly record struct PayrollTotals(
        decimal Basic, decimal Allowances, decimal Withheld, decimal Net, decimal Insurance, decimal Tax, decimal Deductions);
}
