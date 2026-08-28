using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Purchasing
{
    public interface IPurchaseInvoiceService
    {
        Result<PagedResult<PurchaseInvoiceDto>> GetPaged(int page, int pageSize, PurchaseInvoiceFilter filter = null);
        Result<PurchaseInvoiceDetailDto> GetById(int id);
        Result<PurchaseInvoiceDetailDto> Create(CreatePurchaseInvoiceDto dto);
        Result Update(CreatePurchaseInvoiceDto dto);
        Result Delete(int id);
    }
}
