using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.ViewModels
{
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

        /// <summary>
        /// الشجرة تجميعٌ للقيود: الرصيد يُعاد جمعه من الأسطر المرحَّلة عند كل فتح، فلا يبقى رقمٌ متقادم
        /// من حذفٍ سابق. الخدمة مبنيّة في النظام (RecalculateAllBalances) — تُستدعى هنا لا تُكتب ثانيةً،
        /// ونتيجتها مُهمَلة عمداً: مستخدمٌ بلا صلاحية تعديل يرى الأرصدة كما هي بدل أن تُمنع عنه الشاشة.
        /// </summary>
        protected override Result<PagedResult<AccountDto>> FetchPage(int page, int pageSize, AccountTreeFilter filter)
        {
            _accounts.RecalculateAllBalances();
            return _accounts.GetPaged(page, pageSize, filter);
        }

        protected override int IdOf(AccountDto item) => item.Id;

        protected override Result DeleteItem(int id) => _accounts.Delete(id);
    }
}
