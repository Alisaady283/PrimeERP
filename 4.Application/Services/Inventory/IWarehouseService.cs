using System.Collections.Generic;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Inventory
{
    /// <summary>عقد المخازن</summary>
    public interface IWarehouseService
    {
        Result<List<WarehouseDto>> GetAll(bool includeInactive = false);
        Result<WarehouseDto> Create(CreateWarehouseDto dto);
        Result Update(UpdateWarehouseDto dto);
        Result Delete(int id);
    }
}
