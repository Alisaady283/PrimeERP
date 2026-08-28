using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.Services.Sales;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class SalesInvoicesViewModel : CrudViewModelBase<SalesInvoiceDto, SalesInvoiceFilter>
    {
        private readonly ISalesInvoiceService _invoices;

        public SalesInvoicesViewModel(ISalesInvoiceService invoices, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _invoices = invoices;

        protected override string PermissionPrefix => "Sales";

        protected override Result<PagedResult<SalesInvoiceDto>> FetchPage(int page, int pageSize, SalesInvoiceFilter filter)
        {
            var f = filter ?? new SalesInvoiceFilter();
            f.SearchText = SearchText;
            return _invoices.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(SalesInvoiceDto item) => item.Id;

        protected override Result DeleteItem(int id) => _invoices.Delete(id);
    }
}
