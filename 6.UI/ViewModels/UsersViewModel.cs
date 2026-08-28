using System.Linq;
using PrimeERP.Application.DTOs.Security;
using PrimeERP.Application.Services.Security;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class UsersViewModel : CrudViewModelBase<UserDto, UserFilter>
    {
        private readonly IUserService _users;

        public UsersViewModel(IUserService users, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _users = users;

        protected override string PermissionPrefix => "Users";

        protected override Result<PagedResult<UserDto>> FetchPage(int page, int pageSize, UserFilter filter)
        {
            var result = _users.GetAll();
            if (!result.IsSuccess) return Result.Fail<PagedResult<UserDto>>(result.ErrorMessage);

            var items = result.Value;
            if (!string.IsNullOrWhiteSpace(SearchText))
                items = items.Where(u => u.Username.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)
                                       || u.DisplayName.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)).ToList();

            return Result.Ok(new PagedResult<UserDto> { Items = items, Page = 1, PageSize = items.Count, TotalCount = items.Count });
        }

        protected override int IdOf(UserDto item) => item.Id;
        protected override Result DeleteItem(int id) => _users.Delete(id);
    }
}
