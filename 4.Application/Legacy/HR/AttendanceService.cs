using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.HR
{
    /// <summary>الحضور والانصراف</summary>
    public interface IAttendanceService
    {
        Result<PagedResult<AttendanceDto>> GetPaged(int page, int pageSize, AttendanceFilter filter = null);
        Result<AttendanceDto> GetById(int id);
        Result<AttendanceDto> Create(CreateAttendanceDto dto);
        Result Update(CreateAttendanceDto dto);
        Result Delete(int id);
    }

    public class AttendanceService
        : EntityService<Attendance, AttendanceDto, CreateAttendanceDto, CreateAttendanceDto, AttendanceFilter>, IAttendanceService
    {
        private readonly IAttendanceRepository _attendances;
        private readonly IEmployeeRepository _employees;

        public AttendanceService(IAttendanceRepository attendances, IEmployeeRepository employees,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _attendances = attendances; _employees = employees;
        }

        protected override string PermissionPrefix => "HR";
        protected override string StringPrefix => "Str.Attendance";
        protected override string EntityName => "Attendances";

        public static readonly Field<Attendance>[] AttendanceFields =
        [
            .. EmployeeCode.Rules<Attendance>(),
            new(x => x.OvertimeHours, "", From: 0, Message: "Str.Attendance.OvertimeNegative"),
            new(x => x.CheckOut, "", Must: a => a.CheckIn == null || a.CheckOut == null || a.CheckOut >= a.CheckIn,
                Message: "Str.Attendance.OutBeforeIn"),
        ];

        protected override Field<Attendance>[] Fields => AttendanceFields;

        protected override Attendance FindById(int id) => _attendances.GetById(id);

        protected override (List<Attendance> Items, int Total) FindPaged(int page, int pageSize, AttendanceFilter filter)
        {
            filter ??= new AttendanceFilter();
            return _attendances.GetPaged(page, pageSize, filter.SearchText, filter.EmployeeId, filter.SortBy, filter.SortDescending);
        }

        protected override List<Attendance> FindSearch(string term, int maxResults) =>
            _attendances.GetPaged(1, maxResults, term, null, null, false).Items;

        protected override Attendance New(CreateAttendanceDto dto) => Rows.Copy(dto, new Attendance(), to => Fill(to, dto));

        protected override void Apply(Attendance entity, CreateAttendanceDto dto) => Rows.Copy(dto, entity, to => Fill(to, dto));

        protected override int IdOf(CreateAttendanceDto dto) => dto.Id;

        protected override int Insert(PrimeDbContext db, Attendance a) => _attendances.Insert(a, db);

        protected override void Save(PrimeDbContext db, Attendance a) => _attendances.Update(a, db);

        protected override void Erase(PrimeDbContext db, Attendance a) => _attendances.Delete(a.Id, CurrentUser, db);

        protected override object AuditValue(Attendance a) => new { a.EmployeeId, a.Date };

        /// <summary>الموظف والوقتان من المُدخل</summary>
        private void Fill(Attendance to, CreateAttendanceDto dto)
        {
            to.EmployeeId = _employees.IdByCode(dto.EmployeeCode);
            to.CheckIn = ParseTime(dto.CheckIn);
            to.CheckOut = ParseTime(dto.CheckOut);
        }

        private static TimeSpan? ParseTime(string value) =>
            TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        private static string Show(TimeSpan? value) => value?.ToString(@"hh\:mm");

        protected override AttendanceDto ToDto(Attendance a)
        {
            var (variant, status) = Rows.State((a.IsAbsent, StatusVariant.Danger, "Str.Attendance.Absent"),
                (true, StatusVariant.Success, "Str.Attendance.Present"));
            return Rows.Copy<AttendanceDto>(a, new(), to =>
            {
                to.CheckIn = Show(a.CheckIn);
                to.CheckOut = Show(a.CheckOut);
                to.StatusText = status;
                to.StatusVariant = variant;
            });
        }
    }
}
