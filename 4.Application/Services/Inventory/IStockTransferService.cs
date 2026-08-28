using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Inventory
{
    public interface IStockTransferService
    {
        Result<PagedResult<StockTransferDto>> GetPaged(int page, int pageSize, StockTransferFilter filter = null);
        Result<StockTransferDetailDto> GetById(int id);
        Result<StockTransferDetailDto> Create(CreateStockTransferDto dto);
        Result Update(CreateStockTransferDto dto);
        Result Delete(int id);
    }
}
