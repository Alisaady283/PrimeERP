using System.Collections.Generic;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Inventory
{
    public interface IUnitService
    {
        Result<List<UnitDto>> GetAll(bool includeInactive = false);
        Result<UnitDto> Create(CreateUnitDto dto);
        Result Update(UpdateUnitDto dto);
        Result Delete(int id);
    }
}
