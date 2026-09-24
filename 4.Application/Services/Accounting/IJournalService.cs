using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Domain.Results;
using PrimeERP.Application.DTOs.Accounting;

namespace PrimeERP.Application.Services.Accounting
{
    /// <summary>المالك الوحيد لمنطق قيود اليومية</summary>
    public interface IJournalService
    {
        Result<PagedResult<JournalEntryDto>> GetPaged(int page, int pageSize, JournalFilter filter = null);
        Result<JournalEntryDetailDto> GetById(int id);
        Result<JournalEntryDto> GetByEntryNo(string entryNo);

        Result<JournalEntryDto> Create(CreateJournalDto dto);
        Result<JournalEntryDto> Create(PrimeDbContext db, CreateJournalDto dto);

        Result Update(CreateJournalDto dto);
        Result Delete(int id);

        Result UpdateOwned(CreateJournalDto dto, string ownerSource);
        Result DeleteOwned(int id, string ownerSource);
        Result Delete(PrimeDbContext db, int id);

        Result EnsureRemovable(int entryId);
        Result EnsureAffordable(IEnumerable<CreateJournalLineDto> lines);
        Result EnsureReplaceable(int entryId, IEnumerable<CreateJournalLineDto> lines);

        Result Post(int id);
        Result Post(PrimeDbContext db, int id);
        Result Unpost(int id);
        Result Unpost(PrimeDbContext db, int id);
        Result<JournalBatchResult> PostBatch(List<int> ids);

        Result<List<TrialBalanceLine>> GetTrialBalance(DateTime from, DateTime to, bool includeZero = false, bool postedOnly = true);

        Result<int> CountUnpostedBetween(DateTime from, DateTime to);
    }
}
