using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>القيد يُنشأ مُرحَّلاً ويُعكس بحذفه</summary>
    public static class Posting
    {
        public static int Entry(Entries entries, PrimeDbContext db, DateTime date, string description,
            string source, List<CreateJournalLineDto> lines)
        {
            var entry = entries.Create(db, new CreateJournalDto
            {
                EntryDate = date, Description = description, Source = source, Lines = lines
            });
            if (entry.IsFailure) throw new InvalidOperationException(entry.ErrorMessage);

            entries.MarkPosted(db, entry.Value.Id, AppSession.Username ?? "Admin");
            return entry.Value.Id;
        }

        /// <summary>قيدٌ بطرفين</summary>
        public static int Entry(Entries entries, PrimeDbContext db, DateTime date, string description,
            string source, string debitAccount, string creditAccount, decimal amount, string lineNote = null) =>
            Entry(entries, db, date, description, source, new JournalLines()
                .Debit(debitAccount, amount, lineNote ?? description)
                .Credit(creditAccount, amount, lineNote ?? description)
                .ToList());

        public static Result EnsureReversible(Entries entries, int? entryId) =>
            entryId == null ? Result.Ok() : entries.CanRemove(entryId.Value);

        public static void Reverse(Entries entries, PrimeDbContext db, int? entryId)
        {
            if (entryId != null) entries.Delete(db, entryId.Value);
        }
    }
}
