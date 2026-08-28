using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Query;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    // بنفس بنية ProductRepository حرفياً — كيان معاملات بسيط (سجل بلا محرك إهلاك، نطاق مقصود).
    public class AssetRepository : RepositoryBase<Asset>, IAssetRepository
    {
        protected override string TableName => "Assets";

        public void CreateTable() =>
            SchemaBuilder.Table("Assets")
                .Id()
                .Text("Code", 30, required: true, unique: true)
                .Text("Name", 200, required: true)
                .Int("CategoryId")
                .DateCol("PurchaseDate")
                .Decimal("PurchaseCost")
                .Decimal("CurrentValue")
                .Text("Location", 200)
                .Text("Notes")
                .Bool("IsActive", defaultValue: true)
                .Audit()
                .SoftDelete()
                .Concurrency()
                .Index("CategoryId")
                .Create();

        protected override Asset Map(DataRow row) => new()
        {
            Id           = Convert.ToInt32(row["Id"]),
            Code         = row["Code"].ToString(),
            Name         = row["Name"].ToString(),
            CategoryId   = row["CategoryId"] == DBNull.Value ? null : Convert.ToInt32(row["CategoryId"]),
            PurchaseDate = row["PurchaseDate"] == DBNull.Value ? null : Convert.ToDateTime(row["PurchaseDate"]),
            PurchaseCost = Convert.ToDecimal(row["PurchaseCost"]),
            CurrentValue = Convert.ToDecimal(row["CurrentValue"]),
            Location     = row["Location"] == DBNull.Value ? null : row["Location"].ToString(),
            Notes        = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            IsActive     = Convert.ToBoolean(row["IsActive"]),
            CreatedAt    = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy    = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
            UpdatedAt    = row["UpdatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["UpdatedAt"]),
            UpdatedBy    = row["UpdatedBy"] == DBNull.Value ? null : row["UpdatedBy"].ToString(),
            IsDeleted    = Convert.ToBoolean(row["IsDeleted"]),
            DeletedAt    = row["DeletedAt"] == DBNull.Value ? null : Convert.ToDateTime(row["DeletedAt"]),
            DeletedBy    = row["DeletedBy"] == DBNull.Value ? null : row["DeletedBy"].ToString(),
            RowVersion   = Convert.ToInt64(row["RowVersion"])
        };

        public override Asset GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Assets WHERE Id = @id AND IsDeleted = @d", conn, tx, ("@id", id), ("@d", false));

        public List<Asset> Search(string term, int maxResults) =>
            Query($@"SELECT * FROM Assets WHERE IsDeleted = @d AND IsActive = @a AND (Name LIKE @t OR Code LIKE @t)
                     ORDER BY Name {DbFactory.Current.LimitClause(0, maxResults)}",
                null, null, ("@d", false), ("@a", true), ("@t", $"%{term}%"));

        public (List<Asset> Items, int Total) GetPaged(
            int page, int pageSize, string searchText = null, bool? isActive = null, int? categoryId = null,
            string sortColumn = "Name", bool sortDescending = false)
        {
            var where = new WhereBuilder()
                .Eq("IsDeleted", false)
                .LikeAny(searchText, "Name", "Code")
                .Eq("IsActive", isActive)
                .Eq("CategoryId", categoryId);

            var column = sortColumn switch { "Code" => "Code", "PurchaseDate" => "PurchaseDate", "CreatedAt" => "CreatedAt", _ => "Name" };
            var direction = sortDescending ? "DESC" : "ASC";

            var total = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM Assets {where.Sql}", where.Parameters));

            var pageSql = $@"SELECT * FROM Assets {where.Sql}
                              ORDER BY {column} {direction}, Id {direction}
                              {DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";

            return (Query(pageSql, null, null, where.Parameters), total);
        }

        private const string InsertSql = @"
            INSERT INTO Assets
                (Code, Name, CategoryId, PurchaseDate, PurchaseCost, CurrentValue, Location, Notes, IsActive, CreatedBy)
            VALUES
                (@code, @name, @categoryId, @purchaseDate, @purchaseCost, @currentValue, @location, @notes, @isActive, @createdBy)";

        public int Insert(Asset a, DbConnection conn = null, DbTransaction tx = null) =>
            InsertGetId(InsertSql, conn, tx,
                ("@code", a.Code), ("@name", a.Name), ("@categoryId", a.CategoryId), ("@purchaseDate", a.PurchaseDate),
                ("@purchaseCost", a.PurchaseCost), ("@currentValue", a.CurrentValue), ("@location", a.Location ?? ""),
                ("@notes", a.Notes ?? ""), ("@isActive", a.IsActive), ("@createdBy", a.CreatedBy));

        private const string UpdateSql = @"
            UPDATE Assets SET
                Name = @name, CategoryId = @categoryId, PurchaseDate = @purchaseDate, PurchaseCost = @purchaseCost,
                CurrentValue = @currentValue, Location = @location, Notes = @notes, IsActive = @isActive,
                UpdatedAt = @now, UpdatedBy = @updatedBy
            WHERE Id = @id";

        public void Update(Asset a, DbConnection conn = null, DbTransaction tx = null) =>
            Exec(UpdateSql, conn, tx,
                ("@name", a.Name), ("@categoryId", a.CategoryId), ("@purchaseDate", a.PurchaseDate), ("@purchaseCost", a.PurchaseCost),
                ("@currentValue", a.CurrentValue), ("@location", a.Location ?? ""), ("@notes", a.Notes ?? ""), ("@isActive", a.IsActive),
                ("@now", DateTime.Now), ("@updatedBy", a.UpdatedBy), ("@id", a.Id));

        public void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null) =>
            Exec("UPDATE Assets SET IsDeleted = @d, DeletedAt = @now, DeletedBy = @by WHERE Id = @id",
                conn, tx, ("@d", true), ("@now", DateTime.Now), ("@by", deletedBy), ("@id", id));
    }
}
