using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Query;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>
    /// طبقة وصول بيانات السنوات/الفترات المالية — SQL خام ↔ Models فقط. بلا تحقق، بلا منطق تواريخ (كل ذلك
    /// مسؤولية IFiscalPeriodService). يدير كيانين (FiscalYear أساسي عبر RepositoryBase، FiscalPeriod ثانوي
    /// عبر QueryAs) — راجع تعليق RepositoryBase.QueryAs.
    /// </summary>
    public class FiscalPeriodRepository : RepositoryBase<FiscalYear>, IFiscalPeriodRepository
    {
        protected override string TableName => "FiscalYears";

        public void CreateTable()
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

        protected override FiscalYear Map(DataRow row) => new()
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

        public List<FiscalYear> GetAllYears() =>
            Query("SELECT * FROM FiscalYears WHERE IsDeleted = @d ORDER BY StartDate DESC", null, null, ("@d", false));

        public FiscalYear GetYearById(int id) => GetById(id);

        public FiscalYear GetCurrentYear() =>
            QueryOne("SELECT * FROM FiscalYears WHERE IsCurrent = @c AND IsDeleted = @d", null, null, ("@c", true), ("@d", false));

        public FiscalYear GetYearContaining(string date) =>
            QueryOne("SELECT * FROM FiscalYears WHERE @date >= StartDate AND @date <= EndDate AND IsDeleted = @d",
                null, null, ("@date", date), ("@d", false));

        public bool AnyYearOverlapping(string start, string end, int? excludeId)
        {
            var where = new WhereBuilder()
                .Eq("IsDeleted", false)
                .RawWithParam(p => $"StartDate <= {p}", end)
                .RawWithParam(p => $"EndDate >= {p}", start)
                .RawWithParam(p => $"Id != {p}", excludeId);
            return Convert.ToInt64(Scalar($"SELECT COUNT(*) FROM FiscalYears {where.Sql}", where.Parameters)) > 0;
        }

        public int InsertYear(DbConnection conn, DbTransaction tx, FiscalYear year) =>
            InsertGetId("INSERT INTO FiscalYears (Name, StartDate, EndDate, IsCurrent) VALUES (@name, @start, @end, @current)",
                conn, tx, ("@name", year.Name), ("@start", year.StartDate), ("@end", year.EndDate), ("@current", year.IsCurrent));

        public void UpdateYear(DbConnection conn, DbTransaction tx, FiscalYear year) =>
            Exec("UPDATE FiscalYears SET Name = @name, StartDate = @start, EndDate = @end, UpdatedAt = @now WHERE Id = @id",
                conn, tx, ("@name", year.Name), ("@start", year.StartDate), ("@end", year.EndDate), ("@now", DateTime.Now), ("@id", year.Id));

        public void SetYearClosed(DbConnection conn, DbTransaction tx, int id, DateTime closedAt, string closedBy, int? closingEntryId) =>
            Exec("UPDATE FiscalYears SET IsClosed = @c, ClosedAt = @at, ClosedBy = @by, ClosingEntryId = @entry WHERE Id = @id",
                conn, tx, ("@c", true), ("@at", closedAt), ("@by", closedBy), ("@entry", (object)closingEntryId), ("@id", id));

        public void SetYearReopened(DbConnection conn, DbTransaction tx, int id) =>
            Exec("UPDATE FiscalYears SET IsClosed = @c, ClosedAt = NULL, ClosedBy = NULL, ClosingEntryId = NULL WHERE Id = @id",
                conn, tx, ("@c", false), ("@id", id));

        public void SetCurrentYear(DbConnection conn, DbTransaction tx, int id)
        {
            Exec("UPDATE FiscalYears SET IsCurrent = @f", conn, tx, ("@f", false));
            Exec("UPDATE FiscalYears SET IsCurrent = @t WHERE Id = @id", conn, tx, ("@t", true), ("@id", id));
        }

        // ===== فترات =====

        public List<FiscalPeriod> GetPeriods(int yearId) =>
            QueryAs(MapPeriod, "SELECT * FROM FiscalPeriods WHERE FiscalYearId = @y ORDER BY PeriodNo", null, null, ("@y", yearId));

        public FiscalPeriod GetPeriodById(int id) =>
            QueryOneAs(MapPeriod, "SELECT * FROM FiscalPeriods WHERE Id = @id", null, null, ("@id", id));

        public FiscalPeriod GetPeriodContaining(string date) =>
            QueryOneAs(MapPeriod, "SELECT * FROM FiscalPeriods WHERE @date >= StartDate AND @date <= EndDate", null, null, ("@date", date));

        public void InsertPeriod(DbConnection conn, DbTransaction tx, FiscalPeriod period) =>
            Exec(@"INSERT INTO FiscalPeriods (FiscalYearId, PeriodNo, Name, StartDate, EndDate) VALUES (@year, @no, @name, @start, @end)",
                conn, tx,
                ("@year", period.FiscalYearId), ("@no", period.PeriodNo), ("@name", period.Name),
                ("@start", period.StartDate), ("@end", period.EndDate));

        public void UpdatePeriod(DbConnection conn, DbTransaction tx, FiscalPeriod period) =>
            Exec("UPDATE FiscalPeriods SET Name = @name, StartDate = @start, EndDate = @end, UpdatedAt = @now WHERE Id = @id",
                conn, tx, ("@name", period.Name), ("@start", period.StartDate), ("@end", period.EndDate), ("@now", DateTime.Now), ("@id", period.Id));

        public void SetPeriodClosed(DbConnection conn, DbTransaction tx, int id, DateTime closedAt, string closedBy) =>
            Exec("UPDATE FiscalPeriods SET IsClosed = @c, ClosedAt = @at, ClosedBy = @by WHERE Id = @id",
                conn, tx, ("@c", true), ("@at", closedAt), ("@by", closedBy), ("@id", id));

        public void SetPeriodReopened(DbConnection conn, DbTransaction tx, int id) =>
            Exec("UPDATE FiscalPeriods SET IsClosed = @c, ClosedAt = NULL, ClosedBy = NULL WHERE Id = @id",
                conn, tx, ("@c", false), ("@id", id));
    }
}
