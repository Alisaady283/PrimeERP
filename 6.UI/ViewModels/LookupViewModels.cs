using PrimeERP.Application.Services.Common;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>نماذج عرض CategoriesLookupViewModel</summary>
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
