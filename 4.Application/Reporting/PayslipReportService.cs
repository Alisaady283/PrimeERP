using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Services.HR;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Reporting
{
    /// <summary>قسيمة راتب موظف</summary>
    public interface IPayslipReportService
    {
        Result<ReportData> Payslip(int employeeId, DateTime from, DateTime to);
    }

    public class PayslipReportService : ReportServiceBase, IPayslipReportService
    {
        private readonly IPayrollRepository _payrolls;
        private readonly IEmployeeRepository _employees;
        private readonly ILookupRepository<Department> _departments;
        private readonly ILookupRepository<JobTitle> _jobTitles;

        public PayslipReportService(IPayrollRepository payrolls, IEmployeeRepository employees,
            ILookupRepository<Department> departments, ILookupRepository<JobTitle> jobTitles, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
        : base(permissions, settings, localization, audit)
        {
            _payrolls = payrolls; _employees = employees; _departments = departments; _jobTitles = jobTitles;
        }

        public Result<ReportData> Payslip(int employeeId, DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;

            if (employeeId == 0) return Result.Fail<ReportData>("اختر موظفاً");

            var employee = _employees.GetById(employeeId);
            if (employee == null) return Result.Fail<ReportData>("الموظف غير موجود");

            var payrolls = _payrolls.GetPaged(1, 500, null, "PaymentDate", false).Items
                .Where(p => p.IsPosted && p.PaymentDate >= from && p.PaymentDate <= to)
                .ToList();

            var lines = payrolls
                .SelectMany(p => _payrolls.GetLines(p.Id).Where(l => l.EmployeeId == employeeId)
                    .Select(l => (Payroll: p, Line: l)))
                .ToList();

            if (lines.Count == 0)
                return Result.Fail<ReportData>("لا مسير مُرحَّل لهذا الموظف في الفترة المختارة");

            var rows = lines.Select(x => new PayslipRow
            {
                PayrollNo = x.Payroll.PayrollNo,
                PaymentDate = x.Payroll.PaymentDate,
                Period = $"{x.Payroll.PeriodStart:yyyy-MM-dd} — {x.Payroll.PeriodEnd:yyyy-MM-dd}",

                BasicSalary = x.Line.BasicSalary,
                Allowances = x.Line.Allowances,
                Overtime = x.Line.Overtime,
                Gross = x.Line.GrossPay,

                Deductions = x.Line.Deductions,
                Advances = x.Line.Advances,
                Insurance = x.Line.Insurance,
                Tax = x.Line.Tax,
                Withheld = x.Line.TotalWithheld,

                NetSalary = x.Line.NetSalary,
            }).ToList();

            var department = employee.DepartmentId != null ? _departments.GetById(employee.DepartmentId.Value)?.Name : null;
            var jobTitle = employee.JobTitleId != null ? _jobTitles.GetById(employee.JobTitleId.Value)?.Name : null;

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new()
                {
                    ["Employee"] = $"{employee.Name} ({employee.Code})",
                    ["Job"] = string.Join(" — ", new[] { department, jobTitle }.Where(v => !string.IsNullOrWhiteSpace(v))),
                    ["Gross"] = $"الاستحقاقات: {rows.Sum(r => r.Gross):N2}",
                    ["Withheld"] = $"الاستقطاعات: {rows.Sum(r => r.Withheld):N2}",
                    ["Net"] = $"صافي الراتب: {rows.Sum(r => r.NetSalary):N2}",
                }
            });
        }
    }
}
