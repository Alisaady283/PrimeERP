using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Services.HR;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class EmployeesViewModel : CrudViewModelBase<EmployeeDto, EmployeeFilter>
    {
        private readonly IEmployeeService _employees;

        public EmployeesViewModel(IEmployeeService employees, IPermissionService permissions,
                                   IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _employees = employees;

        protected override string PermissionPrefix => "HR";

        protected override Result<PagedResult<EmployeeDto>> FetchPage(int page, int pageSize, EmployeeFilter filter)
        {
            var f = filter ?? new EmployeeFilter();
            f.SearchText = SearchText;
            return _employees.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(EmployeeDto item) => item.Id;

        protected override Result DeleteItem(int id) => _employees.Delete(id);
    }
}
