using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services
{
    /// <summary>القيد يُنشأ مُرحَّلاً ويُعكس بحذفه</summary>
    public static class Posting
    {
        public static int Entry(IJournalService journals, PrimeDbContext db, DateTime date, string description,
            string source, List<CreateJournalLineDto> lines)
        {
            var entry = journals.Create(db, new CreateJournalDto
            {
                EntryDate = date, Description = description, Source = source, Lines = lines
            });
            if (entry.IsFailure) throw new InvalidOperationException(entry.ErrorMessage);

            var posted = journals.Post(db, entry.Value.Id);
            if (posted.IsFailure) throw new InvalidOperationException(posted.ErrorMessage);

            return entry.Value.Id;
        }

        /// <summary>قيدٌ بطرفين</summary>
        public static int Entry(IJournalService journals, PrimeDbContext db, DateTime date, string description,
            string source, string debitAccount, string creditAccount, decimal amount, string lineNote = null) =>
            Entry(journals, db, date, description, source, new List<CreateJournalLineDto>
            {
                new() { LineNo = 1, AccountCode = debitAccount,  Debit  = amount, Notes = lineNote ?? description },
                new() { LineNo = 2, AccountCode = creditAccount, Credit = amount, Notes = lineNote ?? description }
            });

        public static Result EnsureReversible(IJournalService journals, int? entryId) =>
            entryId == null ? Result.Ok() : journals.EnsureRemovable(entryId.Value);

        public static void Reverse(IJournalService journals, PrimeDbContext db, int? entryId)
        {
            if (entryId != null) journals.Delete(db, entryId.Value);
        }
    }
}
