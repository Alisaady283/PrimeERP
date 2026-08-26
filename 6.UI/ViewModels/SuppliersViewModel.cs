using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>ثاني مستهلك لـCrudViewModelBase — يثبت شرط إغلاق R8 (وحدة ثانية بالتكوين، &lt;50 سطراً)
    /// لأن العقد موحّد فعلياً مع Customers (راجع مبدأ استخراج القطعة). AddNew/EditSelected بلا تنفيذ لنفس
    /// سبب CustomersViewModel — حوار المورد الحقيقي يُبنى لاحقاً.</summary>
    public class SuppliersViewModel : CrudViewModelBase<SupplierDto, SupplierFilter>
    {
        private readonly ISupplierService _suppliers;

        public SuppliersViewModel(ISupplierService suppliers, IPermissionService permissions,
                                   IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _suppliers = suppliers;

        protected override string PermissionPrefix => "Suppliers";

        protected override Result<PagedResult<SupplierDto>> FetchPage(int page, int pageSize, SupplierFilter filter)
            => _suppliers.GetPaged(page, pageSize, filter);

        protected override int IdOf(SupplierDto item) => item.Id;

        protected override Result DeleteItem(int id) => _suppliers.Delete(id);

        protected override void AddNew() { }
        protected override void EditSelected() { }
    }
}
