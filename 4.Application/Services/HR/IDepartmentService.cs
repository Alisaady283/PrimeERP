using System.Collections.Generic;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.HR
{
    /// <summary>عقد الأقسام</summary>
    public interface IDepartmentService
    {
        Result<List<DepartmentDto>> GetAll(bool includeInactive = false);
        Result<DepartmentDto> Create(CreateDepartmentDto dto);
        Result Update(UpdateDepartmentDto dto);
        Result Delete(int id);
    }
}
