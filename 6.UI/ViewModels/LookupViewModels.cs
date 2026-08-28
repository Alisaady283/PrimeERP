using PrimeERP.Application.Services.Common;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.ViewModels
{
    // كل وحدة أدناه = CategoryListViewModel بموديول-كي وصلاحية مختلفَين فقط — لا منطق إضافي.
    // Departments/JobTitles/Units/Warehouses انتقلت لكيانات Domain مخصصة موجودة مسبقاً (أغنى من Category
    // العام — Warehouse فيه Code/Location، Unit فيه Symbol)، راجع DepartmentsViewModel/JobTitlesViewModel/
    // UnitsViewModel/WarehousesViewModel في ملفاتها الخاصة.
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

    public class AssetCategoriesViewModel : CategoryListViewModel
    {
        public AssetCategoriesViewModel(ICategoryService c, IPermissionService p, IToastService t, IDialogService d)
            : base("AssetCategories", "AssetCategories", c, p, t, d) { }
    }
}
