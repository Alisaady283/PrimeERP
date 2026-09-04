using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.Services.Cheques;
using PrimeERP.Application.Services.Vouchers;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
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

    /// <summary>الشيكات تُنشأ من السندات لا من هنا — الشاشة للعرض وتحريك الحالة فقط، لذا الحذف ممنوع.</summary>
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
            Result.Fail("الشيك يُلغى بحركة (ارتداد/رد) لا بالحذف", PrimeERP.Domain.Results.ErrorCode.ValidationFailed);
    }
}
