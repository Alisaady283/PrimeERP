using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Services.Assets;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class AssetsViewModel : CrudViewModelBase<AssetDto, AssetFilter>
    {
        private readonly IAssetService _assets;

        public AssetsViewModel(IAssetService assets, IPermissionService permissions,
                                IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _assets = assets;

        protected override string PermissionPrefix => "Assets";

        protected override Result<PagedResult<AssetDto>> FetchPage(int page, int pageSize, AssetFilter filter)
        {
            var f = filter ?? new AssetFilter();
            f.SearchText = SearchText;
            return _assets.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(AssetDto item) => item.Id;

        protected override Result DeleteItem(int id) => _assets.Delete(id);
    }
}
