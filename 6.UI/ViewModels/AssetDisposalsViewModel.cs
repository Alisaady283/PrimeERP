using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Services.Assets;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
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
}
