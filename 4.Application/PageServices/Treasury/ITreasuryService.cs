using System.Collections.Generic;
using PrimeERP.Domain.Results;
using Entity = PrimeERP.Domain.Entities.Treasury;

namespace PrimeERP.Application.PageServices.Treasury
{
    /// <summary>عقد الخزائن والبنوك</summary>
    public interface ITreasuryService
    {
        Result<List<Entity>> GetAll(bool includeInactive = false);
        Result<List<Entity>> GetUsable();
        Result<Entity> GetById(int id);
        Result<Entity> Create(Entity treasury);
        Result Update(Entity treasury);
        Result Delete(int id);
        Result SeedDefaults();
        Result RepairMissingAccounts();
    }
}
