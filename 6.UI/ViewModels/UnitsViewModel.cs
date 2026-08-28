using System.Linq;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class UnitsViewModel : CrudViewModelBase<UnitDto, UnitFilter>
    {
        private readonly IUnitService _units;

        public UnitsViewModel(IUnitService units, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _units = units;

        protected override string PermissionPrefix => "Units";

        protected override Result<PagedResult<UnitDto>> FetchPage(int page, int pageSize, UnitFilter filter)
        {
            var result = _units.GetAll();
            if (!result.IsSuccess) return Result.Fail<PagedResult<UnitDto>>(result.ErrorMessage);

            var items = result.Value;
            if (!string.IsNullOrWhiteSpace(SearchText))
                items = items.Where(u => u.Name.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)).ToList();

            return Result.Ok(new PagedResult<UnitDto> { Items = items, Page = 1, PageSize = items.Count, TotalCount = items.Count });
        }

        protected override int IdOf(UnitDto item) => item.Id;
        protected override Result DeleteItem(int id) => _units.Delete(id);
    }
}
