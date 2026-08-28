using System.Linq;
using PrimeERP.Application.DTOs.Security;
using PrimeERP.Application.Services.Security;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class RolesViewModel : CrudViewModelBase<RoleDto, RoleFilter>
    {
        private readonly IRoleService _roles;

        public RolesViewModel(IRoleService roles, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _roles = roles;

        protected override string PermissionPrefix => "Users";

        protected override Result<PagedResult<RoleDto>> FetchPage(int page, int pageSize, RoleFilter filter)
        {
            var result = _roles.GetAll();
            if (!result.IsSuccess) return Result.Fail<PagedResult<RoleDto>>(result.ErrorMessage);

            var items = result.Value;
            if (!string.IsNullOrWhiteSpace(SearchText))
                items = items.Where(r => r.NameAr.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)).ToList();

            return Result.Ok(new PagedResult<RoleDto> { Items = items, Page = 1, PageSize = items.Count, TotalCount = items.Count });
        }

        protected override int IdOf(RoleDto item) => item.Id;
        protected override Result DeleteItem(int id) => _roles.Delete(id);
    }
}
