using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>
    /// أول مستهلك حقيقي لـCrudViewModelBase — يثبت شرط إغلاق R7 (VM نمطي &lt;60 سطر). AddNew/EditSelected
    /// بلا تنفيذ عمداً: لا حوار عميل حقيقي بعد (Views/Dialogs حُذفت كـstubs فارغة في R4، تُبنى عبر
    /// 7.Composition/8.Modules في R8/R9) — وضعها هنا الآن يعني حواراً وهمياً، ممنوع صراحة.
    /// </summary>
    public class CustomersViewModel : CrudViewModelBase<CustomerDto, CustomerFilter>
    {
        private readonly ICustomerService _customers;

        public CustomersViewModel(ICustomerService customers, IPermissionService permissions,
                                   IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _customers = customers;

        protected override string PermissionPrefix => "Customers";

        protected override Result<PagedResult<CustomerDto>> FetchPage(int page, int pageSize, CustomerFilter filter)
            => _customers.GetPaged(page, pageSize, filter);

        protected override int IdOf(CustomerDto item) => item.Id;

        protected override Result DeleteItem(int id) => _customers.Delete(id);

        // AddNew/EditSelected: تُستكمَل في R8/R9 عند بناء CustomerDialog الحقيقي عبر 7.Composition.
        protected override void AddNew() { }
        protected override void EditSelected() { }
    }
}
