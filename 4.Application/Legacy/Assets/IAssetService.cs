using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Legacy.Assets
{
    /// <summary>عقد خدمة الأصول</summary>
    public interface IAssetService
    {
        Result<PagedResult<AssetDto>> GetPaged(int page, int pageSize, AssetFilter filter = null);
        Result<AssetDto> GetById(int id);
        Result<AssetDto> Create(CreateAssetDto dto);
        Result Update(UpdateAssetDto dto);
        Result Delete(int id);
        Result SeedDefaults();
    }
}
