using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.Services.Purchasing;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class PurchaseInvoicesViewModel : CrudViewModelBase<PurchaseInvoiceDto, PurchaseInvoiceFilter>
    {
        private readonly IPurchaseInvoiceService _invoices;

        public PurchaseInvoicesViewModel(IPurchaseInvoiceService invoices, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _invoices = invoices;

        protected override string PermissionPrefix => "Purchases";

        protected override Result<PagedResult<PurchaseInvoiceDto>> FetchPage(int page, int pageSize, PurchaseInvoiceFilter filter)
        {
            var f = filter ?? new PurchaseInvoiceFilter();
            f.SearchText = SearchText;
            return _invoices.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(PurchaseInvoiceDto item) => item.Id;

        protected override Result DeleteItem(int id) => _invoices.Delete(id);
    }
}
