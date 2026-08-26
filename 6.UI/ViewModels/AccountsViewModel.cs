using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>ثالث مستهلك لـCrudViewModelBase — الوحدة الثالثة بالتكوين (R9)، تثبت أن الشجرة الهرمية
    /// (Account.ParentId/Level) تمر عبر نفس عقد GetPaged/AccountTreeFilter بلا أي تعديل في القاعدة.
    /// AddNew/EditSelected بلا تنفيذ لنفس سبب Customers/Suppliers.</summary>
    public class AccountsViewModel : CrudViewModelBase<AccountDto, AccountTreeFilter>
    {
        private readonly IAccountService _accounts;

        public AccountsViewModel(IAccountService accounts, IPermissionService permissions,
                                  IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _accounts = accounts;

        protected override string PermissionPrefix => "Accounts";

        protected override Result<PagedResult<AccountDto>> FetchPage(int page, int pageSize, AccountTreeFilter filter)
            => _accounts.GetPaged(page, pageSize, filter);

        protected override int IdOf(AccountDto item) => item.Id;

        protected override Result DeleteItem(int id) => _accounts.Delete(id);

        protected override void AddNew() { }
        protected override void EditSelected() { }
    }
}
