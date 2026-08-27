using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>أول مستهلك حقيقي لـCrudViewModelBase — يثبت شرط إغلاق R7 (VM نمطي &lt;60 سطر). لا Dialog
    /// مسجَّل بعد في ModuleRegistrations لهذه الوحدة — AddRequested/EditRequested بلا مستمعين حالياً، فزر
    /// الإضافة/التعديل بلا أثر إلى أن يُضاف Dialog حقيقي هناك (لا كود إضافي هنا مطلوب حينها).</summary>
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
    }
}
