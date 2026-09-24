using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>نماذج عرض CycleVoucherViewModelBase</summary>
    public abstract class CycleVoucherViewModelBase : CrudViewModelBase<StockAdjustmentDto, StockAdjustmentFilter>
    {
        private readonly IStockInService _docs;

        protected CycleVoucherViewModelBase(IStockInService docs, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _docs = docs;

        protected override string PermissionPrefix => "Inventory";

        protected override Result<PagedResult<StockAdjustmentDto>> FetchPage(int page, int pageSize, StockAdjustmentFilter filter)
        {
            var f = filter ?? new StockAdjustmentFilter();
            f.SearchText = SearchText;
            return _docs.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(StockAdjustmentDto item) => item.Id;
        protected override Result DeleteItem(int id) => _docs.Delete(id);
    }

    public class GoodsReceiptViewModel : CycleVoucherViewModelBase
    {
        public GoodsReceiptViewModel(IGoodsReceiptService docs, IPermissionService p, IToastService t, IDialogService d) : base(docs, p, t, d) { }
    }

    public class GoodsIssueViewModel : CycleVoucherViewModelBase
    {
        public GoodsIssueViewModel(IGoodsIssueService docs, IPermissionService p, IToastService t, IDialogService d) : base(docs, p, t, d) { }
    }

    public class DeliveryNoteViewModel : CycleVoucherViewModelBase
    {
        public DeliveryNoteViewModel(IDeliveryNoteService docs, IPermissionService p, IToastService t, IDialogService d) : base(docs, p, t, d) { }
    }

    public class SalesReceiptViewModel : CycleVoucherViewModelBase
    {
        public SalesReceiptViewModel(ISalesReceiptService docs, IPermissionService p, IToastService t, IDialogService d) : base(docs, p, t, d) { }
    }
}
