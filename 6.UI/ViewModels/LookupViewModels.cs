using PrimeERP.Application.Services.Common;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.ViewModels
{
    // كل وحدة أدناه = CategoryListViewModel بموديول-كي وصلاحية مختلفَين فقط — لا منطق إضافي.
    public class CategoriesLookupViewModel : CategoryListViewModel
    {
        public CategoriesLookupViewModel(ICategoryService c, IPermissionService p, IToastService t, IDialogService d)
            : base("Products", "Categories", c, p, t, d) { }
    }

    public class BrandsViewModel : CategoryListViewModel
    {
        public BrandsViewModel(ICategoryService c, IPermissionService p, IToastService t, IDialogService d)
            : base("Brands", "Brands", c, p, t, d) { }
    }

    public class UnitsViewModel : CategoryListViewModel
    {
        public UnitsViewModel(ICategoryService c, IPermissionService p, IToastService t, IDialogService d)
            : base("Units", "Units", c, p, t, d) { }
    }

    public class WarehousesViewModel : CategoryListViewModel
    {
        public WarehousesViewModel(ICategoryService c, IPermissionService p, IToastService t, IDialogService d)
            : base("Warehouses", "Warehouses", c, p, t, d) { }
    }

    public class AssetCategoriesViewModel : CategoryListViewModel
    {
        public AssetCategoriesViewModel(ICategoryService c, IPermissionService p, IToastService t, IDialogService d)
            : base("AssetCategories", "AssetCategories", c, p, t, d) { }
    }

    public class DepartmentsViewModel : CategoryListViewModel
    {
        public DepartmentsViewModel(ICategoryService c, IPermissionService p, IToastService t, IDialogService d)
            : base("Departments", "Departments", c, p, t, d) { }
    }

    public class JobTitlesViewModel : CategoryListViewModel
    {
        public JobTitlesViewModel(ICategoryService c, IPermissionService p, IToastService t, IDialogService d)
            : base("JobTitles", "JobTitles", c, p, t, d) { }
    }
}
