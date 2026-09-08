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
    public class PayrollRepository : RepositoryBase<Payroll>, IPayrollRepository
    {
        protected override string TableName => "Payrolls";

        public void CreateTable()
        {
            SchemaBuilder.Table("Payrolls")
                .Id().Text("PayrollNo", 30, required: true, unique: true)
                .DateCol("PeriodStart", nullable: false).DateCol("PeriodEnd", nullable: false).DateCol("PaymentDate", nullable: false)
                .Decimal("TotalBasic").Decimal("TotalAllowances").Decimal("TotalDeductions").Decimal("NetTotal")
                .Bool("IsPosted", defaultValue: true).Int("JournalEntryId").Text("Notes").Audit().Create();

            SchemaBuilder.Table("PayrollLines")
                .Id().Int("PayrollId", nullable: false).Int("EmployeeId", nullable: false).Text("EmployeeName", 200)
                .Decimal("BasicSalary").Decimal("Allowances").Decimal("Deductions").Decimal("NetSalary").Text("Notes")
                .ForeignKey("PayrollId", "Payrolls", "Id").Index("PayrollId").Create();
        }

        protected override Payroll Map(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["Id"]), PayrollNo = row["PayrollNo"].ToString(),
            PeriodStart = Convert.ToDateTime(row["PeriodStart"]), PeriodEnd = Convert.ToDateTime(row["PeriodEnd"]), PaymentDate = Convert.ToDateTime(row["PaymentDate"]),
            TotalBasic = Convert.ToDecimal(row["TotalBasic"]), TotalAllowances = Convert.ToDecimal(row["TotalAllowances"]),
            TotalDeductions = Convert.ToDecimal(row["TotalDeductions"]), NetTotal = Convert.ToDecimal(row["NetTotal"]),
            IsPosted = Convert.ToBoolean(row["IsPosted"]), JournalEntryId = row["JournalEntryId"] == DBNull.Value ? null : Convert.ToInt32(row["JournalEntryId"]),
            Notes = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            CreatedAt = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
        };

        private static PayrollLine MapLine(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["Id"]), PayrollId = Convert.ToInt32(row["PayrollId"]), EmployeeId = Convert.ToInt32(row["EmployeeId"]),
            EmployeeName = row["EmployeeName"] == DBNull.Value ? null : row["EmployeeName"].ToString(),
            BasicSalary = Convert.ToDecimal(row["BasicSalary"]), Allowances = Convert.ToDecimal(row["Allowances"]),
            Deductions = Convert.ToDecimal(row["Deductions"]), NetSalary = Convert.ToDecimal(row["NetSalary"]),
            Notes = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
        };

        public override Payroll GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Payrolls WHERE Id = @id", conn, tx, ("@id", id));

        public List<PayrollLine> GetLines(int payrollId, DbConnection conn = null, DbTransaction tx = null) =>
            QueryAs(MapLine, "SELECT * FROM PayrollLines WHERE PayrollId = @id", conn, tx, ("@id", payrollId));

        public (List<Payroll> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending)
        {
            var where = new WhereBuilder().LikeAny(searchText, "PayrollNo");
            var column = sortColumn == "PayrollNo" ? "PayrollNo" : "PaymentDate";
            var direction = sortDescending ? "DESC" : "ASC";

            var total = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM Payrolls {where.Sql}", where.Parameters));
            var pageSql = $@"SELECT * FROM Payrolls {where.Sql} {OrderBuilder.By(column, sortDescending, "PayrollNo", "CreatedAt")}
                              {DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";
            return (Query(pageSql, null, null, where.Parameters), total);
        }

        public int InsertHeader(DbConnection conn, DbTransaction tx, Payroll payroll) =>
            InsertGetId(@"INSERT INTO Payrolls (PayrollNo, PeriodStart, PeriodEnd, PaymentDate, TotalBasic, TotalAllowances, TotalDeductions, NetTotal, Notes, CreatedBy)
                          VALUES (@no, @start, @end, @pay, @basic, @allow, @ded, @net, @notes, @by)",
                conn, tx, ("@no", payroll.PayrollNo), ("@start", payroll.PeriodStart), ("@end", payroll.PeriodEnd), ("@pay", payroll.PaymentDate),
                ("@basic", payroll.TotalBasic), ("@allow", payroll.TotalAllowances), ("@ded", payroll.TotalDeductions), ("@net", payroll.NetTotal),
                ("@notes", payroll.Notes ?? ""), ("@by", payroll.CreatedBy));

        public void InsertLine(DbConnection conn, DbTransaction tx, int payrollId, PayrollLine line) =>
            Exec(@"INSERT INTO PayrollLines (PayrollId, EmployeeId, EmployeeName, BasicSalary, Allowances, Deductions, NetSalary, Notes)
                  VALUES (@pid, @eid, @ename, @basic, @allow, @ded, @net, @notes)",
                conn, tx, ("@pid", payrollId), ("@eid", line.EmployeeId), ("@ename", line.EmployeeName ?? ""), ("@basic", line.BasicSalary),
                ("@allow", line.Allowances), ("@ded", line.Deductions), ("@net", line.NetSalary), ("@notes", line.Notes ?? ""));

        public void SetJournalEntryId(DbConnection conn, DbTransaction tx, int payrollId, int journalEntryId) =>
            Exec("UPDATE Payrolls SET JournalEntryId = @jid WHERE Id = @id", conn, tx, ("@jid", journalEntryId), ("@id", payrollId));
    }
}
