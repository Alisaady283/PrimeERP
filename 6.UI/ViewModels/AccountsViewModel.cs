using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>نماذج عرض AccountsViewModel</summary>
    public class AccountsViewModel : TreeViewModelBase<AccountDto, AccountTreeFilter>
    {
        private readonly IAccountService _accounts;

        public AccountsViewModel(IAccountService accounts, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs)
        {
            _accounts = accounts;
            PageSize = 5000;
        }

        protected override string PermissionPrefix => "Accounts";

        protected override Result<PagedResult<AccountDto>> FetchPage(int page, int pageSize, AccountTreeFilter filter)
        {
            _accounts.RecalculateAllBalances();
            return _accounts.GetPaged(page, pageSize, filter);
        }

        protected override int IdOf(AccountDto item) => item.Id;

        protected override Result DeleteItem(int id) => _accounts.Delete(id);
    }
}
