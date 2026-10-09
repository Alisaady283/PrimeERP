using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.PageServices.Documents;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.PageServices.HR
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
        private readonly IEmployeeAdvanceRepository _advances;
        private readonly ILookupRepository<LeaveType> _leaveTypes;
        private readonly AccountOf _accountsOf;

        public PayrollService(IPayrollRepository payrolls, IEmployeeRepository employees, Entries journal,
            INumberSequenceService numbers,
            IEmployeeAllowanceRepository allowances, IEmployeeDeductionRepository deductions,
            IAttendanceRepository attendances, IEmployeeAdvanceRepository advances, ILookupRepository<LeaveType> leaveTypes, AccountOf accountsOf,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit, journals: journal)
        {
            _payrolls = payrolls; _employees = employees; _numbers = numbers;
            _allowances = allowances; _deductions = deductions; _attendances = attendances; _advances = advances; _leaveTypes = leaveTypes;
            _accountsOf = accountsOf;
        }

        protected override string PermissionPrefix => "HR";
        protected override string StringPrefix => "Str.Payroll";
        protected override string EntityName => "Payrolls";

        public static readonly Field<PayrollLine>[] LineFields =
        {
            new(x => x.NetSalary, "", Must: l => l.NetSalary >= 0, Message: "Str.Payroll.DeductionsExceed", Args: l => new object[] { l.EmployeeName }),
        };

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

        public Result<CreatePayrollDto> Open()
        {
            var next = PayrollCalc.NextPayroll(_payrolls.LastPeriodStart(),
                _employees.FirstHireDate() ?? Setting(SettingKeys.Company.StartDate, new DateTime(2026, 1, 1)));
            return Open(next.Month, next.Year);
        }

        public Result<CreatePayrollDto> Open(int month, int year)
        {
            if (!Can("PaySalary")) return FailDenied<CreatePayrollDto>();

            var (from, to) = PayrollCalc.Period(year, month, PayrollSettings.Rules(Settings).StartDay);
            if (_payrolls.HasPeriod(from)) return Fail<CreatePayrollDto>("MonthExists", ErrorCode.ValidationFailed, $"{month:00}/{year}");

            return Result.Ok(new CreatePayrollDto { Month = month, Year = year, PeriodStart = from, PeriodEnd = to, Lines = GenerateLines(from, to) });
        }

        protected override Result<Func<PrimeDbContext, int>> Plan(CreatePayrollDto dto)
        {
            (dto.PeriodStart, dto.PeriodEnd) = PayrollCalc.Period(dto.Year, dto.Month, PayrollSettings.Rules(Settings).StartDay);
            if (_payrolls.HasPeriod(dto.PeriodStart))
                return Fail<Func<PrimeDbContext, int>>("MonthExists", ErrorCode.ValidationFailed, $"{dto.Month:00}/{dto.Year}");

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
                return Check.Valid(line, LineFields).Then(() => Result.Ok(line));
            });
            if (built.IsFailure) return built.As<Func<PrimeDbContext, int>>();
            var resolved = built.Value;

            var advanceAccounts = _employees.GetByIds(resolved.Where(l => l.Advances > 0).Select(l => l.EmployeeId))
                                            .ToDictionary(e => e.Id, e => e.AccountCode);
            var journalLines = EntryLines(resolved, advanceAccounts);
            if (journalLines.IsFailure) return journalLines.As<Func<PrimeDbContext, int>>();

            var paid = resolved.Select(l => l.EmployeeId).ToHashSet();
            var (allowances, deductions, advances) = Pending(dto.PeriodStart);
            List<int> Taken<T>(Dictionary<int, List<T>> pending) where T : EmployeeMovement =>
                pending.Where(p => paid.Contains(p.Key)).SelectMany(p => p.Value).Select(m => m.Id).ToList();

            var totals = PayrollCalc.Totals(resolved);
            return Result.Ok<Func<PrimeDbContext, int>>(db =>
            {
                var payrollNo = _numbers.Next(db, "Payroll");
                var id = _payrolls.InsertHeader(db, Rows.Copy(dto, new Payroll(), row =>
                {
                    row.PayrollNo = payrollNo;
                    row.TotalBasic = totals.Basic;
                    row.TotalAllowances = totals.Allowances;
                    row.TotalDeductions = totals.Withheld;
                    row.NetTotal = totals.Net;
                    row.IsPosted = true;
                }));

                foreach (var line in resolved)
                    _payrolls.InsertLine(db, id, line);

                _allowances.Settle(db, Taken(allowances), id);
                _deductions.Settle(db, Taken(deductions), id);
                _advances.Settle(db, Taken(advances), id);

                _payrolls.SetJournalEntryId(db, id, Posting.Entry(Journals, db, dto.PaymentDate,
                    Msg("EntryDescription", payrollNo), nameof(JournalSource.Payroll), journalLines.Value));
                return id;
            });
        }

        protected override void Remove(PrimeDbContext db, Payroll head)
        {
            Posting.Reverse(Journals, db, head.JournalEntryId);
            _allowances.Release(db, head.Id);
            _deductions.Release(db, head.Id);
            _advances.Release(db, head.Id);
            _payrolls.DeleteDocument(db, head.Id);
        }

        private (Dictionary<int, List<EmployeeAllowance>>, Dictionary<int, List<EmployeeDeduction>>, Dictionary<int, List<EmployeeAdvance>>) Pending(DateTime start) =>
            (_allowances.PendingByEmployee(start.Year, start.Month), _deductions.PendingByEmployee(start.Year, start.Month),
             _advances.PendingByEmployee(start.Year, start.Month));

        /// <summary>سطور قيد الاستحقاق</summary>
        private Result<List<CreateJournalLineDto>> EntryLines(List<PayrollLine> lines, IReadOnlyDictionary<int, string> advanceAccounts)
        {
            Result<string> Account(string key) => _accountsOf.Setting(key, "Str.Payroll.AccountsMissing");
            var treasury = Setting(SettingKeys.HR.PayrollTreasury, 0);
            var salaryExpense = Account(SettingKeys.Accounts.SalaryExpense);
            var allowanceExpense = Account(SettingKeys.Accounts.AllowanceExpense);
            var salariesPayable = !Setting(SettingKeys.Edition.PayrollPays, true) ? Account(SettingKeys.Accounts.SalariesPayable)
                : treasury > 0 ? _accountsOf.Treasury(treasury, "Str.Payroll.TreasuryMissing")
                : Fail<string>("TreasuryMissing", ErrorCode.ValidationFailed);
            var insurancePayable = Account(SettingKeys.Accounts.InsurancePayable);
            var taxPayable = Account(SettingKeys.Accounts.TaxPayable);
            var accounts = Result.Combine(salaryExpense, allowanceExpense, salariesPayable, insurancePayable, taxPayable);
            if (accounts.IsFailure) return accounts.As<List<CreateJournalLineDto>>();

            return PayrollEntry.Lines(lines, advanceAccounts,
                (salaryExpense.Value, allowanceExpense.Value, salariesPayable.Value, insurancePayable.Value, taxPayable.Value));
        }

        private List<CreatePayrollLineDto> GenerateLines(DateTime from, DateTime to)
        {
            var employees = _employees.GetAll(activeOnly: true);
            if (employees.Count == 0) return new List<CreatePayrollLineDto>();

            var (allowances, deductions, advances) = Pending(from);
            var days = _attendances.DaysByEmployee(from, to);
            var leaveTypes = _leaveTypes.GetAll(includeInactive: true);
            var rules = PayrollSettings.Rules(Settings);

            return employees.Select((e, i) => Rows.Copy(
                PayrollCalc.Line(e, allowances.GetValueOrDefault(e.Id) ?? [], deductions.GetValueOrDefault(e.Id) ?? [],
                    advances.GetValueOrDefault(e.Id) ?? [], days.GetValueOrDefault(e.Id) ?? [], leaveTypes, rules, from, to),
                new CreatePayrollLineDto(), row =>
                {
                    row.LineNo = i + 1;
                    row.EmployeeCode = e.Code;
                })).ToList();
        }


        private static T ToDto<T>(Payroll p) where T : PayrollDto, new()
        {
            var (variant, status) = Rows.State((p.IsPosted, Domain.Results.StatusVariant.Success, "Str.Payroll.Posted"),
                (true, Domain.Results.StatusVariant.Warning, "Str.Payroll.Draft"));
            return Rows.Copy<T>(p, new(), row =>
            {
                (row.Month, row.Year) = (p.PeriodStart.Month, p.PeriodStart.Year);
                row.StatusText = status;
                row.StatusVariant = variant;
            });
        }
    }
}
