using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Core;
using PrimeERP.Data.Query;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IEmployeeMovementRepository<T> where T : EmployeeMovement
    {
        void CreateTable();
        T GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        (List<T> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? employeeId,
            string sortColumn, bool sortDescending);

        /// <summary>مجموع ما على كل موظف في شهرٍ بعينه — مادّة سطر المسير، باستعلامٍ واحد لا لكل موظف.</summary>
        Dictionary<int, decimal> SumByEmployee(int month, int year);

        int Insert(T item, DbConnection conn = null, DbTransaction tx = null);
        void Update(T item, DbConnection conn = null, DbTransaction tx = null);
        void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null);
    }

    public interface IEmployeeAllowanceRepository : IEmployeeMovementRepository<EmployeeAllowance> { }
    public interface IEmployeeDeductionRepository : IEmployeeMovementRepository<EmployeeDeduction> { }

    /// <summary>
    /// البدل والخصم جدولان بشكلٍ واحد — الفرق اسمُ الجدول وحده، فالجملة تُكتب هنا مرّةً ويرثها الاثنان.
    /// اسم الموظف يأتي بربطٍ في الاستعلام لا باستعلامٍ لكل صفّ.
    /// </summary>
    public abstract class EmployeeMovementRepository<T> : RepositoryBase<T>, IEmployeeMovementRepository<T>
        where T : EmployeeMovement, new()
    {
        public void CreateTable() =>
            SchemaBuilder.Table(TableName)
                .Id()
                .Int("EmployeeId", nullable: false)
                .DateCol("Date", nullable: false)
                .Int("Month", nullable: false).Int("Year", nullable: false)
                .Text("Reason", 200)
                .Decimal("Amount")
                .Text("Notes")
                .Audit().SoftDelete()
                .Index("EmployeeId")
                .Create();

        private const string SelectWithEmployee = @"
            SELECT t.*, e.Name AS EmployeeName, e.Code AS EmployeeCode
            FROM {0} t LEFT JOIN Employees e ON e.Id = t.EmployeeId";

        protected override T Map(DataRow row) => new()
        {
            Id         = Convert.ToInt32(row["Id"]),
            EmployeeId = Convert.ToInt32(row["EmployeeId"]),
            Date       = Convert.ToDateTime(row["Date"]),
            Month      = Convert.ToInt32(row["Month"]),
            Year       = Convert.ToInt32(row["Year"]),
            Reason     = row["Reason"] == DBNull.Value ? null : row["Reason"].ToString(),
            Amount     = Convert.ToDecimal(row["Amount"]),
            Notes      = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            EmployeeName = row.Table.Columns.Contains("EmployeeName") && row["EmployeeName"] != DBNull.Value ? row["EmployeeName"].ToString() : null,
            EmployeeCode = row.Table.Columns.Contains("EmployeeCode") && row["EmployeeCode"] != DBNull.Value ? row["EmployeeCode"].ToString() : null,
            CreatedAt  = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy  = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
            IsDeleted  = Convert.ToBoolean(row["IsDeleted"]),
        };

        public override T GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne($"{string.Format(SelectWithEmployee, TableName)} WHERE t.Id = @id AND t.IsDeleted = @d",
                conn, tx, ("@id", id), ("@d", false));

        public (List<T> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? employeeId,
            string sortColumn, bool sortDescending)
        {
            var where = new WhereBuilder().Eq("t.IsDeleted", false).Eq("t.EmployeeId", employeeId).LikeAny(searchText, "t.Reason", "e.Name");
            var column = sortColumn == "Amount" ? "t.Amount" : "t.Date";

            return Page(where, page, pageSize, OrderBuilder.By(column, sortDescending, "t.Id"),
                from: $"{TableName} t LEFT JOIN Employees e ON e.Id = t.EmployeeId",
                select: string.Format(SelectWithEmployee, TableName));
        }

        public Dictionary<int, decimal> SumByEmployee(int month, int year)
        {
            var result = new Dictionary<int, decimal>();
            foreach (DataRow row in DbHelper.Query(
                $@"SELECT EmployeeId, COALESCE(SUM(Amount), 0) AS Total FROM {TableName}
                   WHERE IsDeleted = @d AND Month = @m AND Year = @y GROUP BY EmployeeId",
                DbHelper.Params(("@d", false), ("@m", month), ("@y", year))).Rows)
                result[Convert.ToInt32(row["EmployeeId"])] = Convert.ToDecimal(row["Total"]);

            return result;
        }

        public int Insert(T item, DbConnection conn = null, DbTransaction tx = null) =>
            InsertGetId($@"INSERT INTO {TableName} (EmployeeId, Date, Month, Year, Reason, Amount, Notes, CreatedBy)
                           VALUES (@eid, @date, @month, @year, @reason, @amount, @notes, @by)",
                conn, tx, ("@eid", item.EmployeeId), ("@date", item.Date), ("@month", item.Month), ("@year", item.Year),
                ("@reason", item.Reason ?? ""), ("@amount", item.Amount), ("@notes", item.Notes ?? ""), ("@by", item.CreatedBy));

        public void Update(T item, DbConnection conn = null, DbTransaction tx = null) =>
            Exec($@"UPDATE {TableName} SET EmployeeId = @eid, Date = @date, Month = @month, Year = @year, Reason = @reason, Amount = @amount,
                    Notes = @notes, UpdatedAt = @now, UpdatedBy = @by WHERE Id = @id",
                conn, tx, ("@eid", item.EmployeeId), ("@date", item.Date), ("@month", item.Month), ("@year", item.Year),
                ("@reason", item.Reason ?? ""), ("@amount", item.Amount), ("@notes", item.Notes ?? ""), ("@now", DateTime.Now),
                ("@by", item.UpdatedBy), ("@id", item.Id));

        public void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null) =>
            SoftDelete(id, deletedBy, conn, tx);
    }

    public class EmployeeAllowanceRepository : EmployeeMovementRepository<EmployeeAllowance>, IEmployeeAllowanceRepository
    {
        protected override string TableName => "EmployeeAllowances";
    }

    public class EmployeeDeductionRepository : EmployeeMovementRepository<EmployeeDeduction>, IEmployeeDeductionRepository
    {
        protected override string TableName => "EmployeeDeductions";
    }
}
