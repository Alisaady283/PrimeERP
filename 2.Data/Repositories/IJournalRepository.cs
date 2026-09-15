using System;
using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IJournalRepository
    {
        void CreateTable();
        JournalEntry GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        JournalEntry GetByEntryNo(string entryNo);
        List<JournalLine> GetLines(int entryId, DbConnection conn = null, DbTransaction tx = null);
        bool IsPosted(int entryId);
        bool HasLinesForAccount(string accountCode, int? exceptEntryId = null);
        List<(string EntryDate, string EntryNo, string Description, decimal Debit, decimal Credit)> GetPostedLinesForAccount(
            string accountCode, DateTime? from, DateTime? to, DbConnection conn = null, DbTransaction tx = null);
        (int TotalEntries, decimal TotalDebit, decimal TotalCredit) GetSummary();
        int CountUnpostedBetween(DateTime from, DateTime to);
        List<(string AccountCode, decimal SumDebit, decimal SumCredit)> GetAccountSums(DateTime? from, DateTime to, bool postedOnly);
        Dictionary<int, int> GetLineCounts(IEnumerable<int> entryIds);
        (List<JournalEntry> Items, int Total) GetPaged(
            int page, int pageSize,
            string searchText = null, DateTime? dateFrom = null, DateTime? dateTo = null, string source = null,
            bool? isPosted = null, string accountCode = null, decimal? minAmount = null, decimal? maxAmount = null,
            string sortColumn = "EntryDate", bool sortDescending = true);

        int InsertHeader(DbConnection conn, DbTransaction tx, JournalEntry entry);
        void UpdateEntry(DbConnection conn, DbTransaction tx, int id, string entryDate, string description);
        void InsertLine(DbConnection conn, DbTransaction tx, int entryId, int lineNo, JournalLine line);
        void UpdateTotals(DbConnection conn, DbTransaction tx, int entryId, decimal totalDebit, decimal totalCredit);
        void DeleteLines(int entryId, DbConnection conn = null, DbTransaction tx = null);
        void DeleteHeader(int entryId, DbConnection conn = null, DbTransaction tx = null);
        void SetPosted(int entryId, bool isPosted);
        void SetPosted(DbConnection conn, DbTransaction tx, int entryId, DateTime postedAt, string postedBy);
        void SetUnposted(DbConnection conn, DbTransaction tx, int entryId);
    }
}
