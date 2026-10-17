using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.PageServices.Sales
{
    /// <summary>عقد فاتورة البيع</summary>
    public interface ISalesInvoiceService
    {
        Result<PagedResult<SalesInvoiceDto>> GetPaged(int page, int pageSize, SalesInvoiceFilter filter = null);
        Result<SalesInvoiceDetailDto> GetById(int id);
        Result<SalesInvoiceDetailDto> Create(CreateSalesInvoiceDto dto);
        Result Update(CreateSalesInvoiceDto dto);
        Result Delete(int id);
    }
}
