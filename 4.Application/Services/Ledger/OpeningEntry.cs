using PrimeERP.Domain.Calculations;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>القيد الافتتاحي متعدّد الأسطر</summary>
    public static class OpeningEntry
    {
        public static Result<CreateJournalDto> Prepare(CreateJournalDto dto, DateTime startDate, string source, string description)
        {
            dto.Source = source;
            dto.Description = description;
            dto.EntryDate = startDate;
            dto.Lines = dto.Lines?.Where(l => !string.IsNullOrWhiteSpace(l.AccountCode)).ToList() ?? new List<CreateJournalLineDto>();

            var (debit, credit) = (dto.Lines.Sum(l => l.Debit), dto.Lines.Sum(l => l.Credit));
            return debit == credit
                ? Result.Ok(dto)
                : Result.Fail<CreateJournalDto>(LocalizationService.Get("Str.Journal.Unbalanced", debit, credit, Math.Abs(debit - credit)),
                    ErrorCode.ValidationFailed);
        }
    }
}
