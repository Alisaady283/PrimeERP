using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Legacy.Purchasing
{
    /// <summary>عقد مرتجع الشراء</summary>
    public interface IPurchaseReturnService
    {
        Result<PagedResult<PurchaseReturnDto>> GetPaged(int page, int pageSize, PurchaseReturnFilter filter = null);
        Result<PurchaseReturnDetailDto> GetById(int id);
        Result<PurchaseReturnDetailDto> Create(CreatePurchaseReturnDto dto);
        Result Update(CreatePurchaseReturnDto dto);
        Result Delete(int id);
    }
}
