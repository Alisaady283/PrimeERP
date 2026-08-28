using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public class UnitRepository : RepositoryBase<Unit>, IUnitRepository
    {
        protected override string TableName => "Units";

        public void CreateTable() =>
            SchemaBuilder.Table("Units")
                .Id()
                .Text("Name", 200, required: true)
                .Text("NameEn", 200)
                .Text("Symbol", 20)
                .Bool("IsActive", defaultValue: true)
                .Audit()
                .Create();

        public List<Unit> GetAll(bool includeInactive = false) =>
            includeInactive
                ? Query("SELECT * FROM Units ORDER BY Name")
                : Query("SELECT * FROM Units WHERE IsActive = @a ORDER BY Name", null, null, ("@a", true));

        public override Unit GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Units WHERE Id = @id", conn, tx, ("@id", id));

        public int Insert(Unit u) =>
            InsertGetId("INSERT INTO Units (Name, NameEn, Symbol, IsActive) VALUES (@name, @nameEn, @symbol, @active)",
                null, null, ("@name", u.Name), ("@nameEn", u.NameEn), ("@symbol", u.Symbol), ("@active", u.IsActive));

        public void Update(Unit u) =>
            Exec("UPDATE Units SET Name = @name, NameEn = @nameEn, Symbol = @symbol, IsActive = @active, UpdatedAt = @now WHERE Id = @id",
                null, null, ("@name", u.Name), ("@nameEn", u.NameEn), ("@symbol", u.Symbol), ("@active", u.IsActive), ("@now", DateTime.Now), ("@id", u.Id));

        public void Delete(int id) => Exec("UPDATE Units SET IsActive = @a WHERE Id = @id", null, null, ("@a", false), ("@id", id));

        protected override Unit Map(DataRow row) => new()
        {
            Id       = Convert.ToInt32(row["Id"]),
            Name     = row["Name"].ToString(),
            NameEn   = row["NameEn"] == DBNull.Value ? null : row["NameEn"].ToString(),
            Symbol   = row["Symbol"] == DBNull.Value ? null : row["Symbol"].ToString(),
            IsActive = Convert.ToBoolean(row["IsActive"]),
        };
    }
}
