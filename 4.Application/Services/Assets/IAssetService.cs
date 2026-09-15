using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Assets
{
    public interface IAssetService
    {
        Result<PagedResult<AssetDto>> GetPaged(int page, int pageSize, AssetFilter filter = null);
        Result<AssetDto> GetById(int id);
        Result<AssetDto> Create(CreateAssetDto dto);
        Result Update(UpdateAssetDto dto);
        Result Delete(int id);

        /// <summary>يُرحّل قيد اقتناءٍ غائب لأصلٍ سبق وجود الترحيل — تستعمله تسوية الإقلاع.</summary>
        Result PostMissingAcquisition(PrimeERP.Domain.Entities.Asset asset);
    }
}
