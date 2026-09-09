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
using Db = PrimeERP.Data.Core.DbHelper;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Services.HR
{
    // بنفس بنية SalesInvoiceService — قيد واحد بسيط: Debit مصروف الرواتب، Credit الصندوق (سداد نقدي مباشر،
    // بلا حساب "رواتب مستحقة" وسيط — نطاق مُبسَّط عمداً).
    public class PayrollService : ServiceBase, IPayrollService
    {
        private readonly IPayrollRepository _payrolls;
        private readonly IEmployeeRepository _employees;
        private readonly IJournalService _journal;
        private readonly INumberSequenceService _numbers;

        public PayrollService(IPayrollRepository payrolls, IEmployeeRepository employees, IJournalService journal,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _payrolls = payrolls; _employees = employees; _journal = journal; _numbers = numbers;
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

            var baseDto = ToDto(payroll);
            return Result.Ok(new PayrollDetailDto
            {
                Id = baseDto.Id, PayrollNo = baseDto.PayrollNo, PeriodStart = baseDto.PeriodStart, PeriodEnd = baseDto.PeriodEnd, PaymentDate = baseDto.PaymentDate,
                TotalBasic = baseDto.TotalBasic, TotalAllowances = baseDto.TotalAllowances, TotalDeductions = baseDto.TotalDeductions, NetTotal = baseDto.NetTotal,
                CreatedAt = baseDto.CreatedAt,
                Lines = _payrolls.GetLines(id).Select(l => new PayrollLineDto
                { EmployeeId = l.EmployeeId, EmployeeName = l.EmployeeName, BasicSalary = l.BasicSalary, Allowances = l.Allowances, Deductions = l.Deductions, NetSalary = l.NetSalary, Notes = l.Notes }).ToList()
            });
        }

        public Result<PayrollDetailDto> Create(CreatePayrollDto dto)
        {
            if (!Can("PaySalary")) return FailDenied<PayrollDetailDto>();
            if (dto.Lines == null || dto.Lines.Count == 0) return Result.Fail<PayrollDetailDto>("المسير يحتاج سطراً واحداً على الأقل", ErrorCode.ValidationFailed);

            var resolvedLines = new List<PayrollLine>();
            foreach (var l in dto.Lines)
            {
                var employee = _employees.GetByCode(l.EmployeeCode);
                if (employee == null) return Result.Fail<PayrollDetailDto>($"الموظف بالكود {l.EmployeeCode} غير موجود", ErrorCode.ValidationFailed);

                var net = l.BasicSalary + l.Allowances - l.Deductions;
                resolvedLines.Add(new PayrollLine { EmployeeId = employee.Id, EmployeeName = employee.Name, BasicSalary = l.BasicSalary, Allowances = l.Allowances, Deductions = l.Deductions, NetSalary = net, Notes = l.Notes });
            }

            var totalBasic = resolvedLines.Sum(l => l.BasicSalary);
            var totalAllowances = resolvedLines.Sum(l => l.Allowances);
            var totalDeductions = resolvedLines.Sum(l => l.Deductions);
            var netTotal = resolvedLines.Sum(l => l.NetSalary);

            var salariesAccount = Settings.Get(SettingKeys.Accounts.Salaries, "");
            var cashAccount = Settings.Get(SettingKeys.Accounts.Cash, "");
            if (string.IsNullOrWhiteSpace(salariesAccount) || string.IsNullOrWhiteSpace(cashAccount))
                return Result.Fail<PayrollDetailDto>("حساب الرواتب أو الصندوق غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);

            int payrollId;
            try
            {
                payrollId = Db.RunTransaction((conn, tx) =>
                {
                    var payrollNo = _numbers.Next(conn, tx, "Payroll");
                    var payroll = new Payroll
                    {
                        PayrollNo = payrollNo, PeriodStart = dto.PeriodStart, PeriodEnd = dto.PeriodEnd, PaymentDate = dto.PaymentDate,
                        TotalBasic = totalBasic, TotalAllowances = totalAllowances, TotalDeductions = totalDeductions, NetTotal = netTotal,
                        Notes = dto.Notes, CreatedBy = AppSession.Username
                    };
                    var id = _payrolls.InsertHeader(conn, tx, payroll);

                    foreach (var line in resolvedLines)
                        _payrolls.InsertLine(conn, tx, id, line);

                    var journalDto = new CreateJournalDto
                    {
                        EntryDate = dto.PaymentDate, Description = $"مسير رواتب {payrollNo}", Source = nameof(JournalSource.Payroll),
                        Lines = new List<CreateJournalLineDto>
                        {
                            new() { LineNo = 1, AccountCode = salariesAccount, Debit = netTotal },
                            new() { LineNo = 2, AccountCode = cashAccount, Credit = netTotal },
                        }
                    };
                    var createResult = _journal.Create(conn, tx, journalDto);
                    if (!createResult.IsSuccess) throw new InvalidOperationException(createResult.ErrorMessage);

                    var postResult = _journal.Post(conn, tx, createResult.Value.Id);
                    if (!postResult.IsSuccess) throw new InvalidOperationException(postResult.ErrorMessage);

                    _payrolls.SetJournalEntryId(conn, tx, id, createResult.Value.Id);
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

        public Result Update(CreatePayrollDto dto) => Result.Fail("مسير الرواتب مُرحَّل فور إنشائه — لا يمكن تعديله", ErrorCode.ValidationFailed);

        /// <summary>حذف المسير وقيده — لا أثر مخزني له.</summary>
        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var payroll = _payrolls.GetById(id);
            if (payroll == null) return Result.Fail("المسير غير موجود", ErrorCode.NotFound);

            Db.RunTransaction((conn, tx) =>
            {
                if (payroll.JournalEntryId != null) _journal.Delete(conn, tx, payroll.JournalEntryId.Value);
                _payrolls.DeleteDocument(conn, tx, id);
            });

            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private static PayrollDto ToDto(Payroll p) => new()
        {
            Id = p.Id, PayrollNo = p.PayrollNo, PeriodStart = p.PeriodStart, PeriodEnd = p.PeriodEnd, PaymentDate = p.PaymentDate,
            TotalBasic = p.TotalBasic, TotalAllowances = p.TotalAllowances, TotalDeductions = p.TotalDeductions, NetTotal = p.NetTotal, CreatedAt = p.CreatedAt
        };
    }
}
