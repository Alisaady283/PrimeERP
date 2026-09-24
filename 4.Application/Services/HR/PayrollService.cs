using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Services.HR
{
    /// <summary>مسير الرواتب</summary>
    public class PayrollService : ServiceBase, IPayrollService
    {
        private readonly IPayrollRepository _payrolls;
        private readonly IEmployeeRepository _employees;
        private readonly IJournalService _journal;
        private readonly INumberSequenceService _numbers;
        private readonly IEmployeeAllowanceRepository _allowances;
        private readonly IEmployeeDeductionRepository _deductions;
        private readonly IAttendanceRepository _attendances;
        private readonly Accounting.IAccountService _accounts;

        public PayrollService(IPayrollRepository payrolls, IEmployeeRepository employees, IJournalService journal,
            INumberSequenceService numbers,
            IEmployeeAllowanceRepository allowances, IEmployeeDeductionRepository deductions,
            IAttendanceRepository attendances, Accounting.IAccountService accounts,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _payrolls = payrolls; _employees = employees; _journal = journal; _numbers = numbers;
            _allowances = allowances; _deductions = deductions; _attendances = attendances; _accounts = accounts;
        }

        protected override string PermissionPrefix => "HR";
        protected override string StringPrefix => "Str.Payroll";
        protected override string EntityName => "Payrolls";


        public Result<PagedResult<PayrollDto>> GetPaged(int page, int pageSize, PayrollFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<PayrollDto>>();
            filter ??= new PayrollFilter();

            var (items, total) = _payrolls.GetPaged(page, pageSize, filter.SearchText, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<PayrollDto> { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<PayrollDetailDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<PayrollDetailDto>();

            var payroll = _payrolls.GetById(id);
            if (payroll == null) return Result.Fail<PayrollDetailDto>("مسير الرواتب غير موجود", ErrorCode.NotFound);

            var detail = ToDetail(ToDto(payroll));
            detail.Lines = _payrolls.GetLines(id).Select(l => new PayrollLineDto
            {
                EmployeeId = l.EmployeeId, EmployeeName = l.EmployeeName,
                BasicSalary = l.BasicSalary, Allowances = l.Allowances, Overtime = l.Overtime,
                Deductions = l.Deductions, Advances = l.Advances, Insurance = l.Insurance, Tax = l.Tax,
                NetSalary = l.NetSalary, Notes = l.Notes
            }).ToList();

            return Result.Ok(detail);
        }

        public Result<PayrollDetailDto> Create(CreatePayrollDto dto)
        {
            if (!Can("PaySalary")) return FailDenied<PayrollDetailDto>();

            var lines = dto.Lines is { Count: > 0 } ? dto.Lines : GenerateLines(dto.PeriodStart, dto.PeriodEnd);
            if (lines.Count == 0) return Result.Fail<PayrollDetailDto>("لا موظفين نشطين لتوليد المسير", ErrorCode.ValidationFailed);

            var resolvedLines = new List<PayrollLine>();
            foreach (var l in lines)
            {
                var employee = _employees.GetByCode(l.EmployeeCode);
                if (employee == null) return Result.Fail<PayrollDetailDto>($"الموظف بالكود {l.EmployeeCode} غير موجود", ErrorCode.ValidationFailed);

                var line = new PayrollLine
                {
                    EmployeeId = employee.Id, EmployeeName = employee.Name,
                    BasicSalary = l.BasicSalary, Allowances = l.Allowances, Overtime = l.Overtime,
                    Deductions = l.Deductions, Advances = l.Advances, Insurance = l.Insurance, Tax = l.Tax,
                    Notes = l.Notes
                };
                line.NetSalary = line.GrossPay - line.TotalWithheld;

                if (line.NetSalary < 0)
                    return Result.Fail<PayrollDetailDto>($"استقطاعات «{employee.Name}» تتجاوز استحقاقه", ErrorCode.ValidationFailed);

                resolvedLines.Add(line);
            }

            var totalBasic = resolvedLines.Sum(l => l.BasicSalary);
            var totalAllowances = resolvedLines.Sum(l => l.Allowances + l.Overtime);
            var totalDeductions = resolvedLines.Sum(l => l.TotalWithheld);
            var netTotal = resolvedLines.Sum(l => l.NetSalary);

            int payrollId;
            try
            {
                payrollId = Tx(db =>
                {
                    var payrollNo = _numbers.Next(db, "Payroll");
                    var payroll = new Payroll
                    {
                        PayrollNo = payrollNo, PeriodStart = dto.PeriodStart, PeriodEnd = dto.PeriodEnd, PaymentDate = dto.PaymentDate,
                        TotalBasic = totalBasic, TotalAllowances = totalAllowances, TotalDeductions = totalDeductions, NetTotal = netTotal,
                        Notes = dto.Notes, CreatedBy = AppSession.Username
                    };
                    var id = _payrolls.InsertHeader(db, payroll);

                    foreach (var line in resolvedLines)
                        _payrolls.InsertLine(db, id, line);

                    return id;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<PayrollDetailDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log("Payrolls", payrollId, AuditAction.Insert, newValue: new { NetTotal = netTotal, LineCount = resolvedLines.Count });
            return GetById(payrollId);
        }

        public Result Update(CreatePayrollDto dto) => Result.Fail("مسير الرواتب يُحذف ويُعاد إنشاؤه بدل تعديله", ErrorCode.ValidationFailed);

        public Result Post(int id)
        {
            if (!Can("PaySalary")) return FailDenied();

            var payroll = _payrolls.GetById(id);
            if (payroll == null) return Result.Fail("المسير غير موجود", ErrorCode.NotFound);
            if (payroll.IsPosted) return Result.Fail("المسير مُرحَّل بالفعل", ErrorCode.ValidationFailed);

            var lines = _payrolls.GetLines(id);
            if (lines.Count == 0) return Result.Fail("المسير بلا سطور", ErrorCode.ValidationFailed);

            var accounts = ResolveAccounts();
            if (accounts.IsFailure) return Result.Fail(accounts.ErrorMessage, accounts.ErrorCode);
            var (salaryExpense, allowanceExpense, salariesPayable, insurancePayable, taxPayable) = accounts.Value;

            var journalLines = new List<CreateJournalLineDto>();
            void Add(string code, decimal debit, decimal credit)
            {
                if (debit == 0 && credit == 0) return;
                journalLines.Add(new CreateJournalLineDto
                { LineNo = journalLines.Count + 1, AccountCode = code, Debit = debit, Credit = credit });
            }

            Add(salaryExpense,    lines.Sum(l => l.BasicSalary), 0);
            Add(allowanceExpense, lines.Sum(l => l.Allowances + l.Overtime), 0);

            Add(salariesPayable,  0, lines.Sum(l => l.NetSalary));
            Add(insurancePayable, 0, lines.Sum(l => l.Insurance));
            Add(taxPayable,       0, lines.Sum(l => l.Tax));

            foreach (var line in lines.Where(l => l.Advances > 0))
            {
                var employee = _employees.GetById(line.EmployeeId);
                if (string.IsNullOrWhiteSpace(employee?.AccountCode))
                    return Result.Fail($"لا حساب سلفة مرتبط بـ«{line.EmployeeName}» — لا يمكن قطع سلفته", ErrorCode.ValidationFailed);

                Add(employee.AccountCode, 0, line.Advances);
            }

            var otherDeductions = lines.Sum(l => l.Deductions);
            if (otherDeductions > 0) Add(salaryExpense, 0, otherDeductions);

            try
            {
                Tx(db =>
                {
                    var entry = _journal.Create(db, new CreateJournalDto
                    {
                        EntryDate = payroll.PaymentDate,
                        Description = $"استحقاق مسير رواتب {payroll.PayrollNo}",
                        Source = nameof(JournalSource.Payroll),
                        Lines = journalLines
                    });
                    if (!entry.IsSuccess) throw new InvalidOperationException(entry.ErrorMessage);

                    var posted = _journal.Post(db, entry.Value.Id);
                    if (!posted.IsSuccess) throw new InvalidOperationException(posted.ErrorMessage);

                    _payrolls.SetJournalEntryId(db, id, entry.Value.Id);
                    _payrolls.SetPosted(db, id, true);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log(EntityName, id, AuditAction.Update, details: "ترحيل");
            return Result.Ok();
        }

        public Result Unpost(int id)
        {
            if (!Can("PaySalary")) return FailDenied();

            var payroll = _payrolls.GetById(id);
            if (payroll == null) return Result.Fail("المسير غير موجود", ErrorCode.NotFound);
            if (!payroll.IsPosted) return Result.Fail("المسير غير مُرحَّل", ErrorCode.ValidationFailed);

            Tx(db =>
            {
                if (payroll.JournalEntryId != null) _journal.Delete(db, payroll.JournalEntryId.Value);
                _payrolls.SetJournalEntryId(db, id, null);
                _payrolls.SetPosted(db, id, false);
            });

            Audit.Log(EntityName, id, AuditAction.Update, details: "إلغاء ترحيل");
            return Result.Ok();
        }

        private Result<(string SalaryExpense, string AllowanceExpense, string SalariesPayable, string InsurancePayable, string TaxPayable)> ResolveAccounts()
        {
            var salaryExpense = Settings.Get(SettingKeys.Accounts.SalaryExpense, "");
            var allowanceExpense = Settings.Get(SettingKeys.Accounts.AllowanceExpense, "");
            var salariesPayable = Settings.Get(SettingKeys.Accounts.SalariesPayable, "");
            var insurancePayable = Settings.Get(SettingKeys.Accounts.InsurancePayable, "");
            var taxPayable = Settings.Get(SettingKeys.Accounts.TaxPayable, "");

            if (string.IsNullOrWhiteSpace(salaryExpense) || string.IsNullOrWhiteSpace(allowanceExpense) ||
                string.IsNullOrWhiteSpace(salariesPayable) || string.IsNullOrWhiteSpace(insurancePayable) ||
                string.IsNullOrWhiteSpace(taxPayable))
                return Result.Fail<(string, string, string, string, string)>(
                    "حسابات المسير غير مضبوطة في الإعدادات — اضبط مصروف الرواتب والبدلات والمستحقّات الثلاثة",
                    ErrorCode.ValidationFailed);

            return Result.Ok((salaryExpense, allowanceExpense, salariesPayable, insurancePayable, taxPayable));
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var payroll = _payrolls.GetById(id);
            if (payroll == null) return Result.Fail("المسير غير موجود", ErrorCode.NotFound);

            Tx(db =>
            {
                if (payroll.JournalEntryId != null) _journal.Delete(db, payroll.JournalEntryId.Value);
                _payrolls.DeleteDocument(db, id);
            });

            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private List<CreatePayrollLineDto> GenerateLines(DateTime from, DateTime to)
        {
            var employees = _employees.GetAll(activeOnly: true);
            if (employees.Count == 0) return new List<CreatePayrollLineDto>();

            var allowances = _allowances.SumByEmployee(to.Month, to.Year);
            var deductions = _deductions.SumByEmployee(to.Month, to.Year);
            var overtime = _attendances.OvertimeByEmployee(from, to);
            var absences = _attendances.AbsenceDaysByEmployee(from, to);

            decimal Of(Dictionary<int, decimal> source, int id) => source.TryGetValue(id, out var value) ? value : 0;

            var lineNo = 1;
            return employees.Select(e =>
            {
                var dailyRate = e.BasicSalary / 30m;
                var hourlyRate = dailyRate / 8m;

                var absenceDays = absences.TryGetValue(e.Id, out var days) ? days : 0;

                var line = new CreatePayrollLineDto
                {
                    LineNo = lineNo++,
                    EmployeeCode = e.Code,
                    BasicSalary = e.BasicSalary,
                    Allowances = e.FixedAllowances + Of(allowances, e.Id),
                    Overtime = Math.Round(Of(overtime, e.Id) * hourlyRate, 2),
                    Deductions = Of(deductions, e.Id) + Math.Round(absenceDays * dailyRate, 2),
                    Insurance = e.IsInsured ? e.InsuranceAmount : 0,
                    Tax = e.TaxAmount,
                    Advances = AdvanceInstalment(e),
                };

                line.NetSalary = line.BasicSalary + line.Allowances + line.Overtime
                               - (line.Deductions + line.Advances + line.Insurance + line.Tax);

                return line;
            }).ToList();
        }

        private decimal AdvanceInstalment(Employee employee)
        {
            if (string.IsNullOrWhiteSpace(employee.AccountCode)) return 0;

            var account = _accounts.GetByCode(employee.AccountCode);
            if (!account.IsSuccess || account.Value.Balance <= 0) return 0;

            return account.Value.Balance;
        }

        private static PayrollDetailDto ToDetail(PayrollDto source)
        {
            var detail = new PayrollDetailDto();
            foreach (var property in typeof(PayrollDto).GetProperties())
                if (property.CanWrite) property.SetValue(detail, property.GetValue(source));

            return detail;
        }

        private static PayrollDto ToDto(Payroll p) => new()
        {
            Id = p.Id, PayrollNo = p.PayrollNo, PeriodStart = p.PeriodStart, PeriodEnd = p.PeriodEnd, PaymentDate = p.PaymentDate,
            TotalBasic = p.TotalBasic, TotalAllowances = p.TotalAllowances, TotalDeductions = p.TotalDeductions, NetTotal = p.NetTotal,
            IsPosted = p.IsPosted,
            Notes = p.Notes,
            StatusText = p.IsPosted ? "مُرحَّل" : "مسوّدة",
            StatusVariant = p.IsPosted ? Domain.Results.StatusVariant.Success : Domain.Results.StatusVariant.Warning,
            CreatedAt = p.CreatedAt
        };
    }
}
