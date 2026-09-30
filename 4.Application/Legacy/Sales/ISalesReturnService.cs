using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Legacy.Sales
{
    /// <summary>عقد مرتجع البيع</summary>
    public interface ISalesReturnService
    {
        Result<PagedResult<SalesReturnDto>> GetPaged(int page, int pageSize, SalesReturnFilter filter = null);
        Result<SalesReturnDetailDto> GetById(int id);
        Result<SalesReturnDetailDto> Create(CreateSalesReturnDto dto);
        Result Update(CreateSalesReturnDto dto);
        Result Delete(int id);
    }
}
