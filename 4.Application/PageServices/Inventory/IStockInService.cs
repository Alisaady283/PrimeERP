using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.PageServices.Inventory
{
    /// <summary>عقود أذون المخزون الستة</summary>
    public interface IStockInService
    {
        Result<PagedResult<StockAdjustmentDto>> GetPaged(int page, int pageSize, StockAdjustmentFilter filter = null);
        Result<StockAdjustmentDetailDto> GetById(int id);
        Result<StockAdjustmentDetailDto> Create(CreateStockAdjustmentDto dto);
        Result Update(CreateStockAdjustmentDto dto);
        Result Delete(int id);
    }

    public interface IStockOutService : IStockInService { }

    public interface IGoodsReceiptService : IStockInService { }
    public interface IGoodsIssueService : IStockInService { }
    public interface IDeliveryNoteService : IStockInService { }
    public interface ISalesReceiptService : IStockInService { }
}
