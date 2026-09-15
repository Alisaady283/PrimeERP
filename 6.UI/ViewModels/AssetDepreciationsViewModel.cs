using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Services.Assets;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
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
}
