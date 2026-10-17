using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.PageServices.Assets
{
    /// <summary>عقد خدمة الأصول</summary>
    public interface IAssetService
    {
        Result<PagedResult<Asset>> GetPaged(int page, int pageSize, AssetFilter filter = null);
        Result<Asset> GetById(int id);
        Result<Asset> Create(Asset asset);
        Result Update(Asset asset);
        Result Delete(int id);
        Result SeedDefaults();
    }
}
