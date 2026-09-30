using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Legacy.Inventory
{
    /// <summary>عقد الأصناف</summary>
    public interface IProductService
    {
        Result<PagedResult<ProductDto>> GetPaged(int page, int pageSize, ProductFilter filter = null);
        Result<ProductDto> GetById(int id);
        Result<ProductDto> GetByCode(string code);
        Result<ProductDto> Create(CreateProductDto dto);
        Result Update(UpdateProductDto dto);
        Result Delete(int id);
    }
}
