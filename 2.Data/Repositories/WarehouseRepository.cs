using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public class WarehouseRepository : RepositoryBase<Warehouse>, IWarehouseRepository
    {
        protected override string TableName => "Warehouses";

        public void CreateTable() =>
            SchemaBuilder.Table("Warehouses")
                .Id()
                .Text("Code", 30, required: true, unique: true)
                .Text("Name", 200, required: true)
                .Text("Location", 200)
                .Text("ManagerName", 150)
                .Bool("IsActive", defaultValue: true)
                .Audit()
                .Create();

        public List<Warehouse> GetAll(bool includeInactive = false) =>
            includeInactive
                ? Query("SELECT * FROM Warehouses ORDER BY Name")
                : Query("SELECT * FROM Warehouses WHERE IsActive = @a ORDER BY Name", null, null, ("@a", true));

        public override Warehouse GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Warehouses WHERE Id = @id", conn, tx, ("@id", id));

        public int Insert(Warehouse w) =>
            InsertGetId("INSERT INTO Warehouses (Code, Name, Location, ManagerName, IsActive) VALUES (@code, @name, @location, @manager, @active)",
                null, null, ("@code", w.Code), ("@name", w.Name), ("@location", w.Location), ("@manager", w.ManagerName), ("@active", w.IsActive));

        public void Update(Warehouse w) =>
            Exec("UPDATE Warehouses SET Name = @name, Location = @location, ManagerName = @manager, IsActive = @active, UpdatedAt = @now WHERE Id = @id",
                null, null, ("@name", w.Name), ("@location", w.Location), ("@manager", w.ManagerName), ("@active", w.IsActive), ("@now", DateTime.Now), ("@id", w.Id));

        public void Delete(int id) => Exec("UPDATE Warehouses SET IsActive = @a WHERE Id = @id", null, null, ("@a", false), ("@id", id));

        protected override Warehouse Map(DataRow row) => new()
        {
            Id          = Convert.ToInt32(row["Id"]),
            Code        = row["Code"].ToString(),
            Name        = row["Name"].ToString(),
            Location    = row["Location"] == DBNull.Value ? null : row["Location"].ToString(),
            ManagerName = row["ManagerName"] == DBNull.Value ? null : row["ManagerName"].ToString(),
            IsActive    = Convert.ToBoolean(row["IsActive"]),
        };
    }
}
