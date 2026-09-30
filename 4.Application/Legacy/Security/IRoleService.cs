using System.Collections.Generic;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Legacy.Security
{
    /// <summary>عقد الأدوار</summary>
    public interface IRoleService
    {
        Result<List<Role>> GetAll();
        Result<Role> Create(Role role);
        Result Update(Role role);
        Result Delete(int id);
    }
}
