using PrimeERP.Application.DTOs.HR;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Legacy.HR
{
    /// <summary>عقد الموظفين</summary>
    public interface IEmployeeService
    {
        Result<PagedResult<Employee>> GetPaged(int page, int pageSize, EmployeeFilter filter = null);
        Result<Employee> GetById(int id);
        Result<Employee> Create(Employee employee);
        Result Update(Employee employee);
        Result Delete(int id);
    }
}
