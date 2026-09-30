using System.Collections.Generic;
using PrimeERP.Application.DTOs.Security;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Legacy.Security
{
    /// <summary>عقد المستخدمين</summary>
    public interface IUserService
    {
        Result<List<UserDto>> GetAll();
        Result<UserDto> Create(CreateUserDto dto);
        Result Update(UpdateUserDto dto);
        Result Delete(int id);
    }
}
