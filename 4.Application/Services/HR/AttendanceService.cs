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

namespace PrimeERP.Application.Services.HR
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
                ? Result.Fail<AttendanceDto>("السجل غير موجود", ErrorCode.NotFound)
                : Result.Ok(ToDto(item));
        }

        public Result<AttendanceDto> Create(CreateAttendanceDto dto)
        {
            if (!Can("Create")) return FailDenied<AttendanceDto>();

            var built = Build(dto);
            if (built.IsFailure) return Result.Fail<AttendanceDto>(built.ErrorMessage, built.ErrorCode);

            var item = built.Value;
            item.CreatedBy = CurrentUser;
            item.Id = _attendances.Insert(item);

            Audit.Log(EntityName, item.Id, AuditAction.Insert, newValue: new { item.EmployeeId, item.Date });
            return Result.Ok(ToDto(_attendances.GetById(item.Id)));
        }

        public Result Update(CreateAttendanceDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            if (_attendances.GetById(dto.Id) == null) return Result.Fail("السجل غير موجود", ErrorCode.NotFound);

            var built = Build(dto);
            if (built.IsFailure) return built;

            var item = built.Value;
            item.Id = dto.Id;
            item.UpdatedBy = CurrentUser;

            _attendances.Update(item);
            Audit.Log(EntityName, item.Id, AuditAction.Update);
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            if (_attendances.GetById(id) == null) return Result.Fail("السجل غير موجود", ErrorCode.NotFound);

            _attendances.Delete(id, CurrentUser);
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private Result<Attendance> Build(CreateAttendanceDto dto)
        {
            var employee = _employees.GetByCode(dto.EmployeeCode);
            if (employee == null) return Result.Fail<Attendance>("الموظف مطلوب", ErrorCode.ValidationFailed);
            if (dto.OvertimeHours < 0) return Result.Fail<Attendance>("الساعات الإضافية لا تكون سالبة", ErrorCode.ValidationFailed);

            var checkIn = ParseTime(dto.CheckIn);
            var checkOut = ParseTime(dto.CheckOut);
            if (checkIn != null && checkOut != null && checkOut < checkIn)
                return Result.Fail<Attendance>("وقت الانصراف قبل وقت الحضور", ErrorCode.ValidationFailed);

            return Result.Ok(new Attendance
            {
                EmployeeId = employee.Id, Date = dto.Date, CheckIn = checkIn, CheckOut = checkOut,
                OvertimeHours = dto.OvertimeHours, IsAbsent = dto.IsAbsent, Notes = dto.Notes
            });
        }

        private static TimeSpan? ParseTime(string value) =>
            TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        private static string Show(TimeSpan? value) => value?.ToString(@"hh\:mm");

        private static AttendanceDto ToDto(Attendance a) => new()
        {
            Id = a.Id, EmployeeId = a.EmployeeId, EmployeeCode = a.EmployeeCode, EmployeeName = a.EmployeeName,
            Date = a.Date, CheckIn = Show(a.CheckIn), CheckOut = Show(a.CheckOut),
            OvertimeHours = a.OvertimeHours, IsAbsent = a.IsAbsent,
            StatusText = a.IsAbsent ? "غياب" : "حضور",
            StatusVariant = a.IsAbsent ? StatusVariant.Danger : StatusVariant.Success,
            Notes = a.Notes, CreatedAt = a.CreatedAt
        };
    }
}
