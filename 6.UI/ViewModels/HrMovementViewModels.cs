using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Services.HR;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>البدل والخصم شاشةٌ واحدة بخدمتين</summary>
    public abstract class EmployeeMovementViewModel : CrudViewModelBase<EmployeeMovementDto, EmployeeMovementFilter>
    {
        private readonly IEmployeeMovementService _service;

        protected EmployeeMovementViewModel(IEmployeeMovementService service, IPermissionService permissions,
            IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _service = service;

        protected override string PermissionPrefix => "HR";

        protected override Result<PagedResult<EmployeeMovementDto>> FetchPage(int page, int pageSize, EmployeeMovementFilter filter)
        {
            var f = filter ?? new EmployeeMovementFilter();
            f.SearchText = SearchText;
            return _service.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(EmployeeMovementDto item) => item.Id;

        protected override Result DeleteItem(int id) => _service.Delete(id);
    }

    public class AllowanceViewModel : EmployeeMovementViewModel
    {
        public AllowanceViewModel(IAllowanceService service, IPermissionService permissions,
            IToastService toast, IDialogService dialogs)
            : base(service, permissions, toast, dialogs) { }
    }

    public class DeductionViewModel : EmployeeMovementViewModel
    {
        public DeductionViewModel(IDeductionService service, IPermissionService permissions,
            IToastService toast, IDialogService dialogs)
            : base(service, permissions, toast, dialogs) { }
    }

    public class AttendanceViewModel : CrudViewModelBase<AttendanceDto, AttendanceFilter>
    {
        private readonly IAttendanceService _attendances;

        public AttendanceViewModel(IAttendanceService attendances, IPermissionService permissions,
            IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _attendances = attendances;

        protected override string PermissionPrefix => "HR";

        protected override Result<PagedResult<AttendanceDto>> FetchPage(int page, int pageSize, AttendanceFilter filter)
        {
            var f = filter ?? new AttendanceFilter();
            f.SearchText = SearchText;
            return _attendances.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(AttendanceDto item) => item.Id;

        protected override Result DeleteItem(int id) => _attendances.Delete(id);
    }
}
