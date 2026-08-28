using System.Collections.Generic;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.HR
{
    public interface IJobTitleService
    {
        Result<List<JobTitleDto>> GetAll(bool includeInactive = false);
        Result<JobTitleDto> Create(CreateJobTitleDto dto);
        Result Update(UpdateJobTitleDto dto);
        Result Delete(int id);
    }
}
