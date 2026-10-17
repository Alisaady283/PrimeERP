using PrimeERP.Application.PageServices.Security;
using System.Linq;
using System;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Security;
using PrimeERP.Application.DTOs.Treasury;
using PrimeERP.Application.PageServices.Assets;
using PrimeERP.Application.PageServices.HR;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.PageServices.Parties;
using PrimeERP.Application.PageServices.Treasury;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    // نماذج عرض القوائم
/// <summary>نماذج عرض UnitsViewModel</summary>



    public class ProductsViewModel : CrudViewModelBase<Product, ProductFilter>
    {
        private readonly IProductService _products;

        public ProductsViewModel(IProductService products, IPermissionService permissions,
                                  IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _products = products;

        protected override string PermissionPrefix => "Products";

        protected override Result<PagedResult<Product>> FetchPage(int page, int pageSize, ProductFilter filter)
        {
            var f = filter ?? new ProductFilter();
            f.SearchText = SearchText;
            return _products.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(Product item) => item.Id;

        protected override Result DeleteItem(int id) => _products.Delete(id);
    }

    public class CustomersViewModel : CrudViewModelBase<Customer, CustomerFilter>
    {
        private readonly ICustomerService _customers;

        public CustomersViewModel(ICustomerService customers, IPermissionService permissions,
                                   IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _customers = customers;

        protected override string PermissionPrefix => "Customers";

        protected override Result<PagedResult<Customer>> FetchPage(int page, int pageSize, CustomerFilter filter)
            => _customers.GetPaged(page, pageSize, filter);

        protected override int IdOf(Customer item) => item.Id;

        protected override Result DeleteItem(int id) => _customers.Delete(id);
    }

    public class SuppliersViewModel : CrudViewModelBase<Supplier, SupplierFilter>
    {
        private readonly ISupplierService _suppliers;

        public SuppliersViewModel(ISupplierService suppliers, IPermissionService permissions,
                                   IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _suppliers = suppliers;

        protected override string PermissionPrefix => "Suppliers";

        protected override Result<PagedResult<Supplier>> FetchPage(int page, int pageSize, SupplierFilter filter)
            => _suppliers.GetPaged(page, pageSize, filter);

        protected override int IdOf(Supplier item) => item.Id;

        protected override Result DeleteItem(int id) => _suppliers.Delete(id);
    }

    public class EmployeesViewModel : CrudViewModelBase<Employee, EmployeeFilter>
    {
        private readonly IEmployeeService _employees;

        public EmployeesViewModel(IEmployeeService employees, IPermissionService permissions,
                                   IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _employees = employees;

        protected override string PermissionPrefix => "HR";

        protected override Result<PagedResult<Employee>> FetchPage(int page, int pageSize, EmployeeFilter filter)
        {
            var f = filter ?? new EmployeeFilter();
            f.SearchText = SearchText;
            return _employees.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(Employee item) => item.Id;

        protected override Result DeleteItem(int id) => _employees.Delete(id);
    }



    public class RolesViewModel : CrudViewModelBase<Role, RoleFilter>
    {
        private readonly IRoleService _roles;

        public RolesViewModel(IRoleService roles, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _roles = roles;

        protected override string PermissionPrefix => "Users";

        protected override Result<PagedResult<Role>> FetchPage(int page, int pageSize, RoleFilter filter) =>
            AllRows(_roles.GetAll(), r => r.NameAr);

        protected override int IdOf(Role item) => item.Id;
        protected override Result DeleteItem(int id) => _roles.Delete(id);
    }

    public class UsersViewModel : CrudViewModelBase<UserDto, UserFilter>
    {
        private readonly IUserService _users;

        public UsersViewModel(IUserService users, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _users = users;

        protected override string PermissionPrefix => "Users";

        protected override Result<PagedResult<UserDto>> FetchPage(int page, int pageSize, UserFilter filter) =>
            AllRows(_users.GetAll(), u => u.Username, u => u.DisplayName);

        protected override int IdOf(UserDto item) => item.Id;
        protected override Result DeleteItem(int id) => _users.Delete(id);
    }

    public class TreasuriesViewModel : CrudViewModelBase<Treasury, TreasuryFilter>
    {
        private readonly ITreasuryService _treasuries;

        public TreasuriesViewModel(ITreasuryService treasuries, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _treasuries = treasuries;

        protected override string PermissionPrefix => "Treasuries";

        protected override Result<PagedResult<Treasury>> FetchPage(int page, int pageSize, TreasuryFilter filter) =>
            AllRows(_treasuries.GetAll(), t => t.Name, t => t.Code);

        protected override int IdOf(Treasury item) => item.Id;
        protected override Result DeleteItem(int id) => _treasuries.Delete(id);
    }

    public class AssetsViewModel : CrudViewModelBase<Asset, AssetFilter>
    {
        private readonly IAssetService _assets;

        public AssetsViewModel(IAssetService assets, IPermissionService permissions,
                                IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _assets = assets;

        protected override string PermissionPrefix => "Assets";

        protected override Result<PagedResult<Asset>> FetchPage(int page, int pageSize, AssetFilter filter)
        {
            var f = filter ?? new AssetFilter();
            f.SearchText = SearchText;
            return _assets.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(Asset item) => item.Id;

        protected override Result DeleteItem(int id) => _assets.Delete(id);
    }
}
