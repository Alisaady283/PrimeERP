using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class ProductsViewModel : CrudViewModelBase<ProductDto, ProductFilter>
    {
        private readonly IProductService _products;

        public ProductsViewModel(IProductService products, IPermissionService permissions,
                                  IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _products = products;

        protected override string PermissionPrefix => "Products";

        protected override Result<PagedResult<ProductDto>> FetchPage(int page, int pageSize, ProductFilter filter)
        {
            var f = filter ?? new ProductFilter();
            f.SearchText = SearchText;
            return _products.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(ProductDto item) => item.Id;

        protected override Result DeleteItem(int id) => _products.Delete(id);
    }
}
