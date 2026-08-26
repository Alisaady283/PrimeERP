using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>أول مستهلك لـTreeViewModelBase — R11، يثبت أن Composition يمتد لتخطيط شجرة كامل (LayoutKind.
    /// TreeSplit) بصفر منطق شجرة هنا (TreeRenderer يبني الشجرة فعلياً من TreeLayoutOptions المسجَّلة في
    /// ModuleRegistrations). PageSize كبيرة عمداً — شجرة تحتاج الهرم كاملاً دفعة واحدة، لا صفحة واحدة منه
    /// (نفس قيمة AccountPicker.OpenSelectionWindow: DataSource.SearchAsync("", 5000)).</summary>
    public class AccountsViewModel : TreeViewModelBase<AccountDto, AccountTreeFilter>
    {
        private readonly IAccountService _accounts;

        public AccountsViewModel(IAccountService accounts, IPermissionService permissions, IToastService toast)
            : base(permissions, toast)
        {
            _accounts = accounts;
            PageSize = 5000;
        }

        protected override string PermissionPrefix => "Accounts";

        protected override Result<PagedResult<AccountDto>> FetchPage(int page, int pageSize, AccountTreeFilter filter)
            => _accounts.GetPaged(page, pageSize, filter);
    }
}
