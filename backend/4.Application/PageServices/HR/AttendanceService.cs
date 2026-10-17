using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.PageServices.HR
{
    public interface IAttendanceService
    {
        Result<PagedResult<AttendanceDayDto>> GetPaged(int page, int pageSize, AttendanceFilter filter = null);
        Result<AttendanceSheetDto> GetById(int id);
        Result<AttendanceSheetDto> Open();
        Result<AttendanceSheetDto> Open(DateTime date);
        Result<AttendanceSheetDto> Create(AttendanceSheetDto sheet);
        Result Update(AttendanceSheetDto sheet);
        Result Delete(int id);
    }

    public class AttendanceService
        : DocumentService<AttendanceSheet, AttendanceDayDto, AttendanceSheetDto, AttendanceSheetDto, AttendanceFilter>, IAttendanceService
    {
        private readonly IAttendanceRepository _sheets;
        private readonly IEmployeeRepository _employees;

        public AttendanceService(IAttendanceRepository sheets, IEmployeeRepository employees,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _sheets = sheets; _employees = employees;
        }

        protected override string PermissionPrefix => "HR";
        protected override string StringPrefix => "Str.Attendance";
        protected override string EntityName => "AttendanceSheets";
        protected override bool Editable => true;

        public static readonly Field<Attendance>[] AttendanceFields =
        [
            .. EmployeeCode.Rules<Attendance>(),
            new(x => x.CheckOut, "", Must: a => a.CheckIn == null || a.CheckOut == null || a.CheckOut >= a.CheckIn,
                Message: "Str.Attendance.OutBeforeIn", Args: a => new object[] { a.EmployeeName }),
            new(x => x.LeaveTypeId, "", Must: a => a.Status != AttendanceStatus.Leave || a.LeaveTypeId != null,
                Message: "Str.Attendance.LeaveTypeRequired", Args: a => new object[] { a.EmployeeName }),
        ];

        protected override AttendanceSheet FindHead(int id) => _sheets.GetById(id);
        protected override int IdOf(AttendanceSheet head) => head.Id;
        protected override int IdOf(AttendanceSheetDto dto) => dto.Id;
        protected override object AuditValue(AttendanceSheet head) => new { head?.Date };

        protected override (List<AttendanceSheet> Items, int Total) FindPage(int page, int pageSize, AttendanceFilter filter) =>
            _sheets.GetPaged(page, pageSize, filter?.SearchText);

        protected override List<AttendanceDayDto> ToRows(List<AttendanceSheet> heads)
        {
            var counts = _sheets.Counts(heads.Select(h => h.Id));
            return heads.Select(h =>
            {
                var count = counts.GetValueOrDefault(h.Id) ?? new Dictionary<AttendanceStatus, int>();
                return Rows.Copy(h, new AttendanceDayDto(), row =>
                {
                    row.Present = count.GetValueOrDefault(AttendanceStatus.Present);
                    row.Absent = count.GetValueOrDefault(AttendanceStatus.Absent);
                    row.Leave = count.GetValueOrDefault(AttendanceStatus.Leave);
                    row.Mission = count.GetValueOrDefault(AttendanceStatus.Mission);
                    row.Holiday = count.GetValueOrDefault(AttendanceStatus.Holiday);
                });
            }).ToList();
        }

        protected override AttendanceSheetDto ToDetail(AttendanceSheet head) =>
            Rows.Copy(head, new AttendanceSheetDto(), sheet => sheet.Lines = Lines(_sheets.LinesOf(head.Id)));

        public Result<AttendanceSheetDto> Open() => Open(DateTime.Today);

        public Result<AttendanceSheetDto> Open(DateTime date)
        {
            if (!Can("Create")) return FailDenied<AttendanceSheetDto>();
            if (_sheets.SheetOn(date) != null) return Fail<AttendanceSheetDto>("DayExists", ErrorCode.ValidationFailed, date.ToString("yyyy-MM-dd"));

            return Result.Ok(new AttendanceSheetDto { Date = date.Date, Lines = Lines(new Dictionary<int, Attendance>()) });
        }

        protected override Result<Func<PrimeDbContext, int>> Plan(AttendanceSheetDto dto)
        {
            if (_sheets.SheetOn(dto.Date) is int other && other != dto.Id)
                return Fail<Func<PrimeDbContext, int>>("DayExists", ErrorCode.ValidationFailed, dto.Date.ToString("yyyy-MM-dd"));

            var rules = PayrollSettings.Rules(Settings);
            var days = ByCode.Resolve(codes => _employees.ByCodes(codes), dto.Lines, l => l.EmployeeCode, "Str.Employee.CodeNotFound", (line, employee, _) =>
            {
                var shift = PayrollCalc.Shift(employee, rules);
                var day = Rows.Copy(line, new Attendance(), a =>
                {
                    a.EmployeeId = employee.Id;
                    a.EmployeeName = employee.Name;
                    a.Date = dto.Date.Date;
                });
                day.LateMinutes = PayrollCalc.LateMinutes(day, shift.Start);
                day.OvertimeMinutes = PayrollCalc.OvertimeMinutes(day, shift.End);
                return Check.Valid(day, AttendanceFields).Then(() => Result.Ok(day));
            });
            if (days.IsFailure) return days.As<Func<PrimeDbContext, int>>();

            return Result.Ok<Func<PrimeDbContext, int>>(db =>
            {
                var id = _sheets.InsertHeader(db, new AttendanceSheet { Date = dto.Date.Date, Notes = dto.Notes });
                foreach (var day in days.Value) _sheets.InsertLine(db, id, day);
                return id;
            });
        }

        protected override void Remove(PrimeDbContext db, AttendanceSheet head) => _sheets.DeleteDocument(db, head.Id);

        private List<AttendanceLineDto> Lines(IReadOnlyDictionary<int, Attendance> recorded)
        {
            var rules = PayrollSettings.Rules(Settings);
            return _employees.GetAll(activeOnly: true).Select((e, i) =>
            {
                var shift = PayrollCalc.Shift(e, rules);
                var day = recorded.GetValueOrDefault(e.Id) ?? new Attendance { CheckIn = shift.Start, CheckOut = shift.End };
                return Rows.Copy(day, new AttendanceLineDto(), row =>
                {
                    row.LineNo = i + 1;
                    row.EmployeeCode = e.Code;
                    row.EmployeeName = e.Name;
                    row.DepartmentName = e.DepartmentName;
                    row.Late = (string)Rows.To(TimeSpan.FromMinutes(day.LateMinutes), typeof(string));
                    row.Overtime = (string)Rows.To(TimeSpan.FromMinutes(day.OvertimeMinutes), typeof(string));
                });
            }).ToList();
        }
    }
}
