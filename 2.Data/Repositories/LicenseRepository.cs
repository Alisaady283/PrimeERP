using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface ILicenseRepository
    {
        void CreateTable();
        List<License> GetAll(bool includeInactive = false);
        License GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        License GetBySerial(string serial);
        int  Insert(License license);
        void Update(License license);
        void Delete(int id);
    }

    public class LicenseRepository : RepositoryBase<License>, ILicenseRepository
    {
        protected override string TableName => "Licenses";

        public void CreateTable() =>
            SchemaBuilder.Table("Licenses")
                .Id()
                .Text("CustomerName", 200, required: true)
                .Text("Location", 200)
                .Text("Serial", 40, required: true, unique: true)
                .Text("Manifest", 4000)
                .Bool("Simplified")
                .Text("MachineHash", 100)
                .Bool("IsActive", defaultValue: true)
                .Audit()
                .Create();

        public List<License> GetAll(bool includeInactive = false) =>
            includeInactive
                ? Query("SELECT * FROM Licenses ORDER BY CustomerName")
                : Query("SELECT * FROM Licenses WHERE IsActive = @a ORDER BY CustomerName", null, null, ("@a", true));

        public override License GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Licenses WHERE Id = @id", conn, tx, ("@id", id));

        public License GetBySerial(string serial) =>
            QueryOne("SELECT * FROM Licenses WHERE Serial = @s", null, null, ("@s", serial));

        public int Insert(License l) =>
            InsertGetId(@"INSERT INTO Licenses (CustomerName, Location, Serial, Manifest, Simplified, MachineHash, IsActive, CreatedAt, CreatedBy)
                          VALUES (@name, @loc, @serial, @manifest, @simple, @machine, @active, @now, @by)",
                null, null, ("@name", l.CustomerName), ("@loc", l.Location ?? ""), ("@serial", l.Serial),
                ("@manifest", l.Manifest ?? ""), ("@simple", l.Simplified), ("@machine", l.MachineHash ?? ""),
                ("@active", l.IsActive), ("@now", DateTime.Now), ("@by", l.CreatedBy ?? ""));

        public void Update(License l) =>
            Exec(@"UPDATE Licenses SET CustomerName = @name, Location = @loc, Manifest = @manifest,
                          Simplified = @simple, MachineHash = @machine, IsActive = @active, UpdatedAt = @now
                   WHERE Id = @id",
                null, null, ("@name", l.CustomerName), ("@loc", l.Location ?? ""), ("@manifest", l.Manifest ?? ""),
                ("@simple", l.Simplified), ("@machine", l.MachineHash ?? ""), ("@active", l.IsActive),
                ("@now", DateTime.Now), ("@id", l.Id));

        public void Delete(int id) => SoftDelete(id);

        protected override License Map(DataRow row) => new()
        {
            Id           = Convert.ToInt32(row["Id"]),
            CustomerName = row["CustomerName"]?.ToString(),
            Location     = row["Location"]?.ToString(),
            Serial       = row["Serial"]?.ToString(),
            Manifest     = row["Manifest"]?.ToString(),
            Simplified   = row["Simplified"] != DBNull.Value && Convert.ToBoolean(row["Simplified"]),
            MachineHash  = row["MachineHash"]?.ToString(),
            IsActive     = row["IsActive"] == DBNull.Value || Convert.ToBoolean(row["IsActive"]),
            CreatedAt    = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"])
        };
    }
}
