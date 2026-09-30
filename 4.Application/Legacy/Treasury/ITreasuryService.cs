using System.Collections.Generic;
using PrimeERP.Application.DTOs.Treasury;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Legacy.Treasury
{
    /// <summary>عقد الخزائن والبنوك</summary>
    public interface ITreasuryService
    {
        Result<List<TreasuryDto>> GetAll(bool includeInactive = false);
        Result<TreasuryDto> GetById(int id);
        Result<TreasuryDto> Create(CreateTreasuryDto dto);
        Result Update(UpdateTreasuryDto dto);
        Result Delete(int id);
        Result SeedDefaults();
        Result RepairMissingAccounts();
    }
}
