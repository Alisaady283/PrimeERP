using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>القيد الافتتاحي متعدّد الأسطر</summary>
    public static class OpeningEntry
    {
        public static CreateJournalDto Prepare(CreateJournalDto dto, DateTime startDate, string source, string description)
        {
            dto.Source = source;
            dto.Description = description;
            dto.EntryDate = startDate;
            dto.Lines = dto.Lines?.Where(l => !string.IsNullOrWhiteSpace(l.AccountCode)).ToList() ?? new List<CreateJournalLineDto>();
            return dto;
        }
    }
}
