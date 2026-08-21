using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Data.Repositories
{
    /// <summary>
    /// طبقة وصول بيانات قيود اليومية — SQL خام ↔ Models فقط. بلا معاملات تفتحها هذه الطبقة (الخدمة تفتحها
    /// عبر DbHelper.RunTransaction وتمرّر (conn, tx) للدوال هنا)، بلا حساب إجماليات/أرصدة، بلا توليد رقم
    /// (ذلك عبر Services/INumberSequenceService)، بلا Audit. كل ذلك مسؤولية Services/Accounting/JournalService
    /// (المرحلة F.2.3) — راجع MIGRATION_INVENTORY.md لِما نُقل من هنا وإلى أين.
    /// </summary>
    public static class JournalRepository
    {
        public static void CreateTable()
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

        private static JournalEntry MapEntry(DataRow row) => new()
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

        public static JournalEntry GetById(int id) =>
            Db.Query("SELECT * FROM JournalEntries WHERE Id = @id", Db.Params(("@id", id)))
              .AsEnumerable().Select(MapEntry).FirstOrDefault();

        public static JournalEntry GetByEntryNo(string entryNo) =>
            Db.Query("SELECT * FROM JournalEntries WHERE EntryNo = @no", Db.Params(("@no", entryNo)))
              .AsEnumerable().Select(MapEntry).FirstOrDefault();

        /// <summary>لقراءة قيد من داخل معاملة مستدعٍ آخر مفتوحة بالفعل (JournalService.Post/Unpost/PostBatch (conn,tx)) — راجع تعليق DbHelper.Query(conn,tx,...) لسبب وجوب هذا لا القراءة العادية.</summary>
        public static JournalEntry GetById(DbConnection conn, DbTransaction tx, int id) =>
            Db.Query(conn, tx, "SELECT * FROM JournalEntries WHERE Id = @id", Db.Params(("@id", id)))
              .AsEnumerable().Select(MapEntry).FirstOrDefault();

        public static List<JournalLine> GetLines(int entryId) =>
            Db.Query("SELECT * FROM JournalEntryLines WHERE EntryId = @id ORDER BY LineNo", Db.Params(("@id", entryId)))
              .AsEnumerable().Select(MapLine).ToList();

        /// <summary>نفس GetLines أعلاه لكن من داخل معاملة قائمة — نفس سبب GetById(conn,tx,...).</summary>
        public static List<JournalLine> GetLines(DbConnection conn, DbTransaction tx, int entryId) =>
            Db.Query(conn, tx, "SELECT * FROM JournalEntryLines WHERE EntryId = @id ORDER BY LineNo", Db.Params(("@id", entryId)))
              .AsEnumerable().Select(MapLine).ToList();

        public static bool IsPosted(int entryId)
        {
            var result = Db.Scalar("SELECT IsPosted FROM JournalEntries WHERE Id = @id", Db.Params(("@id", entryId)));
            return result != null && Convert.ToBoolean(result);
        }

        /// <summary>هل يوجد لهذا الحساب أي سطر قيد؟ تستخدمه خدمة الحسابات قبل السماح بحذف حساب (بدل قراءة AccountRepository لجدول لا تملكه).</summary>
        public static bool HasLinesForAccount(string accountCode)
        {
            var result = Db.Scalar("SELECT COUNT(*) FROM JournalEntryLines WHERE AccountCode = @c", Db.Params(("@c", accountCode)));
            return Convert.ToInt64(result) > 0;
        }

        /// <summary>سطور القيود المرحّلة فقط لحساب مُعيّن (اختيارياً بين تاريخين) — تستخدمه AccountService لحساب الرصيد وكشف الحساب.</summary>
        public static List<(string EntryDate, string EntryNo, string Description, decimal Debit, decimal Credit)> GetPostedLinesForAccount(
            string accountCode, DateTime? from, DateTime? to)
        {
            var sql = @"SELECT e.EntryDate, e.EntryNo, e.Description, l.Debit, l.Credit
                        FROM JournalEntryLines l
                        JOIN JournalEntries e ON e.Id = l.EntryId
                        WHERE l.AccountCode = @code AND e.IsPosted = @posted";

            var parameters = new List<(string, object)> { ("@code", accountCode), ("@posted", true) };

            if (from.HasValue)
            {
                sql += " AND e.EntryDate >= @from";
                parameters.Add(("@from", from.Value.ToString("yyyy-MM-dd")));
            }
            if (to.HasValue)
            {
                sql += " AND e.EntryDate <= @to";
                parameters.Add(("@to", to.Value.ToString("yyyy-MM-dd")));
            }

            sql += " ORDER BY e.EntryDate, e.Id, l.LineNo";

            return Db.Query(sql, Db.Params(parameters.ToArray()))
                .AsEnumerable()
                .Select(r => (
                    r["EntryDate"].ToString(),
                    r["EntryNo"].ToString(),
                    r["Description"] == DBNull.Value ? "" : r["Description"].ToString(),
                    Convert.ToDecimal(r["Debit"]),
                    Convert.ToDecimal(r["Credit"])))
                .ToList();
        }

        /// <summary>نفس GetPostedLinesForAccount أعلاه لكن من داخل معاملة قائمة — تستخدمه AccountService.RecalculateBalance(conn,tx,...) عندما يُستدعى من JournalService.Post/Unpost (نفس سبب GetById(conn,tx,...) في هذا الملف).</summary>
        public static List<(string EntryDate, string EntryNo, string Description, decimal Debit, decimal Credit)> GetPostedLinesForAccount(
            DbConnection conn, DbTransaction tx, string accountCode, DateTime? from, DateTime? to)
        {
            var sql = @"SELECT e.EntryDate, e.EntryNo, e.Description, l.Debit, l.Credit
                        FROM JournalEntryLines l
                        JOIN JournalEntries e ON e.Id = l.EntryId
                        WHERE l.AccountCode = @code AND e.IsPosted = @posted";

            var parameters = new List<(string, object)> { ("@code", accountCode), ("@posted", true) };

            if (from.HasValue)
            {
                sql += " AND e.EntryDate >= @from";
                parameters.Add(("@from", from.Value.ToString("yyyy-MM-dd")));
            }
            if (to.HasValue)
            {
                sql += " AND e.EntryDate <= @to";
                parameters.Add(("@to", to.Value.ToString("yyyy-MM-dd")));
            }

            sql += " ORDER BY e.EntryDate, e.Id, l.LineNo";

            return Db.Query(conn, tx, sql, Db.Params(parameters.ToArray()))
                .AsEnumerable()
                .Select(r => (
                    r["EntryDate"].ToString(),
                    r["EntryNo"].ToString(),
                    r["Description"] == DBNull.Value ? "" : r["Description"].ToString(),
                    Convert.ToDecimal(r["Debit"]),
                    Convert.ToDecimal(r["Credit"])))
                .ToList();
        }

        public static (int TotalEntries, decimal TotalDebit, decimal TotalCredit) GetSummary()
        {
            var dt = Db.Query(
                @"SELECT COUNT(*) AS TotalEntries,
                         COALESCE(SUM(TotalDebit),0) AS TotalDebit,
                         COALESCE(SUM(TotalCredit),0) AS TotalCredit
                  FROM JournalEntries");

            var row = dt.Rows[0];
            return (Convert.ToInt32(row["TotalEntries"]), Convert.ToDecimal(row["TotalDebit"]), Convert.ToDecimal(row["TotalCredit"]));
        }

        /// <summary>عدد القيود غير المرحّلة بتاريخ بين from وto (شامل الحدّين) — تستخدمه FiscalPeriodService.ClosePeriod.</summary>
        public static int CountUnpostedBetween(DateTime from, DateTime to)
        {
            var result = Db.Scalar(
                "SELECT COUNT(*) FROM JournalEntries WHERE IsPosted = @p AND EntryDate >= @f AND EntryDate <= @t",
                Db.Params(("@p", false), ("@f", from.ToString("yyyy-MM-dd")), ("@t", to.ToString("yyyy-MM-dd"))));
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// مجموع مدين/دائن كل حساب ظهر بسطر قيد بين from (اختياري — null يعني منذ البداية) وto — استعلام
        /// GROUP BY واحد، لا حلقة على الحسابات. تستخدمه JournalService.GetTrialBalance مرتين: مرة للرصيد
        /// الافتتاحي (from=null، to=يوم قبل بداية الفترة) ومرة لحركة الفترة (from/to الفترة نفسها).
        /// </summary>
        public static List<(string AccountCode, decimal SumDebit, decimal SumCredit)> GetAccountSums(
            DateTime? from, DateTime to, bool postedOnly)
        {
            var sql = @"SELECT l.AccountCode,
                               COALESCE(SUM(l.Debit),0)  AS SumDebit,
                               COALESCE(SUM(l.Credit),0) AS SumCredit
                        FROM JournalEntryLines l
                        JOIN JournalEntries e ON e.Id = l.EntryId
                        WHERE e.EntryDate <= @to";

            var parameters = new List<(string, object)> { ("@to", to.ToString("yyyy-MM-dd")) };

            if (from.HasValue)
            {
                sql += " AND e.EntryDate >= @from";
                parameters.Add(("@from", from.Value.ToString("yyyy-MM-dd")));
            }
            if (postedOnly)
            {
                sql += " AND e.IsPosted = @posted";
                parameters.Add(("@posted", true));
            }

            sql += " GROUP BY l.AccountCode";

            return Db.Query(sql, Db.Params(parameters.ToArray()))
                .AsEnumerable()
                .Select(r => (r["AccountCode"].ToString(), Convert.ToDecimal(r["SumDebit"]), Convert.ToDecimal(r["SumCredit"])))
                .ToList();
        }

        /// <summary>عدد سطور كل قيد من مجموعة معرّفات — استعلام واحد، لا حلقة لكل صف صفحة.</summary>
        public static Dictionary<int, int> GetLineCounts(IEnumerable<int> entryIds)
        {
            var ids = entryIds.ToList();
            if (ids.Count == 0) return new Dictionary<int, int>();

            var placeholders = string.Join(",", ids.Select((_, i) => $"@id{i}"));
            var parameters = ids.Select((id, i) => ($"@id{i}", (object)id)).ToArray();

            var dt = Db.Query(
                $"SELECT EntryId, COUNT(*) AS Cnt FROM JournalEntryLines WHERE EntryId IN ({placeholders}) GROUP BY EntryId",
                Db.Params(parameters));

            return dt.AsEnumerable().ToDictionary(r => Convert.ToInt32(r["EntryId"]), r => Convert.ToInt32(r["Cnt"]));
        }

        /// <summary>قائمة مُرقَّمة (صفحات) لقيود اليومية مع الفلاتر — بناء SQL شرطي حسب الفلاتر الممرَّرة (لا قرار أعمال، بناء استعلام فقط). sortColumn يُطابَق بقائمة أعمدة مسموحة صراحة (لا يُدرَج كنص حر في ORDER BY لمنع حقن SQL).</summary>
        public static (List<JournalEntry> Items, int Total) GetPaged(
            int page, int pageSize,
            string searchText = null, DateTime? dateFrom = null, DateTime? dateTo = null, string source = null,
            bool? isPosted = null, string accountCode = null, decimal? minAmount = null, decimal? maxAmount = null,
            string sortColumn = "EntryDate", bool sortDescending = true)
        {
            var where = new List<string>();
            var parameters = new List<(string, object)>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                where.Add("(e.EntryNo LIKE @search OR e.Description LIKE @search)");
                parameters.Add(("@search", $"%{searchText}%"));
            }
            if (dateFrom.HasValue)
            {
                where.Add("e.EntryDate >= @dateFrom");
                parameters.Add(("@dateFrom", dateFrom.Value.ToString("yyyy-MM-dd")));
            }
            if (dateTo.HasValue)
            {
                where.Add("e.EntryDate <= @dateTo");
                parameters.Add(("@dateTo", dateTo.Value.ToString("yyyy-MM-dd")));
            }
            if (!string.IsNullOrWhiteSpace(source))
            {
                where.Add("e.Source = @source");
                parameters.Add(("@source", source));
            }
            if (isPosted.HasValue)
            {
                where.Add("e.IsPosted = @isPosted");
                parameters.Add(("@isPosted", isPosted.Value));
            }
            if (!string.IsNullOrWhiteSpace(accountCode))
            {
                where.Add("EXISTS (SELECT 1 FROM JournalEntryLines l WHERE l.EntryId = e.Id AND l.AccountCode = @accountCode)");
                parameters.Add(("@accountCode", accountCode));
            }
            if (minAmount.HasValue)
            {
                where.Add("e.TotalDebit >= @minAmount");
                parameters.Add(("@minAmount", minAmount.Value));
            }
            if (maxAmount.HasValue)
            {
                where.Add("e.TotalDebit <= @maxAmount");
                parameters.Add(("@maxAmount", maxAmount.Value));
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

            var column = sortColumn switch
            {
                "EntryNo"     => "e.EntryNo",
                "TotalDebit"  => "e.TotalDebit",
                "TotalCredit" => "e.TotalCredit",
                "IsPosted"    => "e.IsPosted",
                "CreatedAt"   => "e.CreatedAt",
                _             => "e.EntryDate"
            };
            var direction = sortDescending ? "DESC" : "ASC";

            var total = Convert.ToInt32(Db.Scalar($"SELECT COUNT(*) FROM JournalEntries e {whereClause}", Db.Params(parameters.ToArray())));

            var pageSql = $@"SELECT e.* FROM JournalEntries e {whereClause}
                              ORDER BY {column} {direction}, e.Id {direction}
                              {DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";

            var items = Db.Query(pageSql, Db.Params(parameters.ToArray())).AsEnumerable().Select(MapEntry).ToList();

            return (items, total);
        }

        // ===== كتابة — تأخذ (conn, tx) من الخدمة، لا تفتح معاملة هنا =====

        public static int InsertHeader(DbConnection conn, DbTransaction tx, JournalEntry entry) =>
            Db.InsertAndGetId(conn, tx,
                "INSERT INTO JournalEntries (EntryNo, EntryDate, Description, Source, CreatedBy) VALUES (@no, @date, @desc, @source, @by)",
                Db.Params(
                    ("@no", entry.EntryNo), ("@date", entry.EntryDate),
                    ("@desc", entry.Description ?? ""), ("@source", entry.Source ?? "يدوي"),
                    ("@by", entry.CreatedBy)));

        public static void UpdateEntry(DbConnection conn, DbTransaction tx, int id, string entryDate, string description)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE JournalEntries SET EntryDate = @date, Description = @desc, UpdatedAt = @now WHERE Id = @id",
                Db.Params(("@date", entryDate), ("@desc", description ?? ""), ("@now", DateTime.Now), ("@id", id)));
            cmd.ExecuteNonQuery();
        }

        public static void InsertLine(DbConnection conn, DbTransaction tx, int entryId, int lineNo, JournalLine line)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                @"INSERT INTO JournalEntryLines (EntryId, LineNo, AccountCode, AccountName, Debit, Credit, Notes)
                  VALUES (@eid, @lno, @code, @name, @debit, @credit, @notes)",
                Db.Params(
                    ("@eid", entryId), ("@lno", lineNo), ("@code", line.AccountCode),
                    ("@name", line.AccountName ?? ""), ("@debit", line.Debit),
                    ("@credit", line.Credit), ("@notes", line.Notes ?? "")));
            cmd.ExecuteNonQuery();
        }

        public static void UpdateTotals(DbConnection conn, DbTransaction tx, int entryId, decimal totalDebit, decimal totalCredit)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE JournalEntries SET TotalDebit = @d, TotalCredit = @c, UpdatedAt = @now WHERE Id = @id",
                Db.Params(("@d", totalDebit), ("@c", totalCredit), ("@now", DateTime.Now), ("@id", entryId)));
            cmd.ExecuteNonQuery();
        }

        public static void DeleteLines(int entryId) =>
            Db.Execute("DELETE FROM JournalEntryLines WHERE EntryId = @id", Db.Params(("@id", entryId)));

        public static void DeleteLines(DbConnection conn, DbTransaction tx, int entryId)
        {
            using var cmd = Db.CreateCommand(conn, tx, "DELETE FROM JournalEntryLines WHERE EntryId = @id", Db.Params(("@id", entryId)));
            cmd.ExecuteNonQuery();
        }

        public static void DeleteHeader(int entryId) =>
            Db.Execute("DELETE FROM JournalEntries WHERE Id = @id", Db.Params(("@id", entryId)));

        public static void DeleteHeader(DbConnection conn, DbTransaction tx, int entryId)
        {
            using var cmd = Db.CreateCommand(conn, tx, "DELETE FROM JournalEntries WHERE Id = @id", Db.Params(("@id", entryId)));
            cmd.ExecuteNonQuery();
        }

        /// <summary>تبديل IsPosted فقط — بلا PostedAt/PostedBy. تستخدمه اختبارات زرع بيانات (SeedPostedEntry) لا JournalService (تستخدم النسخة (conn,tx) أدناه).</summary>
        public static void SetPosted(int entryId, bool isPosted) =>
            Db.Execute("UPDATE JournalEntries SET IsPosted = @p, UpdatedAt = @now WHERE Id = @id",
                Db.Params(("@p", isPosted), ("@now", DateTime.Now), ("@id", entryId)));

        public static void SetPosted(DbConnection conn, DbTransaction tx, int entryId, DateTime postedAt, string postedBy)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE JournalEntries SET IsPosted = @p, PostedAt = @at, PostedBy = @by, UpdatedAt = @now WHERE Id = @id",
                Db.Params(("@p", true), ("@at", postedAt), ("@by", postedBy), ("@now", DateTime.Now), ("@id", entryId)));
            cmd.ExecuteNonQuery();
        }

        public static void SetUnposted(DbConnection conn, DbTransaction tx, int entryId)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE JournalEntries SET IsPosted = @p, PostedAt = NULL, PostedBy = NULL, UpdatedAt = @now WHERE Id = @id",
                Db.Params(("@p", false), ("@now", DateTime.Now), ("@id", entryId)));
            cmd.ExecuteNonQuery();
        }
    }
}
