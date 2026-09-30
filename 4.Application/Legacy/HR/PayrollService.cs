using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Legacy.Documents;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Legacy.HR
{
    /// <summary>مسير الرواتب</summary>
    public class PayrollService
        : DocumentService<Payroll, PayrollDto, PayrollDetailDto, CreatePayrollDto, PayrollFilter>, IPayrollService
    {
        private readonly IPayrollRepository _payrolls;
        private readonly IEmployeeRepository _employees;
        private readonly INumberSequenceService _numbers;
        private readonly IEmployeeAllowanceRepository _allowances;
        private readonly IEmployeeDeductionRepository _deductions;
        private readonly IAttendanceRepository _attendances;
        private readonly IAccountRepository _accountRows;

        public PayrollService(IPayrollRepository payrolls, IEmployeeRepository employees, Entries journal,
            INumberSequenceService numbers,
            IEmployeeAllowanceRepository allowances, IEmployeeDeductionRepository deductions,
            IAttendanceRepository attendances, IAccountRepository accountRows,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit, journals: journal)
        {
            _payrolls = payrolls; _employees = employees; _numbers = numbers;
            _allowances = allowances; _deductions = deductions; _attendances = attendances; _accountRows = accountRows;
        }

        protected override string PermissionPrefix => "HR";
        protected override string StringPrefix => "Str.Payroll";
        protected override string EntityName => "Payrolls";

        protected override bool CanDo(string action) => Can(action == "Create" ? "PaySalary" : action);

        protected override Payroll FindHead(int id) => _payrolls.GetById(id);
        protected override int IdOf(Payroll head) => head.Id;
        protected override int? EntryOf(Payroll head) => head.JournalEntryId;
        protected override object AuditValue(Payroll head) => new { head?.NetTotal };

        protected override (List<Payroll> Items, int Total) FindPage(int page, int pageSize, PayrollFilter filter)
        {
            filter ??= new PayrollFilter();
            return _payrolls.GetPaged(page, pageSize, filter.SearchText, filter.SortBy, filter.SortDescending);
        }

        protected override List<PayrollDto> ToRows(List<Payroll> heads) => heads.Select(ToDto<PayrollDto>).ToList();

        protected override PayrollDetailDto ToDetail(Payroll head)
        {
            var detail = ToDto<PayrollDetailDto>(head);
            detail.Lines = _payrolls.GetLines(head.Id).Select(l => Rows.Copy(l, new PayrollLineDto())).ToList();
            return detail;
        }

        protected override Result<Func<PrimeDbContext, int>> Plan(CreatePayrollDto dto)
        {
            var lines = dto.Lines is { Count: > 0 } ? dto.Lines : GenerateLines(dto.PeriodStart, dto.PeriodEnd);
            if (lines.Count == 0) return Result.Fail<Func<PrimeDbContext, int>>(Msg("NoActiveEmployees"), ErrorCode.ValidationFailed);

            var built = ByCode.Resolve(codes => _employees.ByCodes(codes), lines, l => l.EmployeeCode, "Str.Employee.CodeNotFound", (l, employee, _) =>
            {
                var line = Rows.Copy(l, new PayrollLine(), row =>
                {
                    row.EmployeeId = employee.Id;
                    row.EmployeeName = employee.Name;
                });
                line.NetSalary = PayrollCalc.Net(line);
                return line.NetSalary < 0
                    ? Result.Fail<PayrollLine>(Msg("DeductionsExceed", employee.Name), ErrorCode.ValidationFailed)
                    : Result.Ok(line);
            });
            if (built.IsFailure) return built.As<Func<PrimeDbContext, int>>();
            var resolved = built.Value;

            var totals = PayrollCalc.Totals(resolved);
            return Result.Ok<Func<PrimeDbContext, int>>(db =>
            {
                var id = _payrolls.InsertHeader(db, Rows.Copy(dto, new Payroll(), row =>
                {
                    row.PayrollNo = _numbers.Next(db, "Payroll");
                    row.TotalBasic = totals.Basic;
                    row.TotalAllowances = totals.Allowances;
                    row.TotalDeductions = totals.Withheld;
                    row.NetTotal = totals.Net;
                }));

                foreach (var line in resolved)
                    _payrolls.InsertLine(db, id, line);

                return id;
            });
        }

        protected override void Remove(PrimeDbContext db, Payroll head)
        {
            Posting.Reverse(Journals, db, head.JournalEntryId);
            _payrolls.DeleteDocument(db, head.Id);
        }

        public Result Post(int id)
        {
            if (!Can("PaySalary")) return FailDenied();

            var payroll = _payrolls.GetById(id);
            if (payroll == null) return Fail("NotFound", ErrorCode.NotFound);
            var lines = _payrolls.GetLines(id);
            if (payroll.IsPosted) return Result.Fail(Msg("AlreadyPosted"), ErrorCode.ValidationFailed);
            if (lines.Count == 0) return Result.Fail(Msg("NoLines"), ErrorCode.ValidationFailed);

            var advances = _employees.GetByIds(lines.Where(l => l.Advances > 0).Select(l => l.EmployeeId))
                                     .ToDictionary(e => e.Id, e => e.AccountCode);
            var journalLines = AccrualLines(Settings, lines, advances);
            if (journalLines.IsFailure) return journalLines;

            var posted = Commit(db =>
            {
                _payrolls.SetJournalEntryId(db, id, Posting.Entry(Journals, db, payroll.PaymentDate,
                    Msg("EntryDescription", payroll.PayrollNo), nameof(JournalSource.Payroll), journalLines.Value));
                _payrolls.SetPosted(db, id, true);
                return Result.Ok();
            });
            if (posted.IsFailure) return posted;

            Audit.Log(EntityName, id, AuditAction.Update, details: Localization.Get("Str.Action.Post"));
            return Result.Ok();
        }

        public Result Unpost(int id)
        {
            if (!Can("PaySalary")) return FailDenied();

            var payroll = _payrolls.GetById(id);
            if (payroll == null) return Fail("NotFound", ErrorCode.NotFound);
            if (!payroll.IsPosted) return Result.Fail(Msg("NotPosted"), ErrorCode.ValidationFailed);

            var unposted = Posting.EnsureReversible(Journals, payroll.JournalEntryId).Then(() => Commit(db =>
            {
                Posting.Reverse(Journals, db, payroll.JournalEntryId);
                _payrolls.SetJournalEntryId(db, id, null);
                _payrolls.SetPosted(db, id, false);
                return Result.Ok();
            }));
            if (unposted.IsFailure) return unposted;

            Audit.Log(EntityName, id, AuditAction.Update, details: Localization.Get("Str.Action.Unpost"));
            return Result.Ok();
        }

        /// <summary>سطور قيد الاستحقاق</summary>
        private static Result<List<CreateJournalLineDto>> AccrualLines(ISettingsProvider settings, List<PayrollLine> lines,
            IReadOnlyDictionary<int, string> advanceAccounts)
        {
            var salaryExpense = settings.Get(SettingKeys.Accounts.SalaryExpense, "");
            var allowanceExpense = settings.Get(SettingKeys.Accounts.AllowanceExpense, "");
            var salariesPayable = settings.Get(SettingKeys.Accounts.SalariesPayable, "");
            var insurancePayable = settings.Get(SettingKeys.Accounts.InsurancePayable, "");
            var taxPayable = settings.Get(SettingKeys.Accounts.TaxPayable, "");

            if (new[] { salaryExpense, allowanceExpense, salariesPayable, insurancePayable, taxPayable }.Any(string.IsNullOrWhiteSpace))
                return Result.Fail<List<CreateJournalLineDto>>(LocalizationService.Get("Str.Payroll.AccountsMissing"), ErrorCode.ValidationFailed);

            var totals = PayrollCalc.Totals(lines);
            var entry = new JournalLines()
                .Debit(salaryExpense, totals.Basic)
                .Debit(allowanceExpense, totals.Allowances)
                .Credit(salariesPayable, totals.Net)
                .Credit(insurancePayable, totals.Insurance)
                .Credit(taxPayable, totals.Tax);

            foreach (var line in lines.Where(l => l.Advances > 0))
            {
                var account = advanceAccounts.GetValueOrDefault(line.EmployeeId);
                if (string.IsNullOrWhiteSpace(account))
                    return Result.Fail<List<CreateJournalLineDto>>(
                        LocalizationService.Get("Str.Payroll.AdvanceAccountMissing", line.EmployeeName), ErrorCode.ValidationFailed);

                entry.Credit(account, line.Advances);
            }

            return Result.Ok(entry.Credit(salaryExpense, totals.Deductions).ToList());
        }

        private List<CreatePayrollLineDto> GenerateLines(DateTime from, DateTime to)
        {
            var employees = _employees.GetAll(activeOnly: true);
            if (employees.Count == 0) return new List<CreatePayrollLineDto>();

            var advanceBalances = _accountRows.GetByCodes(employees.Select(e => e.AccountCode).Where(c => !string.IsNullOrWhiteSpace(c)))
                                              .ToDictionary(a => a.Code, a => a.Balance);

            var allowances = _allowances.SumByEmployee(to.Month, to.Year);
            var deductions = _deductions.SumByEmployee(to.Month, to.Year);
            var overtime = _attendances.OvertimeByEmployee(from, to);
            var absences = _attendances.AbsenceDaysByEmployee(from, to);

            decimal Of(Dictionary<int, decimal> source, int id) => source.TryGetValue(id, out var value) ? value : 0;

            var lineNo = 1;
            return employees.Select(e =>
            {

                var absenceDays = absences.TryGetValue(e.Id, out var days) ? days : 0;

                var line = new CreatePayrollLineDto
                {
                    LineNo = lineNo++,
                    EmployeeCode = e.Code,
                    BasicSalary = e.BasicSalary,
                    Allowances = e.FixedAllowances + Of(allowances, e.Id),
                    Overtime = PayrollCalc.Overtime(Of(overtime, e.Id), e.BasicSalary),
                    Deductions = Of(deductions, e.Id) + PayrollCalc.Absence(absenceDays, e.BasicSalary),
                    Insurance = e.IsInsured ? e.InsuranceAmount : 0,
                    Tax = e.TaxAmount,
                    Advances = AdvanceInstalment(e, advanceBalances),
                };

                line.NetSalary = PayrollCalc.Net(line.BasicSalary, line.Allowances, line.Overtime, line.Deductions, line.Advances, line.Insurance, line.Tax);

                return line;
            }).ToList();
        }

        private static decimal AdvanceInstalment(Employee employee, IReadOnlyDictionary<string, decimal> balances) =>
            !string.IsNullOrWhiteSpace(employee.AccountCode) && balances.TryGetValue(employee.AccountCode, out var balance) && balance > 0
                ? balance
                : 0;


        private static T ToDto<T>(Payroll p) where T : PayrollDto, new()
        {
            var (variant, status) = Rows.State((p.IsPosted, Domain.Results.StatusVariant.Success, "Str.Payroll.Posted"),
                (true, Domain.Results.StatusVariant.Warning, "Str.Payroll.Draft"));
            return Rows.Copy<T>(p, new(), row =>
            {
                row.StatusText = status;
                row.StatusVariant = variant;
            });
        }
    }
}
