using PrimeERP.Application.DTOs.HR;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.HR
{
    public interface IEmployeeService
    {
        Result<PagedResult<EmployeeDto>> GetPaged(int page, int pageSize, EmployeeFilter filter = null);
        Result<EmployeeDto> GetById(int id);
        Result<EmployeeDto> Create(CreateEmployeeDto dto);
        Result Update(UpdateEmployeeDto dto);
        Result Delete(int id);
    }
}
