using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.Services.Sales;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class SalesReturnsViewModel : CrudViewModelBase<SalesReturnDto, SalesReturnFilter>
    {
        private readonly ISalesReturnService _returns;

        public SalesReturnsViewModel(ISalesReturnService returns, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _returns = returns;

        protected override string PermissionPrefix => "Sales";

        protected override Result<PagedResult<SalesReturnDto>> FetchPage(int page, int pageSize, SalesReturnFilter filter)
        {
            var f = filter ?? new SalesReturnFilter();
            f.SearchText = SearchText;
            return _returns.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(SalesReturnDto item) => item.Id;

        protected override Result DeleteItem(int id) => _returns.Delete(id);
    }
}
