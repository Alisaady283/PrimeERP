using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Services.Assets;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class AssetRevaluationsViewModel : CrudViewModelBase<AssetRevaluationDto, AssetRevaluationFilter>
    {
        private readonly IAssetRevaluationService _revaluations;

        public AssetRevaluationsViewModel(IAssetRevaluationService revaluations, IPermissionService permissions,
                                          IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _revaluations = revaluations;

        protected override string PermissionPrefix => "Assets";

        protected override Result<PagedResult<AssetRevaluationDto>> FetchPage(int page, int pageSize, AssetRevaluationFilter filter)
        {
            var f = filter ?? new AssetRevaluationFilter();
            f.SearchText = SearchText;
            return _revaluations.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(AssetRevaluationDto item) => item.Id;

        protected override Result DeleteItem(int id) => _revaluations.Delete(id);
    }
}
