using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.PageServices.Inventory
{
    /// <summary>عقد التحويل المخزني</summary>
    public interface IStockTransferService
    {
        Result<PagedResult<StockTransferDto>> GetPaged(int page, int pageSize, StockTransferFilter filter = null);
        Result<StockTransferDetailDto> GetById(int id);
        Result<StockTransferDetailDto> Create(CreateStockTransferDto dto);
        Result Update(CreateStockTransferDto dto);
        Result Delete(int id);
    }
}
