using System;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.PageServices.HR
{
    /// <summary>عقد مسير الرواتب</summary>
    public interface IPayrollService
    {
        Result<PagedResult<PayrollDto>> GetPaged(int page, int pageSize, PayrollFilter filter = null);
        Result<PayrollDetailDto> GetById(int id);
        Result<CreatePayrollDto> Open();
        Result<CreatePayrollDto> Open(int month, int year);
        Result<PayrollDetailDto> Create(CreatePayrollDto dto);
        Result Update(CreatePayrollDto dto);
        Result Delete(int id);
    }
}
