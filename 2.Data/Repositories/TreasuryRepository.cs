using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    public interface ITreasuryRepository
    {
        void CreateTable();
        List<Treasury> GetAll(bool includeInactive = false);
        Treasury GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        int Insert(Treasury t);
        void Update(Treasury t);
        void Delete(int id);
    }

    public class TreasuryRepository : RepositoryBase<Treasury>, ITreasuryRepository
    {
        protected override string TableName => "Treasuries";

        public void CreateTable() =>
            SchemaBuilder.Table("Treasuries")
                .Id()
                .Text("Code", 30, required: true, unique: true)
                .Text("Name", 200, required: true)
                .Int("Kind", nullable: false, defaultValue: 1)
                .Text("AccountCode", 30)
                .Text("BankName", 200)
                .Text("AccountNumber", 60)
                .Text("Notes")
                .Bool("IsActive", defaultValue: true)
                .Audit()
                .Create();

        public List<Treasury> GetAll(bool includeInactive = false) =>
            includeInactive
                ? Query("SELECT * FROM Treasuries ORDER BY Code")
                : Query("SELECT * FROM Treasuries WHERE IsActive = @a ORDER BY Code", null, null, ("@a", true));

        public override Treasury GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Treasuries WHERE Id = @id", conn, tx, ("@id", id));

        public int Insert(Treasury t) =>
            InsertGetId(@"INSERT INTO Treasuries (Code, Name, Kind, AccountCode, BankName, AccountNumber, Notes, IsActive)
                          VALUES (@code, @name, @kind, @acc, @bank, @accno, @notes, @active)",
                null, null, ("@code", t.Code), ("@name", t.Name), ("@kind", (int)t.Kind), ("@acc", t.AccountCode ?? ""),
                ("@bank", t.BankName ?? ""), ("@accno", t.AccountNumber ?? ""), ("@notes", t.Notes ?? ""), ("@active", t.IsActive));

        public void Update(Treasury t) =>
            Exec(@"UPDATE Treasuries SET Name = @name, Kind = @kind, AccountCode = @acc, BankName = @bank,
                          AccountNumber = @accno, Notes = @notes, IsActive = @active, UpdatedAt = @now WHERE Id = @id",
                null, null, ("@name", t.Name), ("@kind", (int)t.Kind), ("@acc", t.AccountCode ?? ""), ("@bank", t.BankName ?? ""),
                ("@accno", t.AccountNumber ?? ""), ("@notes", t.Notes ?? ""), ("@active", t.IsActive), ("@now", DateTime.Now), ("@id", t.Id));

        public void Delete(int id) => Exec("UPDATE Treasuries SET IsActive = @a WHERE Id = @id", null, null, ("@a", false), ("@id", id));

        protected override Treasury Map(DataRow row) => new()
        {
            Id            = Convert.ToInt32(row["Id"]),
            Code          = row["Code"].ToString(),
            Name          = row["Name"].ToString(),
            Kind          = (TreasuryKind)Convert.ToInt32(row["Kind"]),
            AccountCode   = row["AccountCode"] == DBNull.Value ? null : row["AccountCode"].ToString(),
            BankName      = row["BankName"] == DBNull.Value ? null : row["BankName"].ToString(),
            AccountNumber = row["AccountNumber"] == DBNull.Value ? null : row["AccountNumber"].ToString(),
            Notes         = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            IsActive      = Convert.ToBoolean(row["IsActive"]),
        };
    }
}
