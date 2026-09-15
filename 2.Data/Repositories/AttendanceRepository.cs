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
    public interface IAttendanceRepository
    {
        void CreateTable();
        Attendance GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        (List<Attendance> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? employeeId,
            string sortColumn, bool sortDescending);

        /// <summary>مجموع الساعات الإضافية لكل موظف في فترة — مادّة بند الإضافي في المسير.</summary>
        Dictionary<int, decimal> OvertimeByEmployee(DateTime from, DateTime to);

        /// <summary>عدد أيام الغياب لكل موظف في فترة — يُحتسب خصماً بأجر اليوم.</summary>
        Dictionary<int, int> AbsenceDaysByEmployee(DateTime from, DateTime to);

        int Insert(Attendance a, DbConnection conn = null, DbTransaction tx = null);
        void Update(Attendance a, DbConnection conn = null, DbTransaction tx = null);
        void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null);
    }

    // الوقت يُخزَّن دقائقَ من منتصف الليل لا نصّاً: يُقارَن ويُجمَع بلا تحويل، ويعمل على كل محرِّك.
    public class AttendanceRepository : RepositoryBase<Attendance>, IAttendanceRepository
    {
        protected override string TableName => "Attendances";

        public void CreateTable() =>
            SchemaBuilder.Table("Attendances")
                .Id()
                .Int("EmployeeId", nullable: false)
                .DateCol("Date", nullable: false)
                .Int("CheckInMinutes")
                .Int("CheckOutMinutes")
                .Decimal("OvertimeHours")
                .Bool("IsAbsent")
                .Text("Notes")
                .Audit().SoftDelete()
                .Index("EmployeeId")
                .Create();

        private const string SelectWithEmployee = @"
            SELECT t.*, e.Name AS EmployeeName, e.Code AS EmployeeCode
            FROM Attendances t LEFT JOIN Employees e ON e.Id = t.EmployeeId";

        private static TimeSpan? Minutes(object value) =>
            value == DBNull.Value ? null : TimeSpan.FromMinutes(Convert.ToInt32(value));

        protected override Attendance Map(DataRow row) => new()
        {
            Id            = Convert.ToInt32(row["Id"]),
            EmployeeId    = Convert.ToInt32(row["EmployeeId"]),
            Date          = Convert.ToDateTime(row["Date"]),
            CheckIn       = Minutes(row["CheckInMinutes"]),
            CheckOut      = Minutes(row["CheckOutMinutes"]),
            OvertimeHours = Convert.ToDecimal(row["OvertimeHours"]),
            IsAbsent      = Convert.ToBoolean(row["IsAbsent"]),
            Notes         = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            EmployeeName  = row.Table.Columns.Contains("EmployeeName") && row["EmployeeName"] != DBNull.Value ? row["EmployeeName"].ToString() : null,
            EmployeeCode  = row.Table.Columns.Contains("EmployeeCode") && row["EmployeeCode"] != DBNull.Value ? row["EmployeeCode"].ToString() : null,
            CreatedAt     = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            IsDeleted     = Convert.ToBoolean(row["IsDeleted"]),
        };

        public override Attendance GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne($"{SelectWithEmployee} WHERE t.Id = @id AND t.IsDeleted = @d", conn, tx, ("@id", id), ("@d", false));

        public (List<Attendance> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? employeeId,
            string sortColumn, bool sortDescending)
        {
            var where = new WhereBuilder().Eq("t.IsDeleted", false).Eq("t.EmployeeId", employeeId).LikeAny(searchText, "e.Name");
            var column = sortColumn == "OvertimeHours" ? "t.OvertimeHours" : "t.Date";

            return Page(where, page, pageSize, OrderBuilder.By(column, sortDescending, "t.Id"),
                from: "Attendances t LEFT JOIN Employees e ON e.Id = t.EmployeeId", select: SelectWithEmployee);
        }

        public Dictionary<int, decimal> OvertimeByEmployee(DateTime from, DateTime to)
        {
            var result = new Dictionary<int, decimal>();
            foreach (DataRow row in DbHelper.Query(
                @"SELECT EmployeeId, COALESCE(SUM(OvertimeHours), 0) AS Total FROM Attendances
                  WHERE IsDeleted = @d AND Date >= @from AND Date <= @to GROUP BY EmployeeId",
                DbHelper.Params(("@d", false), ("@from", from), ("@to", to))).Rows)
                result[Convert.ToInt32(row["EmployeeId"])] = Convert.ToDecimal(row["Total"]);

            return result;
        }

        public Dictionary<int, int> AbsenceDaysByEmployee(DateTime from, DateTime to)
        {
            var result = new Dictionary<int, int>();
            foreach (DataRow row in DbHelper.Query(
                @"SELECT EmployeeId, COUNT(*) AS Days FROM Attendances
                  WHERE IsDeleted = @d AND IsAbsent = @a AND Date >= @from AND Date <= @to GROUP BY EmployeeId",
                DbHelper.Params(("@d", false), ("@a", true), ("@from", from), ("@to", to))).Rows)
                result[Convert.ToInt32(row["EmployeeId"])] = Convert.ToInt32(row["Days"]);

            return result;
        }

        private static object ToMinutes(TimeSpan? value) => value == null ? null : (int)value.Value.TotalMinutes;

        public int Insert(Attendance a, DbConnection conn = null, DbTransaction tx = null) =>
            InsertGetId(@"INSERT INTO Attendances (EmployeeId, Date, CheckInMinutes, CheckOutMinutes, OvertimeHours, IsAbsent, Notes, CreatedBy)
                          VALUES (@eid, @date, @in, @out, @ot, @absent, @notes, @by)",
                conn, tx, ("@eid", a.EmployeeId), ("@date", a.Date), ("@in", ToMinutes(a.CheckIn)), ("@out", ToMinutes(a.CheckOut)),
                ("@ot", a.OvertimeHours), ("@absent", a.IsAbsent), ("@notes", a.Notes ?? ""), ("@by", a.CreatedBy));

        public void Update(Attendance a, DbConnection conn = null, DbTransaction tx = null) =>
            Exec(@"UPDATE Attendances SET EmployeeId = @eid, Date = @date, CheckInMinutes = @in, CheckOutMinutes = @out,
                   OvertimeHours = @ot, IsAbsent = @absent, Notes = @notes, UpdatedAt = @now, UpdatedBy = @by WHERE Id = @id",
                conn, tx, ("@eid", a.EmployeeId), ("@date", a.Date), ("@in", ToMinutes(a.CheckIn)), ("@out", ToMinutes(a.CheckOut)),
                ("@ot", a.OvertimeHours), ("@absent", a.IsAbsent), ("@notes", a.Notes ?? ""),
                ("@now", DateTime.Now), ("@by", a.UpdatedBy), ("@id", a.Id));

        public void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null) =>
            SoftDelete(id, deletedBy, conn, tx);
    }
}
