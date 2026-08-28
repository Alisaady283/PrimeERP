using System.Linq;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class WarehousesViewModel : CrudViewModelBase<WarehouseDto, WarehouseFilter>
    {
        private readonly IWarehouseService _warehouses;

        public WarehousesViewModel(IWarehouseService warehouses, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _warehouses = warehouses;

        protected override string PermissionPrefix => "Warehouses";

        protected override Result<PagedResult<WarehouseDto>> FetchPage(int page, int pageSize, WarehouseFilter filter)
        {
            var result = _warehouses.GetAll();
            if (!result.IsSuccess) return Result.Fail<PagedResult<WarehouseDto>>(result.ErrorMessage);

            var items = result.Value;
            if (!string.IsNullOrWhiteSpace(SearchText))
                items = items.Where(w => w.Name.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)).ToList();

            return Result.Ok(new PagedResult<WarehouseDto> { Items = items, Page = 1, PageSize = items.Count, TotalCount = items.Count });
        }

        protected override int IdOf(WarehouseDto item) => item.Id;
        protected override Result DeleteItem(int id) => _warehouses.Delete(id);
    }
}
