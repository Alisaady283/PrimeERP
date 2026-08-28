using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Services.HR;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class PayrollViewModel : CrudViewModelBase<PayrollDto, PayrollFilter>
    {
        private readonly IPayrollService _payrolls;

        public PayrollViewModel(IPayrollService payrolls, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _payrolls = payrolls;

        protected override string PermissionPrefix => "HR";

        protected override Result<PagedResult<PayrollDto>> FetchPage(int page, int pageSize, PayrollFilter filter)
        {
            var f = filter ?? new PayrollFilter();
            f.SearchText = SearchText;
            return _payrolls.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(PayrollDto item) => item.Id;
        protected override Result DeleteItem(int id) => _payrolls.Delete(id);
    }
}
