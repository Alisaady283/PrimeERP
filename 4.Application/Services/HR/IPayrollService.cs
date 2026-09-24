using PrimeERP.Application.DTOs.HR;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.HR
{
    /// <summary>عقد مسير الرواتب</summary>
    public interface IPayrollService
    {
        Result<PagedResult<PayrollDto>> GetPaged(int page, int pageSize, PayrollFilter filter = null);
        Result<PayrollDetailDto> GetById(int id);
        Result<PayrollDetailDto> Create(CreatePayrollDto dto);
        Result Update(CreatePayrollDto dto);
        Result Delete(int id);

        Result Post(int id);
        Result Unpost(int id);
    }
}
