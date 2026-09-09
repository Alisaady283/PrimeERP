using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Query;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    // بنفس بنية ProductRepository — DepartmentName/JobTitleName على Employee ليسا عمودين هنا (يُملآن في
    // EmployeeService.ToDto عبر ICategoryRepository، نفس نمط Product.CategoryName).
    public class EmployeeRepository : RepositoryBase<Employee>, IEmployeeRepository
    {
        protected override string TableName => "Employees";

        public void CreateTable() =>
            SchemaBuilder.Table("Employees")
                .Id()
                .Text("Code", 30, required: true, unique: true)
                .Text("Name", 200, required: true)
                .Text("NameEn", 200)
                .Text("NationalId", 30)
                .Text("Phone", 30)
                .Text("Email", 120)
                .Text("Address", 300)
                .Int("DepartmentId")
                .Int("JobTitleId")
                .DateCol("HireDate", nullable: false)
                .DateCol("TerminationDate")
                .Decimal("BasicSalary")
                .Int("AccountId")
                .Text("BankAccount", 60)
                .Int("Status", nullable: false, defaultValue: (int)EmployeeStatus.Active)
                .Text("Notes")
                .Audit()
                .SoftDelete()
                .Concurrency()
                .Index("DepartmentId")
                .Create();

        protected override Employee Map(DataRow row) => new()
        {
            Id              = Convert.ToInt32(row["Id"]),
            Code            = row["Code"].ToString(),
            Name            = row["Name"].ToString(),
            NameEn          = row["NameEn"] == DBNull.Value ? null : row["NameEn"].ToString(),
            NationalId      = row["NationalId"] == DBNull.Value ? null : row["NationalId"].ToString(),
            Phone           = row["Phone"] == DBNull.Value ? null : row["Phone"].ToString(),
            Email           = row["Email"] == DBNull.Value ? null : row["Email"].ToString(),
            Address         = row["Address"] == DBNull.Value ? null : row["Address"].ToString(),
            DepartmentId    = row["DepartmentId"] == DBNull.Value ? null : Convert.ToInt32(row["DepartmentId"]),
            JobTitleId      = row["JobTitleId"] == DBNull.Value ? null : Convert.ToInt32(row["JobTitleId"]),
            HireDate        = Convert.ToDateTime(row["HireDate"]),
            TerminationDate = row["TerminationDate"] == DBNull.Value ? null : Convert.ToDateTime(row["TerminationDate"]),
            BasicSalary     = Convert.ToDecimal(row["BasicSalary"]),
            AccountId       = row["AccountId"] == DBNull.Value ? null : Convert.ToInt32(row["AccountId"]),
            BankAccount     = row["BankAccount"] == DBNull.Value ? null : row["BankAccount"].ToString(),
            Status          = (EmployeeStatus)Convert.ToInt32(row["Status"]),
            Notes           = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            CreatedAt       = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy       = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
            UpdatedAt       = row["UpdatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["UpdatedAt"]),
            UpdatedBy       = row["UpdatedBy"] == DBNull.Value ? null : row["UpdatedBy"].ToString(),
            IsDeleted       = Convert.ToBoolean(row["IsDeleted"]),
            DeletedAt       = row["DeletedAt"] == DBNull.Value ? null : Convert.ToDateTime(row["DeletedAt"]),
            DeletedBy       = row["DeletedBy"] == DBNull.Value ? null : row["DeletedBy"].ToString(),
            RowVersion      = Convert.ToInt64(row["RowVersion"])
        };

        public override Employee GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Employees WHERE Id = @id AND IsDeleted = @d", conn, tx, ("@id", id), ("@d", false));

        public Employee GetByCode(string code, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Employees WHERE Code = @code AND IsDeleted = @d", conn, tx, ("@code", code), ("@d", false));

        public List<Employee> Search(string term, int maxResults) =>
            Query($@"SELECT * FROM Employees WHERE IsDeleted = @d AND (Name LIKE @t OR Code LIKE @t)
                     ORDER BY Name {DbFactory.Current.LimitClause(0, maxResults)}",
                null, null, ("@d", false), ("@t", $"%{term}%"));

        public (List<Employee> Items, int Total) GetPaged(
            int page, int pageSize, string searchText = null, int? departmentId = null,
            string sortColumn = "Name", bool sortDescending = false)
        {
            var where = new WhereBuilder()
                .Eq("IsDeleted", false)
                .LikeAny(searchText, "Name", "Code")
                .Eq("DepartmentId", departmentId);

            var column = sortColumn switch { "Name" => "Name", "HireDate" => "HireDate", "CreatedAt" => "CreatedAt", _ => "Code" };
            var direction = sortDescending ? "DESC" : "ASC";

            var total = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM Employees {where.Sql}", where.Parameters));

            var pageSql = $@"SELECT * FROM Employees {where.Sql}
                              {OrderBuilder.By(column, sortDescending)}
                              {DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";

            return (Query(pageSql, null, null, where.Parameters), total);
        }

        private const string InsertSql = @"
            INSERT INTO Employees
                (Code, Name, NameEn, NationalId, Phone, Email, Address, DepartmentId, JobTitleId, HireDate,
                 TerminationDate, BasicSalary, AccountId, BankAccount, Status, Notes, CreatedBy)
            VALUES
                (@code, @name, @nameEn, @nationalId, @phone, @email, @address, @departmentId, @jobTitleId, @hireDate,
                 @terminationDate, @basicSalary, @accountId, @bankAccount, @status, @notes, @createdBy)";

        public int Insert(Employee e, DbConnection conn = null, DbTransaction tx = null) =>
            InsertGetId(InsertSql, conn, tx,
                ("@code", e.Code), ("@name", e.Name), ("@nameEn", e.NameEn), ("@nationalId", e.NationalId),
                ("@phone", e.Phone), ("@email", e.Email), ("@address", e.Address), ("@departmentId", e.DepartmentId),
                ("@jobTitleId", e.JobTitleId), ("@hireDate", e.HireDate), ("@terminationDate", e.TerminationDate),
                ("@basicSalary", e.BasicSalary), ("@accountId", e.AccountId), ("@bankAccount", e.BankAccount),
                ("@status", (int)e.Status), ("@notes", e.Notes ?? ""), ("@createdBy", e.CreatedBy));

        private const string UpdateSql = @"
            UPDATE Employees SET
                Name = @name, NameEn = @nameEn, NationalId = @nationalId, Phone = @phone, Email = @email, Address = @address,
                DepartmentId = @departmentId, JobTitleId = @jobTitleId, HireDate = @hireDate, TerminationDate = @terminationDate,
                BasicSalary = @basicSalary, AccountId = @accountId, BankAccount = @bankAccount, Status = @status,
                Notes = @notes, UpdatedAt = @now, UpdatedBy = @updatedBy
            WHERE Id = @id";

        public void Update(Employee e, DbConnection conn = null, DbTransaction tx = null) =>
            Exec(UpdateSql, conn, tx,
                ("@name", e.Name), ("@nameEn", e.NameEn), ("@nationalId", e.NationalId), ("@phone", e.Phone),
                ("@email", e.Email), ("@address", e.Address), ("@departmentId", e.DepartmentId), ("@jobTitleId", e.JobTitleId),
                ("@hireDate", e.HireDate), ("@terminationDate", e.TerminationDate), ("@basicSalary", e.BasicSalary),
                ("@accountId", e.AccountId), ("@bankAccount", e.BankAccount), ("@status", (int)e.Status), ("@notes", e.Notes ?? ""),
                ("@now", DateTime.Now), ("@updatedBy", e.UpdatedBy), ("@id", e.Id));

        public void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null) =>
            SoftDelete(id, deletedBy, conn, tx);
    }
}
