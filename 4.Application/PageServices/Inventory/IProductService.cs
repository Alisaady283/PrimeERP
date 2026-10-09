using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.PageServices.Inventory
{
    /// <summary>عقد الأصناف</summary>
    public interface IProductService
    {
        Result<PagedResult<Product>> GetPaged(int page, int pageSize, ProductFilter filter = null);
        Result<Product> GetById(int id);
        Result<Product> GetByCode(string code);
        Result<Product> Create(Product product);
        Result Update(Product product);
        Result Delete(int id);
    }
}
