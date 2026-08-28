using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
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
}
