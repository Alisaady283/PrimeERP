using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.Legacy.Cheques;
using PrimeERP.Application.Legacy.Vouchers;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;
using PrimeERP.Platform.Localization;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>نماذج عرض السندات والشيكات</summary>
    public abstract class VoucherViewModelBase : CrudViewModelBase<VoucherDto, VoucherFilter>
    {
        private readonly IVoucherService _vouchers;
        private readonly string _permissionPrefix;

        protected VoucherViewModelBase(IVoucherService vouchers, string permissionPrefix,
            IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs)
        {
            _vouchers = vouchers;
            _permissionPrefix = permissionPrefix;
        }

        protected override string PermissionPrefix => _permissionPrefix;

        protected override Result<PagedResult<VoucherDto>> FetchPage(int page, int pageSize, VoucherFilter filter)
        {
            var f = filter ?? new VoucherFilter();
            f.SearchText = SearchText;
            return _vouchers.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(VoucherDto item) => item.Id;
        protected override Result DeleteItem(int id) => _vouchers.Delete(id);
    }

    public class ReceiptVouchersViewModel : VoucherViewModelBase
    {
        public ReceiptVouchersViewModel(IReceiptVoucherService vouchers, IPermissionService p, IToastService t, IDialogService d)
            : base(vouchers, "Receipts", p, t, d) { }
    }

    public class PaymentVouchersViewModel : VoucherViewModelBase
    {
        public PaymentVouchersViewModel(IPaymentVoucherService vouchers, IPermissionService p, IToastService t, IDialogService d)
            : base(vouchers, "Payments", p, t, d) { }
    }

    /// <summary>الشيك يُنشأ من سنده</summary>
    public class ChequesViewModel : CrudViewModelBase<ChequeDto, ChequeFilter>
    {
        private readonly IChequeService _cheques;

        public ChequesViewModel(IChequeService cheques, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _cheques = cheques;

        protected override string PermissionPrefix => "Cheques";

        protected override Result<PagedResult<ChequeDto>> FetchPage(int page, int pageSize, ChequeFilter filter)
        {
            var f = filter ?? new ChequeFilter();
            f.SearchText = SearchText;
            return _cheques.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(ChequeDto item) => item.Id;

        protected override Result DeleteItem(int id) =>
            Result.Fail(LocalizationService.Get("Str.Cheque.DeleteByMovement"), PrimeERP.Domain.Results.ErrorCode.ValidationFailed);
    }

    /// <summary>شبكة شيكات الاتجاه الواحد</summary>
    public abstract class ChequeDocumentViewModelBase : CrudViewModelBase<ChequeDto, ChequeFilter>
    {
        private readonly IChequeDocumentService _documents;

        protected ChequeDocumentViewModelBase(IChequeDocumentService documents, IPermissionService permissions,
            IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _documents = documents;

        protected override string PermissionPrefix => "Cheques";

        protected override Result<PagedResult<ChequeDto>> FetchPage(int page, int pageSize, ChequeFilter filter)
        {
            var f = filter ?? new ChequeFilter();
            f.SearchText = SearchText;
            return _documents.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(ChequeDto item) => item.Id;

        protected override Result DeleteItem(int id) => _documents.Delete(id);
    }

    public class ChequeReceiptsViewModel : ChequeDocumentViewModelBase
    {
        public ChequeReceiptsViewModel(PrimeERP.Application.Legacy.Cheques.IChequeReceiptDocumentService documents, IPermissionService permissions,
            IToastService toast, IDialogService dialogs) : base(documents, permissions, toast, dialogs) { }
    }

    public class ChequeIssuesViewModel : ChequeDocumentViewModelBase
    {
        public ChequeIssuesViewModel(PrimeERP.Application.Legacy.Cheques.IChequeIssueDocumentService documents, IPermissionService permissions,
            IToastService toast, IDialogService dialogs) : base(documents, permissions, toast, dialogs) { }
    }

    /// <summary>الأرصدة الافتتاحية: حساباتٌ وأصناف</summary>
    public class OpeningBalancesViewModel : CrudViewModelBase<JournalEntryDto, JournalFilter>
    {
        private readonly PrimeERP.Application.Legacy.Accounting.IOpeningBalanceService _openings;

        public OpeningBalancesViewModel(PrimeERP.Application.Legacy.Accounting.IOpeningBalanceService openings,
            IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _openings = openings;

        protected override string PermissionPrefix => "Journal";

        protected override Result<PagedResult<JournalEntryDto>> FetchPage(int page, int pageSize, JournalFilter filter)
        {
            var f = filter ?? new JournalFilter();
            f.SearchText = SearchText;
            return _openings.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(JournalEntryDto item) => item.Id;

        protected override Result DeleteItem(int id) => _openings.Delete(id);
    }
}
