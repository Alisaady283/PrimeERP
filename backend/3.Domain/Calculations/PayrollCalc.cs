using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Domain.Calculations
{
    /// <summary>حساب الراتب</summary>
    public static class PayrollCalc
    {
        public static decimal Gross(decimal basic, decimal allowances, decimal overtime) => basic + allowances + overtime;

        public static decimal Withheld(decimal deductions, decimal advances, decimal insurance, decimal tax) =>
            deductions + advances + insurance + tax;

        public static decimal Gross(PayrollLine l) => Gross(l.BasicSalary, l.Allowances, l.Overtime);

        public static decimal Withheld(PayrollLine l) => Withheld(l.Deductions, l.Advances, l.Insurance, l.Tax);

        public static decimal Net(PayrollLine l) => Gross(l) - Withheld(l);

        public static (DateTime From, DateTime To) Period(int year, int month, int startDay)
        {
            var from = new DateTime(year, month, Math.Clamp(startDay, 1, 28));
            return (from, from.AddMonths(1).AddDays(-1));
        }

        public static (DateTime From, DateTime To) Cycle(DateTime date, int startDay)
        {
            var month = date.Day < Math.Clamp(startDay, 1, 28) ? date.AddMonths(-1) : date;
            return Period(month.Year, month.Month, startDay);
        }

        public static DateTime NextPayroll(DateTime? lastStart, DateTime opening) =>
            lastStart?.AddMonths(1) ?? new DateTime(opening.Year, opening.Month, 1);

        public static (TimeSpan Start, TimeSpan End) Shift(Employee employee, PayrollRules rules) =>
            (employee.WorkStart ?? rules.WorkStart, employee.WorkEnd ?? rules.WorkEnd);

        public static int LateMinutes(Attendance day, TimeSpan start) =>
            day.Status == AttendanceStatus.Present && day.CheckIn > start ? (int)(day.CheckIn.Value - start).TotalMinutes : 0;

        public static int OvertimeMinutes(Attendance day, TimeSpan end) =>
            day.Status == AttendanceStatus.Present && day.CheckOut > end ? (int)(day.CheckOut.Value - end).TotalMinutes : 0;

        public static decimal DailyRate(decimal basic, PayrollRules rules, DateTime from, DateTime to) =>
            basic / (rules.DailyWageDays > 0 ? rules.DailyWageDays : (to - from).Days + 1);

        public static decimal HourlyRate(decimal daily, (TimeSpan Start, TimeSpan End) shift)
        {
            var hours = (decimal)(shift.End - shift.Start).TotalHours;
            return hours > 0 ? daily / hours : 0;
        }

        public static decimal Minutes(IEnumerable<int> perDay, TimeSpan minimum, decimal hourly, decimal rate) =>
            Math.Round(perDay.Where(m => m > 0 && m >= minimum.TotalMinutes).Sum() / 60m * hourly * rate, 2);

        public static int UnpaidDays(IEnumerable<Attendance> days, IEnumerable<LeaveType> leaveTypes)
        {
            var unpaid = leaveTypes.Where(t => !t.IsPaid).Select(t => t.Id).ToHashSet();
            return days.Count(d => d.Status == AttendanceStatus.Absent
                                || d.Status == AttendanceStatus.Leave && d.LeaveTypeId is int type && unpaid.Contains(type));
        }

        /// <summary>سطر راتب الموظف للفترة</summary>
        public static PayrollLine Line(Employee employee, IEnumerable<EmployeeMovement> allowances, IEnumerable<EmployeeMovement> deductions,
            IEnumerable<EmployeeMovement> advances, IReadOnlyCollection<Attendance> days, IReadOnlyCollection<LeaveType> leaveTypes,
            PayrollRules rules, DateTime from, DateTime to)
        {
            var daily = DailyRate(employee.BasicSalary, rules, from, to);
            var hourly = HourlyRate(daily, Shift(employee, rules));
            var line = new PayrollLine
            {
                BasicSalary = employee.BasicSalary,
                Allowances = employee.FixedAllowances + allowances.Sum(m => m.Amount),
                Overtime = Minutes(days.Select(d => d.OvertimeMinutes), rules.OvertimeMinimum, hourly, rules.OvertimeRate),
                Deductions = deductions.Sum(m => m.Amount) + Math.Round(UnpaidDays(days, leaveTypes) * daily, 2)
                           + Minutes(days.Select(d => d.LateMinutes), rules.LateMinimum, hourly, rules.LateRate),
                Insurance = employee.IsInsured ? employee.InsuranceAmount : 0,
                Tax = employee.TaxAmount,
                Advances = advances.Sum(m => m.Amount),
            };
            line.NetSalary = Net(line);
            return line;
        }

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

    public readonly record struct PayrollRules(int DailyWageDays, decimal OvertimeRate, TimeSpan OvertimeMinimum,
        decimal LateRate, TimeSpan LateMinimum, TimeSpan WorkStart, TimeSpan WorkEnd, int StartDay);

    public readonly record struct PayrollTotals(
        decimal Basic, decimal Allowances, decimal Withheld, decimal Net, decimal Insurance, decimal Tax, decimal Deductions);
}
