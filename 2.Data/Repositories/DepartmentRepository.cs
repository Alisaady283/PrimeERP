using System;
using System.Collections.Generic;
using System.Data;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    // بنفس اتفاقية CategoryRepository — بيانات هيكلية منخفضة الكتابة، بلا SoftDelete/Concurrency.
    public class DepartmentRepository : RepositoryBase<Department>, IDepartmentRepository
    {
        protected override string TableName => "Departments";

        public void CreateTable() =>
            SchemaBuilder.Table("Departments")
                .Id()
                .Text("Name", 200, required: true)
                .Text("NameEn", 200)
                .Int("ManagerId")
                .Bool("IsActive", defaultValue: true)
                .Audit()
                .Create();

        public List<Department> GetAll(bool includeInactive = false) =>
            includeInactive
                ? Query("SELECT * FROM Departments ORDER BY Name")
                : Query("SELECT * FROM Departments WHERE IsActive = @a ORDER BY Name", null, null, ("@a", true));

        public override Department GetById(int id, System.Data.Common.DbConnection conn = null, System.Data.Common.DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Departments WHERE Id = @id", conn, tx, ("@id", id));

        public int Insert(Department d) =>
            InsertGetId("INSERT INTO Departments (Name, NameEn, ManagerId, IsActive) VALUES (@name, @nameEn, @managerId, @active)",
                null, null, ("@name", d.Name), ("@nameEn", d.NameEn), ("@managerId", d.ManagerId), ("@active", d.IsActive));

        public void Update(Department d) =>
            Exec("UPDATE Departments SET Name = @name, NameEn = @nameEn, ManagerId = @managerId, IsActive = @active, UpdatedAt = @now WHERE Id = @id",
                null, null, ("@name", d.Name), ("@nameEn", d.NameEn), ("@managerId", d.ManagerId), ("@active", d.IsActive), ("@now", DateTime.Now), ("@id", d.Id));

        public void Delete(int id) => Exec("UPDATE Departments SET IsActive = @a WHERE Id = @id", null, null, ("@a", false), ("@id", id));

        protected override Department Map(DataRow row) => new()
        {
            Id        = Convert.ToInt32(row["Id"]),
            Name      = row["Name"].ToString(),
            NameEn    = row["NameEn"] == DBNull.Value ? null : row["NameEn"].ToString(),
            ManagerId = row["ManagerId"] == DBNull.Value ? null : Convert.ToInt32(row["ManagerId"]),
            IsActive  = Convert.ToBoolean(row["IsActive"]),
        };
    }
}
