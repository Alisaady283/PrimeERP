using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Inventory
{
    public interface IStockInService
    {
        Result<PagedResult<StockAdjustmentDto>> GetPaged(int page, int pageSize, StockAdjustmentFilter filter = null);
        Result<StockAdjustmentDetailDto> GetById(int id);
        Result<StockAdjustmentDetailDto> Create(CreateStockAdjustmentDto dto);
        Result Update(CreateStockAdjustmentDto dto);
        Result Delete(int id);
    }

    public interface IStockOutService
    {
        Result<PagedResult<StockAdjustmentDto>> GetPaged(int page, int pageSize, StockAdjustmentFilter filter = null);
        Result<StockAdjustmentDetailDto> GetById(int id);
        Result<StockAdjustmentDetailDto> Create(CreateStockAdjustmentDto dto);
        Result Update(CreateStockAdjustmentDto dto);
        Result Delete(int id);
    }
}
