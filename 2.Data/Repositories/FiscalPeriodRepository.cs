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
    /// طبقة وصول بيانات السنوات/الفترات المالية — SQL خام ↔ Models فقط. بلا تحقق، بلا معاملات تفتحها هذه
    /// الطبقة، بلا منطق تواريخ (تقسيم فترات/تراكب/إقفال — كل ذلك مسؤولية Services/Accounting/IFiscalPeriodService).
    /// </summary>
    public static class FiscalPeriodRepository
    {
        public static void CreateTable()
        {
            SchemaBuilder.Table("FiscalYears")
                .Id()
                .Text("Name", 100, required: true)
                .Text("StartDate", 20, required: true)
                .Text("EndDate", 20, required: true)
                .Bool("IsClosed", defaultValue: false)
                .DateCol("ClosedAt")
                .Text("ClosedBy", 100)
                .Int("ClosingEntryId")
                .Bool("IsCurrent", defaultValue: false)
                .Audit()
                .SoftDelete()
                .Concurrency()
                .Create();

            SchemaBuilder.Table("FiscalPeriods")
                .Id()
                .Int("FiscalYearId", nullable: false)
                .Int("PeriodNo", nullable: false)
                .Text("Name", 100, required: true)
                .Text("StartDate", 20, required: true)
                .Text("EndDate", 20, required: true)
                .Bool("IsClosed", defaultValue: false)
                .DateCol("ClosedAt")
                .Text("ClosedBy", 100)
                .Audit()
                .Concurrency()
                .ForeignKey("FiscalYearId", "FiscalYears", "Id")
                .Index("FiscalYearId")
                .Create();
        }

        private static FiscalYear MapYear(DataRow row) => new()
        {
            Id             = Convert.ToInt32(row["Id"]),
            Name           = row["Name"].ToString(),
            StartDate      = row["StartDate"].ToString(),
            EndDate        = row["EndDate"].ToString(),
            IsClosed       = Convert.ToBoolean(row["IsClosed"]),
            ClosedAt       = row["ClosedAt"] == DBNull.Value ? null : Convert.ToDateTime(row["ClosedAt"]),
            ClosedBy       = row["ClosedBy"] == DBNull.Value ? null : row["ClosedBy"].ToString(),
            ClosingEntryId = row["ClosingEntryId"] == DBNull.Value ? null : Convert.ToInt32(row["ClosingEntryId"]),
            IsCurrent      = Convert.ToBoolean(row["IsCurrent"]),
            IsDeleted      = Convert.ToBoolean(row["IsDeleted"]),
            RowVersion     = Convert.ToInt64(row["RowVersion"])
        };

        private static FiscalPeriod MapPeriod(DataRow row) => new()
        {
            Id           = Convert.ToInt32(row["Id"]),
            FiscalYearId = Convert.ToInt32(row["FiscalYearId"]),
            PeriodNo     = Convert.ToInt32(row["PeriodNo"]),
            Name         = row["Name"].ToString(),
            StartDate    = row["StartDate"].ToString(),
            EndDate      = row["EndDate"].ToString(),
            IsClosed     = Convert.ToBoolean(row["IsClosed"]),
            ClosedAt     = row["ClosedAt"] == DBNull.Value ? null : Convert.ToDateTime(row["ClosedAt"]),
            ClosedBy     = row["ClosedBy"] == DBNull.Value ? null : row["ClosedBy"].ToString(),
            RowVersion   = Convert.ToInt64(row["RowVersion"])
        };

        // ===== سنوات =====

        public static List<FiscalYear> GetAllYears() =>
            Db.Query("SELECT * FROM FiscalYears WHERE IsDeleted = @d ORDER BY StartDate DESC", Db.Params(("@d", false)))
              .AsEnumerable().Select(MapYear).ToList();

        public static FiscalYear GetYearById(int id) =>
            Db.Query("SELECT * FROM FiscalYears WHERE Id = @id", Db.Params(("@id", id)))
              .AsEnumerable().Select(MapYear).FirstOrDefault();

        public static FiscalYear GetCurrentYear() =>
            Db.Query("SELECT * FROM FiscalYears WHERE IsCurrent = @c AND IsDeleted = @d", Db.Params(("@c", true), ("@d", false)))
              .AsEnumerable().Select(MapYear).FirstOrDefault();

        public static FiscalYear GetYearContaining(string date) =>
            Db.Query("SELECT * FROM FiscalYears WHERE @date >= StartDate AND @date <= EndDate AND IsDeleted = @d",
                Db.Params(("@date", date), ("@d", false)))
              .AsEnumerable().Select(MapYear).FirstOrDefault();

        public static bool AnyYearOverlapping(string start, string end, int? excludeId)
        {
            var sql = "SELECT COUNT(*) FROM FiscalYears WHERE IsDeleted = @d AND StartDate <= @end AND EndDate >= @start";
            var parameters = new List<(string, object)> { ("@d", false), ("@start", start), ("@end", end) };

            if (excludeId.HasValue)
            {
                sql += " AND Id != @excludeId";
                parameters.Add(("@excludeId", excludeId.Value));
            }

            return Convert.ToInt64(Db.Scalar(sql, Db.Params(parameters.ToArray()))) > 0;
        }

        public static int InsertYear(DbConnection conn, DbTransaction tx, FiscalYear year) =>
            Db.InsertAndGetId(conn, tx,
                "INSERT INTO FiscalYears (Name, StartDate, EndDate, IsCurrent) VALUES (@name, @start, @end, @current)",
                Db.Params(("@name", year.Name), ("@start", year.StartDate), ("@end", year.EndDate), ("@current", year.IsCurrent)));

        public static void UpdateYear(DbConnection conn, DbTransaction tx, FiscalYear year)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE FiscalYears SET Name = @name, StartDate = @start, EndDate = @end, UpdatedAt = @now WHERE Id = @id",
                Db.Params(("@name", year.Name), ("@start", year.StartDate), ("@end", year.EndDate), ("@now", DateTime.Now), ("@id", year.Id)));
            cmd.ExecuteNonQuery();
        }

        public static void SetYearClosed(DbConnection conn, DbTransaction tx, int id, DateTime closedAt, string closedBy, int? closingEntryId)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE FiscalYears SET IsClosed = @c, ClosedAt = @at, ClosedBy = @by, ClosingEntryId = @entry WHERE Id = @id",
                Db.Params(("@c", true), ("@at", closedAt), ("@by", closedBy), ("@entry", (object)closingEntryId), ("@id", id)));
            cmd.ExecuteNonQuery();
        }

        public static void SetYearReopened(DbConnection conn, DbTransaction tx, int id)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE FiscalYears SET IsClosed = @c, ClosedAt = NULL, ClosedBy = NULL, ClosingEntryId = NULL WHERE Id = @id",
                Db.Params(("@c", false), ("@id", id)));
            cmd.ExecuteNonQuery();
        }

        public static void SetCurrentYear(DbConnection conn, DbTransaction tx, int id)
        {
            using (var clearCmd = Db.CreateCommand(conn, tx, "UPDATE FiscalYears SET IsCurrent = @f", Db.Params(("@f", false))))
                clearCmd.ExecuteNonQuery();

            using var setCmd = Db.CreateCommand(conn, tx, "UPDATE FiscalYears SET IsCurrent = @t WHERE Id = @id", Db.Params(("@t", true), ("@id", id)));
            setCmd.ExecuteNonQuery();
        }

        // ===== فترات =====

        public static List<FiscalPeriod> GetPeriods(int yearId) =>
            Db.Query("SELECT * FROM FiscalPeriods WHERE FiscalYearId = @y ORDER BY PeriodNo", Db.Params(("@y", yearId)))
              .AsEnumerable().Select(MapPeriod).ToList();

        public static FiscalPeriod GetPeriodById(int id) =>
            Db.Query("SELECT * FROM FiscalPeriods WHERE Id = @id", Db.Params(("@id", id)))
              .AsEnumerable().Select(MapPeriod).FirstOrDefault();

        public static FiscalPeriod GetPeriodContaining(string date) =>
            Db.Query("SELECT * FROM FiscalPeriods WHERE @date >= StartDate AND @date <= EndDate", Db.Params(("@date", date)))
              .AsEnumerable().Select(MapPeriod).FirstOrDefault();

        public static void InsertPeriod(DbConnection conn, DbTransaction tx, FiscalPeriod period)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                @"INSERT INTO FiscalPeriods (FiscalYearId, PeriodNo, Name, StartDate, EndDate)
                  VALUES (@year, @no, @name, @start, @end)",
                Db.Params(
                    ("@year", period.FiscalYearId), ("@no", period.PeriodNo), ("@name", period.Name),
                    ("@start", period.StartDate), ("@end", period.EndDate)));
            cmd.ExecuteNonQuery();
        }

        public static void UpdatePeriod(DbConnection conn, DbTransaction tx, FiscalPeriod period)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE FiscalPeriods SET Name = @name, StartDate = @start, EndDate = @end, UpdatedAt = @now WHERE Id = @id",
                Db.Params(("@name", period.Name), ("@start", period.StartDate), ("@end", period.EndDate), ("@now", DateTime.Now), ("@id", period.Id)));
            cmd.ExecuteNonQuery();
        }

        public static void SetPeriodClosed(DbConnection conn, DbTransaction tx, int id, DateTime closedAt, string closedBy)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE FiscalPeriods SET IsClosed = @c, ClosedAt = @at, ClosedBy = @by WHERE Id = @id",
                Db.Params(("@c", true), ("@at", closedAt), ("@by", closedBy), ("@id", id)));
            cmd.ExecuteNonQuery();
        }

        public static void SetPeriodReopened(DbConnection conn, DbTransaction tx, int id)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE FiscalPeriods SET IsClosed = @c, ClosedAt = NULL, ClosedBy = NULL WHERE Id = @id",
                Db.Params(("@c", false), ("@id", id)));
            cmd.ExecuteNonQuery();
        }
    }
}
