using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Assets;
using PrimeERP.Application.Services.HR;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Purchasing;
using PrimeERP.Application.Services.Sales;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    // نماذج عرض المستندات
/// <summary>نماذج عرض JournalsViewModel</summary>

    public class JournalsViewModel : CrudViewModelBase<JournalEntryDto, JournalFilter>
    {
        private readonly IJournalService _journal;

        public JournalsViewModel(IJournalService journal, IPermissionService permissions,
                                  IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _journal = journal;

        protected override string PermissionPrefix => "Journal";

        protected override Result<PagedResult<JournalEntryDto>> FetchPage(int page, int pageSize, JournalFilter filter)
        {
            var f = filter ?? new JournalFilter();
            f.SearchText = SearchText;
            return _journal.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(JournalEntryDto item) => item.Id;

        protected override Result DeleteItem(int id) => _journal.Delete(id);
    }

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

    public class PurchaseReturnsViewModel : CrudViewModelBase<PurchaseReturnDto, PurchaseReturnFilter>
    {
        private readonly IPurchaseReturnService _returns;

        public PurchaseReturnsViewModel(IPurchaseReturnService returns, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _returns = returns;

        protected override string PermissionPrefix => "Purchases";

        protected override Result<PagedResult<PurchaseReturnDto>> FetchPage(int page, int pageSize, PurchaseReturnFilter filter)
        {
            var f = filter ?? new PurchaseReturnFilter();
            f.SearchText = SearchText;
            return _returns.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(PurchaseReturnDto item) => item.Id;

        protected override Result DeleteItem(int id) => _returns.Delete(id);
    }

    public class StockInViewModel : CrudViewModelBase<StockAdjustmentDto, StockAdjustmentFilter>
    {
        private readonly IStockInService _docs;

        public StockInViewModel(IStockInService docs, IPermissionService permissions, IToastService toast, IDialogService dialogs)
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

    public class StockOutViewModel : CrudViewModelBase<StockAdjustmentDto, StockAdjustmentFilter>
    {
        private readonly IStockOutService _docs;

        public StockOutViewModel(IStockOutService docs, IPermissionService permissions, IToastService toast, IDialogService dialogs)
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

    public class StockTransferViewModel : CrudViewModelBase<StockTransferDto, StockTransferFilter>
    {
        private readonly IStockTransferService _docs;

        public StockTransferViewModel(IStockTransferService docs, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _docs = docs;

        protected override string PermissionPrefix => "Inventory";

        protected override Result<PagedResult<StockTransferDto>> FetchPage(int page, int pageSize, StockTransferFilter filter)
        {
            var f = filter ?? new StockTransferFilter();
            f.SearchText = SearchText;
            return _docs.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(StockTransferDto item) => item.Id;
        protected override Result DeleteItem(int id) => _docs.Delete(id);
    }

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

    public class AssetDepreciationsViewModel : CrudViewModelBase<AssetDepreciationDto, AssetDepreciationFilter>
    {
        private readonly IAssetDepreciationService _depreciation;

        public AssetDepreciationsViewModel(IAssetDepreciationService depreciation, IPermissionService permissions,
                                           IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _depreciation = depreciation;

        protected override string PermissionPrefix => "Assets";

        protected override Result<PagedResult<AssetDepreciationDto>> FetchPage(int page, int pageSize, AssetDepreciationFilter filter)
        {
            var f = filter ?? new AssetDepreciationFilter();
            f.SearchText = SearchText;
            return _depreciation.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(AssetDepreciationDto item) => item.Id;

        protected override Result DeleteItem(int id) => _depreciation.Delete(id);
    }

    public class AssetDisposalsViewModel : CrudViewModelBase<AssetDisposalDto, AssetDisposalFilter>
    {
        private readonly IAssetDisposalService _disposals;

        public AssetDisposalsViewModel(IAssetDisposalService disposals, IPermissionService permissions,
                                       IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _disposals = disposals;

        protected override string PermissionPrefix => "Assets";

        protected override Result<PagedResult<AssetDisposalDto>> FetchPage(int page, int pageSize, AssetDisposalFilter filter)
        {
            var f = filter ?? new AssetDisposalFilter();
            f.SearchText = SearchText;
            return _disposals.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(AssetDisposalDto item) => item.Id;

        protected override Result DeleteItem(int id) => _disposals.Delete(id);
    }

    public class AssetRevaluationsViewModel : CrudViewModelBase<AssetRevaluationDto, AssetRevaluationFilter>
    {
        private readonly IAssetRevaluationService _revaluations;

        public AssetRevaluationsViewModel(IAssetRevaluationService revaluations, IPermissionService permissions,
                                          IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _revaluations = revaluations;

        protected override string PermissionPrefix => "Assets";

        protected override Result<PagedResult<AssetRevaluationDto>> FetchPage(int page, int pageSize, AssetRevaluationFilter filter)
        {
            var f = filter ?? new AssetRevaluationFilter();
            f.SearchText = SearchText;
            return _revaluations.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(AssetRevaluationDto item) => item.Id;

        protected override Result DeleteItem(int id) => _revaluations.Delete(id);
    }
}
