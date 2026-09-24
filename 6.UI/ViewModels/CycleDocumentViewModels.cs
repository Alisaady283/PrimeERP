using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>نماذج عرض CycleDocumentViewModelBase</summary>
    public abstract class CycleDocumentViewModelBase : CrudViewModelBase<CycleDocumentDto, CycleDocumentFilter>
    {
        private readonly ICycleDocumentService _docs;
        private readonly string _permissionPrefix;

        protected CycleDocumentViewModelBase(ICycleDocumentService docs, string permissionPrefix,
            IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs)
        {
            _docs = docs;
            _permissionPrefix = permissionPrefix;
        }

        protected override string PermissionPrefix => _permissionPrefix;

        protected override Result<PagedResult<CycleDocumentDto>> FetchPage(int page, int pageSize, CycleDocumentFilter filter)
        {
            var f = filter ?? new CycleDocumentFilter();
            f.SearchText = SearchText;
            return _docs.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(CycleDocumentDto item) => item.Id;
        protected override Result DeleteItem(int id) => _docs.Delete(id);
    }

    public class PurchaseRequestViewModel : CycleDocumentViewModelBase
    {
        public PurchaseRequestViewModel(IPurchaseRequestService docs, IPermissionService p, IToastService t, IDialogService d)
            : base(docs, "Purchases", p, t, d) { }
    }

    public class PurchaseOrderViewModel : CycleDocumentViewModelBase
    {
        public PurchaseOrderViewModel(IPurchaseOrderService docs, IPermissionService p, IToastService t, IDialogService d)
            : base(docs, "Purchases", p, t, d) { }
    }

    public class QuotationViewModel : CycleDocumentViewModelBase
    {
        public QuotationViewModel(IQuotationService docs, IPermissionService p, IToastService t, IDialogService d)
            : base(docs, "Sales", p, t, d) { }
    }

    public class SalesOrderViewModel : CycleDocumentViewModelBase
    {
        public SalesOrderViewModel(ISalesOrderService docs, IPermissionService p, IToastService t, IDialogService d)
            : base(docs, "Sales", p, t, d) { }
    }
}
