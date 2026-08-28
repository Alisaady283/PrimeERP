using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.Services.Purchasing;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
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
}
