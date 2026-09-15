using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    // فئات مشتركة عبر عدة وحدات (ModuleKey مميّز) — بنفس اتفاقية AccountRepository (بلا SoftDelete/Concurrency،
    // بيانات هيكلية منخفضة الكتابة).
    public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
    {
        protected override string TableName => "Categories";

        public void CreateTable() =>
            SchemaBuilder.Table("Categories")
                .Id()
                .Text("Name", 200, required: true)
                .Int("ParentId")
                .Text("ModuleKey", 30, required: true)
                .Bool("IsActive", defaultValue: true)
                .Text("Notes")
                .Text("AccountCode", 30)
                .Text("DepAccountCode", 30)
                .Audit()
                .Index("ModuleKey")
                .Index("ParentId")
                .Create();

        public List<Category> GetAll(string moduleKey, bool includeInactive = false) =>
            includeInactive
                ? Query("SELECT * FROM Categories WHERE ModuleKey = @m ORDER BY Name", null, null, ("@m", moduleKey))
                : Query("SELECT * FROM Categories WHERE ModuleKey = @m AND IsActive = @a ORDER BY Name", null, null, ("@m", moduleKey), ("@a", true));

        public bool HasChildren(int id) =>
            Convert.ToInt64(Scalar("SELECT COUNT(*) FROM Categories WHERE ParentId = @p AND IsActive = @a", ("@p", id), ("@a", true))) > 0;

        public int Insert(Category c, DbConnection conn = null, DbTransaction tx = null) =>
            InsertGetId(
                "INSERT INTO Categories (Name, ParentId, ModuleKey, IsActive, Notes, AccountCode, DepAccountCode) VALUES (@name, @parent, @module, @active, @notes, @account, @dep)",
                conn, tx, ("@name", c.Name), ("@parent", c.ParentId), ("@module", c.ModuleKey), ("@active", c.IsActive),
                ("@notes", c.Notes ?? ""), ("@account", c.AccountCode ?? ""), ("@dep", c.DepreciationAccountCode ?? ""));

        public void Update(Category c, DbConnection conn = null, DbTransaction tx = null) =>
            Exec(@"UPDATE Categories SET Name = @name, ParentId = @parent, IsActive = @active, Notes = @notes,
                          AccountCode = @account, DepAccountCode = @dep, UpdatedAt = @now WHERE Id = @id",
                conn, tx, ("@name", c.Name), ("@parent", c.ParentId), ("@active", c.IsActive), ("@notes", c.Notes ?? ""),
                ("@account", c.AccountCode ?? ""), ("@dep", c.DepreciationAccountCode ?? ""),
                ("@now", DateTime.Now), ("@id", c.Id));

        public void Delete(int id, DbConnection conn = null, DbTransaction tx = null) =>
            Exec("UPDATE Categories SET IsActive = @a WHERE Id = @id", conn, tx, ("@a", false), ("@id", id));

        protected override Category Map(DataRow row) => new()
        {
            Id        = Convert.ToInt32(row["Id"]),
            Name      = row["Name"].ToString(),
            ParentId  = row["ParentId"] == DBNull.Value ? null : Convert.ToInt32(row["ParentId"]),
            ModuleKey = row["ModuleKey"].ToString(),
            AccountCode = row["AccountCode"] == DBNull.Value ? null : row["AccountCode"].ToString(),
            DepreciationAccountCode = row["DepAccountCode"] == DBNull.Value ? null : row["DepAccountCode"].ToString(),
            IsActive  = Convert.ToBoolean(row["IsActive"]),
            Notes     = row["Notes"] == DBNull.Value ? "" : row["Notes"].ToString(),
        };
    }
}
