using System.Collections.Generic;
using PrimeERP.Application.DTOs.Security;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Security
{
    /// <summary>عقد الأدوار</summary>
    public interface IRoleService
    {
        Result<List<RoleDto>> GetAll();
        Result<RoleDto> Create(CreateRoleDto dto);
        Result Update(UpdateRoleDto dto);
        Result Delete(int id);
    }
}
