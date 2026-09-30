using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Core;
using System;
using System.Globalization;
using System.Linq;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Domain.Enums;
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

    public class AttendanceService : ServiceBase, IAttendanceService
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

        public Result<PagedResult<AttendanceDto>> GetPaged(int page, int pageSize, AttendanceFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<AttendanceDto>>();
            filter ??= new AttendanceFilter();

            var (items, total) = _attendances.GetPaged(page, pageSize, filter.SearchText, filter.EmployeeId, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<AttendanceDto>
            { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<AttendanceDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<AttendanceDto>();

            var item = _attendances.GetById(id);
            return item == null
                ? Result.Fail<AttendanceDto>(Localization.Get("Str.Common.RecordNotFound"), ErrorCode.NotFound)
                : Result.Ok(ToDto(item));
        }

        public Result<AttendanceDto> Create(CreateAttendanceDto dto)
        {
            if (!Can("Create")) return FailDenied<AttendanceDto>();

            var built = Build(dto);
            if (built.IsFailure) return Result.Fail<AttendanceDto>(built.ErrorMessage, built.ErrorCode);

            var item = built.Value;
            item.Id = _attendances.Insert(item);

            Audit.Log(EntityName, item.Id, AuditAction.Insert, newValue: new { item.EmployeeId, item.Date });
            return Result.Ok(ToDto(_attendances.GetById(item.Id)));
        }

        public Result Update(CreateAttendanceDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            if (_attendances.GetById(dto.Id) == null) return Result.Fail(Localization.Get("Str.Common.RecordNotFound"), ErrorCode.NotFound);

            var built = Build(dto);
            if (built.IsFailure) return built;

            var item = built.Value;
            item.Id = dto.Id;

            _attendances.Update(item);
            Audit.Log(EntityName, item.Id, AuditAction.Update);
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            if (_attendances.GetById(id) == null) return Result.Fail(Localization.Get("Str.Common.RecordNotFound"), ErrorCode.NotFound);

            _attendances.Delete(id, CurrentUser);
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private Result<Attendance> Build(CreateAttendanceDto dto)
        {
            var employee = _employees.GetByCode(dto.EmployeeCode);
            var checkIn = ParseTime(dto.CheckIn);
            var checkOut = ParseTime(dto.CheckOut);

            var input = Check.Valid(dto,
                new Field<CreateAttendanceDto>(x => x.EmployeeCode, "", Must: _ => employee != null, Message: "Str.Employee.Required"),
                new Field<CreateAttendanceDto>(x => x.OvertimeHours, "", From: 0, Message: "Str.Attendance.OvertimeNegative"),
                new Field<CreateAttendanceDto>(x => x.CheckOut, "", Must: _ => checkIn == null || checkOut == null || checkOut >= checkIn,
                    Message: "Str.Attendance.OutBeforeIn"));
            if (input.IsFailure) return input.As<Attendance>();

            return Result.Ok(Rows.Copy(dto, new Attendance(), to =>
            {
                to.EmployeeId = employee.Id;
                to.CheckIn = checkIn;
                to.CheckOut = checkOut;
            }));
        }

        private static TimeSpan? ParseTime(string value) =>
            TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        private static string Show(TimeSpan? value) => value?.ToString(@"hh\:mm");

        private static AttendanceDto ToDto(Attendance a)
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
