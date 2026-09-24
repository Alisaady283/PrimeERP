using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع Journal</summary>
    public interface IJournalRepository
    {
        JournalEntry GetById(int id, PrimeDbContext db = null);
        JournalEntry GetByEntryNo(string entryNo);
        List<JournalLine> GetLines(int entryId, PrimeDbContext db = null);
        bool IsPosted(int entryId);
        bool HasLinesForAccount(string accountCode, int? exceptEntryId = null);
        List<(string EntryDate, string EntryNo, string Description, decimal Debit, decimal Credit)> GetPostedLinesForAccount(
            string accountCode, DateTime? from, DateTime? to, PrimeDbContext db = null);
        (int TotalEntries, decimal TotalDebit, decimal TotalCredit) GetSummary();
        int CountUnpostedBetween(DateTime from, DateTime to);
        List<(string AccountCode, decimal SumDebit, decimal SumCredit)> GetAccountSums(DateTime? from, DateTime to, bool postedOnly);
        Dictionary<int, int> GetLineCounts(IEnumerable<int> entryIds);
        (List<JournalEntry> Items, int Total) GetPaged(
            int page, int pageSize,
            string searchText = null, DateTime? dateFrom = null, DateTime? dateTo = null, string source = null,
            bool? isPosted = null, string accountCode = null, decimal? minAmount = null, decimal? maxAmount = null,
            string sortColumn = "EntryDate", bool sortDescending = true);

        int InsertHeader(PrimeDbContext db, JournalEntry entry);
        void UpdateEntry(PrimeDbContext db, int id, string entryDate, string description);
        void InsertLine(PrimeDbContext db, int entryId, int lineNo, JournalLine line);
        void UpdateTotals(PrimeDbContext db, int entryId, decimal totalDebit, decimal totalCredit);
        void DeleteLines(int entryId, PrimeDbContext db = null);
        void DeleteHeader(int entryId, PrimeDbContext db = null);
        void SetPosted(int entryId, bool isPosted);
        void SetPosted(PrimeDbContext db, int entryId, DateTime postedAt, string postedBy);
        void SetUnposted(PrimeDbContext db, int entryId);
    }

    /// <summary>طبقة وصول بيانات قيود اليومية</summary>
    public class JournalRepository : RepositoryBase<JournalEntry>, IJournalRepository
    {
        protected override string TableName => "JournalEntries";





        private const string LinesOf = "JournalEntryLines";

        public JournalEntry GetByEntryNo(string entryNo) => One(q => q.Where(e => e.EntryNo == entryNo));

        public List<JournalLine> GetLines(int entryId, PrimeDbContext db = null) =>
            FetchOf<JournalLine>(LinesOf, q => q.Where(l => l.EntryId == entryId).OrderBy(l => l.LineNo), db);

        public bool IsPosted(int entryId) => One(q => q.Where(e => e.Id == entryId))?.IsPosted ?? false;

        public bool HasLinesForAccount(string accountCode, int? exceptEntryId = null)
        {
            using var db = DbContextFactory.Open();
            return RowsOf<JournalLine>(db, LinesOf).AsNoTracking()
                .Any(l => l.AccountCode == accountCode && (exceptEntryId == null || l.EntryId != exceptEntryId));
        }

        public List<(string EntryDate, string EntryNo, string Description, decimal Debit, decimal Credit)>
            GetPostedLinesForAccount(string accountCode, DateTime? from, DateTime? to,
                                     PrimeDbContext db = null)
        {
            var since = from?.ToString("yyyy-MM-dd");
            var until = to?.ToString("yyyy-MM-dd");

            return Scope(db, ctx =>
                   (from l in RowsOf<JournalLine>(ctx, LinesOf).AsNoTracking()
                    join e in Rows(ctx).AsNoTracking() on l.EntryId equals e.Id
                    where l.AccountCode == accountCode && e.IsPosted
                       && (since == null || string.Compare(e.EntryDate, since) >= 0)
                       && (until == null || string.Compare(e.EntryDate, until) <= 0)
                    orderby e.EntryDate, l.LineNo, e.EntryNo, e.CreatedAt, e.Id
                    select new { e.EntryDate, e.EntryNo, e.Description, l.Debit, l.Credit })
                .AsEnumerable()
                .Select(x => (x.EntryDate, x.EntryNo, x.Description ?? "", x.Debit, x.Credit))
                .ToList());
        }

        public (int TotalEntries, decimal TotalDebit, decimal TotalCredit) GetSummary()
        {
            using var db = DbContextFactory.Open();
            var rows = Rows(db).AsNoTracking();
            return (rows.Count(), rows.Sum(e => (decimal?)e.TotalDebit) ?? 0m, rows.Sum(e => (decimal?)e.TotalCredit) ?? 0m);
        }

        public int CountUnpostedBetween(DateTime from, DateTime to)
        {
            var since = from.ToString("yyyy-MM-dd");
            var until = to.ToString("yyyy-MM-dd");
            return Count(q => q.Where(e => !e.IsPosted
                                        && string.Compare(e.EntryDate, since) >= 0
                                        && string.Compare(e.EntryDate, until) <= 0));
        }

        public List<(string AccountCode, decimal SumDebit, decimal SumCredit)> GetAccountSums(
            DateTime? from, DateTime to, bool postedOnly)
        {
            var since = from?.ToString("yyyy-MM-dd");
            var until = to.ToString("yyyy-MM-dd");

            using var db = DbContextFactory.Open();
            return (from l in RowsOf<JournalLine>(db, LinesOf).AsNoTracking()
                    join e in Rows(db).AsNoTracking() on l.EntryId equals e.Id
                    where string.Compare(e.EntryDate, until) <= 0
                       && (since == null || string.Compare(e.EntryDate, since) >= 0)
                       && (!postedOnly || e.IsPosted)
                    group l by l.AccountCode into g
                    select new { Code = g.Key, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
                .AsEnumerable()
                .Select(x => (x.Code, x.Debit, x.Credit))
                .ToList();
        }

        public Dictionary<int, int> GetLineCounts(IEnumerable<int> entryIds)
        {
            var ids = entryIds.ToList();
            if (ids.Count == 0) return new Dictionary<int, int>();

            using var db = DbContextFactory.Open();
            return RowsOf<JournalLine>(db, LinesOf).AsNoTracking()
                .Where(l => ids.Contains(l.EntryId))
                .GroupBy(l => l.EntryId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionary(x => x.Key, x => x.Count);
        }

        public (List<JournalEntry> Items, int Total) GetPaged(
            int page, int pageSize,
            string searchText = null, DateTime? dateFrom = null, DateTime? dateTo = null, string source = null,
            bool? isPosted = null, string accountCode = null, decimal? minAmount = null, decimal? maxAmount = null,
            string sortColumn = "EntryDate", bool sortDescending = true)
        {
            var since = dateFrom?.ToString("yyyy-MM-dd");
            var until = dateTo?.ToString("yyyy-MM-dd");

            using var db = DbContextFactory.Open();
            var q = Rows(db).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchText))
                q = q.Where(e => EF.Functions.Like(e.EntryNo, $"%{searchText}%")
                              || EF.Functions.Like(e.Description, $"%{searchText}%"));
            if (since != null) q = q.Where(e => string.Compare(e.EntryDate, since) >= 0);
            if (until != null) q = q.Where(e => string.Compare(e.EntryDate, until) <= 0);
            if (!string.IsNullOrWhiteSpace(source)) q = q.Where(e => e.Source == source);
            if (isPosted != null) q = q.Where(e => e.IsPosted == isPosted);
            if (!string.IsNullOrWhiteSpace(accountCode))
                q = q.Where(e => RowsOf<JournalLine>(db, LinesOf).Any(l => l.EntryId == e.Id && l.AccountCode == accountCode));
            if (minAmount != null) q = q.Where(e => e.TotalDebit >= minAmount);
            if (maxAmount != null) q = q.Where(e => e.TotalDebit <= maxAmount);

            var total = q.Count();
            var ordered = (sortColumn switch
            {
                "EntryNo"     => By(e => e.EntryNo,     sortDescending),
                "TotalDebit"  => By(e => e.TotalDebit,  sortDescending),
                "TotalCredit" => By(e => e.TotalCredit, sortDescending),
                "IsPosted"    => By(e => e.IsPosted,    sortDescending),
                "CreatedAt"   => By(e => e.CreatedAt,   sortDescending),
                _             => By(e => e.EntryDate,   sortDescending),
            })(q);

            var items = ordered.ThenByDescending(e => e.Id).ThenByDescending(e => e.EntryNo)
                               .ThenByDescending(e => e.CreatedAt)
                               .Skip(Math.Max(0, page - 1) * pageSize).Take(pageSize).ToList();
            return (items, total);
        }

        public int InsertHeader(PrimeDbContext db, JournalEntry entry)
        {
            entry.Source ??= "يدوي";
            entry.Description ??= "";
            return Add(entry, db);
        }

        public void UpdateEntry(PrimeDbContext db, int id, string entryDate, string description) =>
            Edit(e => e.Id == id, row =>
            {
                row.EntryDate = entryDate;
                row.Description = description ?? "";
            }, db);

        public void InsertLine(PrimeDbContext db, int entryId, int lineNo, JournalLine line) =>
            Write(db =>
            {
                line.EntryId = entryId;
                line.LineNo = lineNo;
                line.AccountName ??= "";
                line.Notes ??= "";
                SetOf<JournalLine>(db, LinesOf).Add(line);
                return 0;
            }, db);

        public void UpdateTotals(PrimeDbContext db, int entryId, decimal totalDebit, decimal totalCredit) =>
            Edit(e => e.Id == entryId, row =>
            {
                row.TotalDebit = totalDebit;
                row.TotalCredit = totalCredit;
            }, db);

        public void DeleteLines(int entryId, PrimeDbContext db = null) =>
            Write(db =>
            {
                SetOf<JournalLine>(db, LinesOf)
                    .RemoveRange(RowsOf<JournalLine>(db, LinesOf).Where(l => l.EntryId == entryId));
                return 0;
            }, db);

        public void DeleteHeader(int entryId, PrimeDbContext db = null) =>
            Write(db =>
            {
                var row = Rows(db).AsTracking().FirstOrDefault(e => e.Id == entryId);
                if (row != null) SetOf(db).Remove(row);
                return 0;
            }, db);

        public void SetPosted(int entryId, bool isPosted) =>
            Write(db =>
            {
                var row = Rows(db).AsTracking().FirstOrDefault(e => e.Id == entryId);
                if (row == null) return 0;
                row.IsPosted = isPosted;
                row.UpdatedAt = DateTime.Now;
                return 0;
            });

        public void SetPosted(PrimeDbContext db, int entryId, DateTime postedAt, string postedBy) =>
            Edit(e => e.Id == entryId, row =>
            {
                row.IsPosted = true;
                row.PostedAt = postedAt;
                row.PostedBy = postedBy;
            }, db);

        public void SetUnposted(PrimeDbContext db, int entryId) =>
            Edit(e => e.Id == entryId, row =>
            {
                row.IsPosted = false;
                row.PostedAt = null;
                row.PostedBy = null;
            }, db);
    }
}
