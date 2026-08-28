using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
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
}
