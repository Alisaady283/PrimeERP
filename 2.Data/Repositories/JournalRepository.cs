using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Data.Query;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>
    /// طبقة وصول بيانات قيود اليومية — SQL خام ↔ Models فقط. بلا معاملات تفتحها هذه الطبقة (الخدمة تفتحها
    /// عبر DbHelper.RunTransaction وتمرّر (conn, tx) للدوال هنا)، بلا حساب إجماليات/أرصدة، بلا توليد رقم،
    /// بلا Audit — كل ذلك مسؤولية IJournalService. يدير كيانين (JournalEntry أساسي عبر RepositoryBase،
    /// JournalLine ثانوي عبر QueryAs) — راجع تعليق RepositoryBase.QueryAs.
    /// </summary>
    public class JournalRepository : RepositoryBase<JournalEntry>, IJournalRepository
    {
        protected override string TableName => "JournalEntries";

        public void CreateTable()
        {
            SchemaBuilder.Table("JournalEntries")
                .Id()
                .Text("EntryNo", 30, required: true, unique: true)
                .Text("EntryDate", 20, required: true)
                .Text("Description", 400)
                .Decimal("TotalDebit")
                .Decimal("TotalCredit")
                .Text("Source", 50)
                .Bool("IsPosted", defaultValue: false)
                .DateCol("PostedAt")
                .Text("PostedBy", 100)
                .Audit()
                .Create();

            SchemaBuilder.Table("JournalEntryLines")
                .Id()
                .Int("EntryId", nullable: false)
                .Int("LineNo", nullable: false, defaultValue: 1)
                .Text("AccountCode", 30, required: true)
                .Text("AccountName", 200)
                .Decimal("Debit")
                .Decimal("Credit")
                .Text("Notes", 400)
                .ForeignKey("EntryId", "JournalEntries", "Id")
                .Index("EntryId")
                .Index("AccountCode")
                .Create();
        }

        protected override JournalEntry Map(DataRow row) => new()
        {
            Id          = Convert.ToInt32(row["Id"]),
            EntryNo     = row["EntryNo"].ToString(),
            EntryDate   = row["EntryDate"].ToString(),
            Description = row["Description"] == DBNull.Value ? "" : row["Description"].ToString(),
            TotalDebit  = Convert.ToDecimal(row["TotalDebit"]),
            TotalCredit = Convert.ToDecimal(row["TotalCredit"]),
            Source      = row["Source"] == DBNull.Value ? "يدوي" : row["Source"].ToString(),
            IsPosted    = Convert.ToBoolean(row["IsPosted"]),
            PostedAt    = row["PostedAt"] == DBNull.Value ? null : Convert.ToDateTime(row["PostedAt"]),
            PostedBy    = row["PostedBy"] == DBNull.Value ? null : row["PostedBy"].ToString(),
            CreatedBy   = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
            CreatedAt   = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            UpdatedAt   = row["UpdatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["UpdatedAt"])
        };

        private static JournalLine MapLine(DataRow row) => new()
        {
            Id          = Convert.ToInt32(row["Id"]),
            EntryId     = Convert.ToInt32(row["EntryId"]),
            LineNo      = Convert.ToInt32(row["LineNo"]),
            AccountCode = row["AccountCode"].ToString(),
            AccountName = row["AccountName"] == DBNull.Value ? null : row["AccountName"].ToString(),
            Debit       = Convert.ToDecimal(row["Debit"]),
            Credit      = Convert.ToDecimal(row["Credit"]),
            Notes       = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString()
        };

        // ===== قراءة =====

        public JournalEntry GetByEntryNo(string entryNo) =>
            QueryOne("SELECT * FROM JournalEntries WHERE EntryNo = @no", null, null, ("@no", entryNo));

        public List<JournalLine> GetLines(int entryId, DbConnection conn = null, DbTransaction tx = null) =>
            QueryAs(MapLine, "SELECT * FROM JournalEntryLines WHERE EntryId = @id ORDER BY LineNo", conn, tx, ("@id", entryId));

        public bool IsPosted(int entryId)
        {
            var result = Scalar("SELECT IsPosted FROM JournalEntries WHERE Id = @id", ("@id", entryId));
            return result != null && Convert.ToBoolean(result);
        }

        /// <summary>
        /// هل يوجد لهذا الحساب أي سطر قيد؟ تستخدمه خدمة الحسابات قبل السماح بحذف حساب.
        /// exceptEntryId يستثني قيد المستند المالك نفسه — يُعكَس مع حذفه فلا يصحّ أن يمنعه.
        /// </summary>
        public bool HasLinesForAccount(string accountCode, int? exceptEntryId = null) =>
            Convert.ToInt64(Scalar(
                "SELECT COUNT(*) FROM JournalEntryLines WHERE AccountCode = @c AND (@e IS NULL OR EntryId <> @e)",
                ("@c", accountCode), ("@e", exceptEntryId))) > 0;

        /// <summary>سطور القيود المرحّلة فقط لحساب مُعيّن (اختيارياً بين تاريخين) — تستخدمه AccountService لحساب الرصيد وكشف الحساب.</summary>
        public List<(string EntryDate, string EntryNo, string Description, decimal Debit, decimal Credit)> GetPostedLinesForAccount(
            string accountCode, DateTime? from, DateTime? to, DbConnection conn = null, DbTransaction tx = null)
        {
            var where = new WhereBuilder()
                .Eq("l.AccountCode", accountCode)
                .Eq("e.IsPosted", true)
                .RawWithParam(p => $"e.EntryDate >= {p}", from?.ToString("yyyy-MM-dd"))
                .RawWithParam(p => $"e.EntryDate <= {p}", to?.ToString("yyyy-MM-dd"));

            var sql = $@"SELECT e.EntryDate, e.EntryNo, e.Description, l.Debit, l.Credit
                         FROM JournalEntryLines l JOIN JournalEntries e ON e.Id = l.EntryId
                         {where.Sql}
                         {OrderBuilder.By("e.EntryDate", false, "l.LineNo", "e.EntryNo", "e.CreatedAt", "e.Id")}";

            return QueryAs(r => (
                r["EntryDate"].ToString(), r["EntryNo"].ToString(),
                r["Description"] == DBNull.Value ? "" : r["Description"].ToString(),
                Convert.ToDecimal(r["Debit"]), Convert.ToDecimal(r["Credit"])),
                sql, conn, tx, where.Parameters);
        }

        public (int TotalEntries, decimal TotalDebit, decimal TotalCredit) GetSummary()
        {
            var row = QueryAs(r => r, @"SELECT COUNT(*) AS TotalEntries,
                         COALESCE(SUM(TotalDebit),0) AS TotalDebit, COALESCE(SUM(TotalCredit),0) AS TotalCredit
                  FROM JournalEntries").First();
            return (Convert.ToInt32(row["TotalEntries"]), Convert.ToDecimal(row["TotalDebit"]), Convert.ToDecimal(row["TotalCredit"]));
        }

        /// <summary>عدد القيود غير المرحّلة بتاريخ بين from وto (شامل الحدّين) — تستخدمه FiscalPeriodService.ClosePeriod.</summary>
        public int CountUnpostedBetween(DateTime from, DateTime to) => Convert.ToInt32(Scalar(
            "SELECT COUNT(*) FROM JournalEntries WHERE IsPosted = @p AND EntryDate >= @f AND EntryDate <= @t",
            ("@p", false), ("@f", from.ToString("yyyy-MM-dd")), ("@t", to.ToString("yyyy-MM-dd"))));

        /// <summary>مجموع مدين/دائن كل حساب ظهر بسطر قيد بين from (اختياري) وto — استعلام GROUP BY واحد، لا حلقة على الحسابات.</summary>
        public List<(string AccountCode, decimal SumDebit, decimal SumCredit)> GetAccountSums(DateTime? from, DateTime to, bool postedOnly)
        {
            var where = new WhereBuilder()
                .RawWithParam(p => $"e.EntryDate <= {p}", to.ToString("yyyy-MM-dd"))
                .RawWithParam(p => $"e.EntryDate >= {p}", from?.ToString("yyyy-MM-dd"))
                .Eq("e.IsPosted", postedOnly ? true : (bool?)null);

            var sql = $@"SELECT l.AccountCode, COALESCE(SUM(l.Debit),0) AS SumDebit, COALESCE(SUM(l.Credit),0) AS SumCredit
                        FROM JournalEntryLines l JOIN JournalEntries e ON e.Id = l.EntryId
                        {where.Sql} GROUP BY l.AccountCode";

            return QueryAs(r => (r["AccountCode"].ToString(), Convert.ToDecimal(r["SumDebit"]), Convert.ToDecimal(r["SumCredit"])),
                sql, null, null, where.Parameters);
        }

        /// <summary>عدد سطور كل قيد من مجموعة معرّفات — استعلام واحد، لا حلقة لكل صف صفحة.</summary>
        public Dictionary<int, int> GetLineCounts(IEnumerable<int> entryIds)
        {
            var ids = entryIds.ToList();
            if (ids.Count == 0) return new Dictionary<int, int>();

            var placeholders = string.Join(",", ids.Select((_, i) => $"@id{i}"));
            var parameters = ids.Select((id, i) => ($"@id{i}", (object)id)).ToArray();

            return QueryAs(r => (Convert.ToInt32(r["EntryId"]), Convert.ToInt32(r["Cnt"])),
                $"SELECT EntryId, COUNT(*) AS Cnt FROM JournalEntryLines WHERE EntryId IN ({placeholders}) GROUP BY EntryId",
                null, null, parameters).ToDictionary(x => x.Item1, x => x.Item2);
        }

        /// <summary>قائمة مُرقَّمة (صفحات) لقيود اليومية مع الفلاتر — sortColumn يُطابَق بقائمة أعمدة مسموحة صراحة (لا يُدرَج كنص حر في ORDER BY لمنع حقن SQL).</summary>
        public (List<JournalEntry> Items, int Total) GetPaged(
            int page, int pageSize,
            string searchText = null, DateTime? dateFrom = null, DateTime? dateTo = null, string source = null,
            bool? isPosted = null, string accountCode = null, decimal? minAmount = null, decimal? maxAmount = null,
            string sortColumn = "EntryDate", bool sortDescending = true)
        {
            var where = new WhereBuilder()
                .LikeAny(searchText, "e.EntryNo", "e.Description")
                .RawWithParam(p => $"e.EntryDate >= {p}", dateFrom?.ToString("yyyy-MM-dd"))
                .RawWithParam(p => $"e.EntryDate <= {p}", dateTo?.ToString("yyyy-MM-dd"))
                .Eq("e.Source", source)
                .Eq("e.IsPosted", isPosted)
                .RawWithParam(p => $"EXISTS (SELECT 1 FROM JournalEntryLines l WHERE l.EntryId = e.Id AND l.AccountCode = {p})", accountCode)
                .RawWithParam(p => $"e.TotalDebit >= {p}", minAmount)
                .RawWithParam(p => $"e.TotalDebit <= {p}", maxAmount);

            var column = sortColumn switch
            {
                "EntryNo"     => "e.EntryNo",
                "TotalDebit"  => "e.TotalDebit",
                "TotalCredit" => "e.TotalCredit",
                "IsPosted"    => "e.IsPosted",
                "CreatedAt"   => "e.CreatedAt",
                _             => "e.EntryDate"
            };
            return Page(where, page, pageSize, OrderBuilder.By(column, sortDescending, "e.Id", "e.EntryNo", "e.CreatedAt"),
                from: "JournalEntries e", select: "SELECT e.* FROM JournalEntries e");
        }

        // ===== كتابة — تأخذ (conn, tx) من الخدمة، لا تفتح معاملة هنا =====

        public int InsertHeader(DbConnection conn, DbTransaction tx, JournalEntry entry) =>
            InsertGetId(
                "INSERT INTO JournalEntries (EntryNo, EntryDate, Description, Source, CreatedBy) VALUES (@no, @date, @desc, @source, @by)",
                conn, tx,
                ("@no", entry.EntryNo), ("@date", entry.EntryDate),
                ("@desc", entry.Description ?? ""), ("@source", entry.Source ?? "يدوي"), ("@by", entry.CreatedBy));

        public void UpdateEntry(DbConnection conn, DbTransaction tx, int id, string entryDate, string description) =>
            Exec("UPDATE JournalEntries SET EntryDate = @date, Description = @desc, UpdatedAt = @now WHERE Id = @id",
                conn, tx, ("@date", entryDate), ("@desc", description ?? ""), ("@now", DateTime.Now), ("@id", id));

        public void InsertLine(DbConnection conn, DbTransaction tx, int entryId, int lineNo, JournalLine line) =>
            Exec(@"INSERT INTO JournalEntryLines (EntryId, LineNo, AccountCode, AccountName, Debit, Credit, Notes)
                  VALUES (@eid, @lno, @code, @name, @debit, @credit, @notes)",
                conn, tx,
                ("@eid", entryId), ("@lno", lineNo), ("@code", line.AccountCode),
                ("@name", line.AccountName ?? ""), ("@debit", line.Debit), ("@credit", line.Credit), ("@notes", line.Notes ?? ""));

        public void UpdateTotals(DbConnection conn, DbTransaction tx, int entryId, decimal totalDebit, decimal totalCredit) =>
            Exec("UPDATE JournalEntries SET TotalDebit = @d, TotalCredit = @c, UpdatedAt = @now WHERE Id = @id",
                conn, tx, ("@d", totalDebit), ("@c", totalCredit), ("@now", DateTime.Now), ("@id", entryId));

        public void DeleteLines(int entryId, DbConnection conn = null, DbTransaction tx = null) =>
            Exec("DELETE FROM JournalEntryLines WHERE EntryId = @id", conn, tx, ("@id", entryId));

        public void DeleteHeader(int entryId, DbConnection conn = null, DbTransaction tx = null) =>
            Exec("DELETE FROM JournalEntries WHERE Id = @id", conn, tx, ("@id", entryId));

        /// <summary>تبديل IsPosted فقط — بلا PostedAt/PostedBy. تستخدمه اختبارات زرع بيانات (SeedPostedEntry) لا JournalService (تستخدم النسخة (conn,tx,postedAt,postedBy) أدناه).</summary>
        public void SetPosted(int entryId, bool isPosted) =>
            Exec("UPDATE JournalEntries SET IsPosted = @p, UpdatedAt = @now WHERE Id = @id",
                null, null, ("@p", isPosted), ("@now", DateTime.Now), ("@id", entryId));

        public void SetPosted(DbConnection conn, DbTransaction tx, int entryId, DateTime postedAt, string postedBy) =>
            Exec("UPDATE JournalEntries SET IsPosted = @p, PostedAt = @at, PostedBy = @by, UpdatedAt = @now WHERE Id = @id",
                conn, tx, ("@p", true), ("@at", postedAt), ("@by", postedBy), ("@now", DateTime.Now), ("@id", entryId));

        public void SetUnposted(DbConnection conn, DbTransaction tx, int entryId) =>
            Exec("UPDATE JournalEntries SET IsPosted = @p, PostedAt = NULL, PostedBy = NULL, UpdatedAt = @now WHERE Id = @id",
                conn, tx, ("@p", false), ("@now", DateTime.Now), ("@id", entryId));
    }
}
