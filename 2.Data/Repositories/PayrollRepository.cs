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
                // المسير يُنشأ مسوّدةً ويُرحَّل بزرّه — كان الافتراضيّ true حين كان يُرحَّل فور إنشائه.
                .Bool("IsPosted", defaultValue: false).Int("JournalEntryId").Text("Notes").Audit().Create();

            SchemaBuilder.Table("PayrollLines")
                .Id().Int("PayrollId", nullable: false).Int("EmployeeId", nullable: false).Text("EmployeeName", 200)
                .Decimal("BasicSalary").Decimal("Allowances").Decimal("Overtime")
                .Decimal("Deductions").Decimal("Advances").Decimal("Insurance").Decimal("Tax")
                .Decimal("NetSalary").Text("Notes")
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
            Overtime = Convert.ToDecimal(row["Overtime"]),
            Deductions = Convert.ToDecimal(row["Deductions"]), Advances = Convert.ToDecimal(row["Advances"]),
            Insurance = Convert.ToDecimal(row["Insurance"]), Tax = Convert.ToDecimal(row["Tax"]),
            NetSalary = Convert.ToDecimal(row["NetSalary"]),
            Notes = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
        };

        // الحذف من RepositoryBase — هنا إعلان جدول السطور ومفتاحه فقط.
        protected override string LineTable => "PayrollLines";
        protected override string LineForeignKey => "PayrollId";

        public void DeleteDocument(DbConnection conn, DbTransaction tx, int id) => HardDelete(id, conn, tx);

        public override Payroll GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Payrolls WHERE Id = @id", conn, tx, ("@id", id));

        public List<PayrollLine> GetLines(int payrollId, DbConnection conn = null, DbTransaction tx = null) =>
            QueryAs(MapLine, "SELECT * FROM PayrollLines WHERE PayrollId = @id", conn, tx, ("@id", payrollId));

        public (List<Payroll> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending)
        {
            var where = new WhereBuilder().LikeAny(searchText, "PayrollNo");
            var column = sortColumn == "PayrollNo" ? "PayrollNo" : "PaymentDate";
            return Page(where, page, pageSize, OrderBuilder.By(column, sortDescending, "PayrollNo", "CreatedAt"));
        }

        public int InsertHeader(DbConnection conn, DbTransaction tx, Payroll payroll) =>
            InsertGetId(@"INSERT INTO Payrolls (PayrollNo, PeriodStart, PeriodEnd, PaymentDate, TotalBasic, TotalAllowances, TotalDeductions, NetTotal, IsPosted, Notes, CreatedBy)
                          VALUES (@no, @start, @end, @pay, @basic, @allow, @ded, @net, @posted, @notes, @by)",
                conn, tx, ("@no", payroll.PayrollNo), ("@start", payroll.PeriodStart), ("@end", payroll.PeriodEnd), ("@pay", payroll.PaymentDate),
                ("@basic", payroll.TotalBasic), ("@allow", payroll.TotalAllowances), ("@ded", payroll.TotalDeductions), ("@net", payroll.NetTotal),
                ("@posted", payroll.IsPosted), ("@notes", payroll.Notes ?? ""), ("@by", payroll.CreatedBy));

        public void InsertLine(DbConnection conn, DbTransaction tx, int payrollId, PayrollLine line) =>
            Exec(@"INSERT INTO PayrollLines
                      (PayrollId, EmployeeId, EmployeeName, BasicSalary, Allowances, Overtime,
                       Deductions, Advances, Insurance, Tax, NetSalary, Notes)
                  VALUES (@pid, @eid, @ename, @basic, @allow, @over, @ded, @adv, @ins, @tax, @net, @notes)",
                conn, tx, ("@pid", payrollId), ("@eid", line.EmployeeId), ("@ename", line.EmployeeName ?? ""),
                ("@basic", line.BasicSalary), ("@allow", line.Allowances), ("@over", line.Overtime),
                ("@ded", line.Deductions), ("@adv", line.Advances), ("@ins", line.Insurance), ("@tax", line.Tax),
                ("@net", line.NetSalary), ("@notes", line.Notes ?? ""));

        public void SetJournalEntryId(DbConnection conn, DbTransaction tx, int payrollId, int? journalEntryId) =>
            Exec("UPDATE Payrolls SET JournalEntryId = @jid WHERE Id = @id", conn, tx, ("@jid", journalEntryId), ("@id", payrollId));

        public void SetPosted(DbConnection conn, DbTransaction tx, int payrollId, bool posted) =>
            Exec("UPDATE Payrolls SET IsPosted = @p WHERE Id = @id", conn, tx, ("@p", posted), ("@id", payrollId));
    }
}
