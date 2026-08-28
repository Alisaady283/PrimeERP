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
    // طبقة وصول بيانات الأصناف — SQL خام ↔ Model فقط، بلا تحقق/معاملات ذاتية. الحقول المرتبطة بوحدات لم
    // تُبنَ بعد (Unit/TaxGroup/الحسابات المحاسبية) موجودة في المخطط (تطابق Product.cs الفعلية) لكن غير
    // مُستهلَكة من الحوار الحالي — "جدول عادي فقط" كما طُلب، بلا واجهة لها الآن.
    public class ProductRepository : RepositoryBase<Product>, IProductRepository
    {
        protected override string TableName => "Products";

        public void CreateTable() =>
            SchemaBuilder.Table("Products")
                .Id()
                .Text("Code", 30, required: true, unique: true)
                .Text("Barcode", 60)
                .Text("Name", 200, required: true)
                .Text("NameEn", 200)
                .Int("CategoryId")
                .Int("BrandId")
                .Int("UnitId")
                .Decimal("CostPrice")
                .Decimal("SalePrice")
                .Decimal("MinPrice")
                .Decimal("MinQty")
                .Decimal("MaxQty")
                .Decimal("ReorderPoint")
                .Int("CostMethod", nullable: false, defaultValue: (int)CostMethod.WeightedAverage)
                .Int("TaxGroupId")
                .Int("InventoryAccountId")
                .Int("SalesAccountId")
                .Int("CostAccountId")
                .Bool("IsStockTracked", defaultValue: true)
                .Bool("IsService", defaultValue: false)
                .Text("Notes")
                .Bool("IsActive", defaultValue: true)
                .Audit()
                .SoftDelete()
                .Concurrency()
                .Index("CategoryId")
                .Index("BrandId")
                .Create();

        protected override Product Map(DataRow row) => new()
        {
            Id                 = Convert.ToInt32(row["Id"]),
            Code               = row["Code"].ToString(),
            Barcode            = row["Barcode"] == DBNull.Value ? null : row["Barcode"].ToString(),
            Name               = row["Name"].ToString(),
            NameEn             = row["NameEn"] == DBNull.Value ? null : row["NameEn"].ToString(),
            CategoryId         = row["CategoryId"] == DBNull.Value ? null : Convert.ToInt32(row["CategoryId"]),
            BrandId            = row["BrandId"] == DBNull.Value ? null : Convert.ToInt32(row["BrandId"]),
            UnitId             = row["UnitId"] == DBNull.Value ? null : Convert.ToInt32(row["UnitId"]),
            CostPrice          = Convert.ToDecimal(row["CostPrice"]),
            SalePrice          = Convert.ToDecimal(row["SalePrice"]),
            MinPrice           = Convert.ToDecimal(row["MinPrice"]),
            MinQty             = Convert.ToDecimal(row["MinQty"]),
            MaxQty             = Convert.ToDecimal(row["MaxQty"]),
            ReorderPoint       = Convert.ToDecimal(row["ReorderPoint"]),
            CostMethod         = (CostMethod)Convert.ToInt32(row["CostMethod"]),
            TaxGroupId         = row["TaxGroupId"] == DBNull.Value ? null : Convert.ToInt32(row["TaxGroupId"]),
            InventoryAccountId = row["InventoryAccountId"] == DBNull.Value ? null : Convert.ToInt32(row["InventoryAccountId"]),
            SalesAccountId     = row["SalesAccountId"] == DBNull.Value ? null : Convert.ToInt32(row["SalesAccountId"]),
            CostAccountId      = row["CostAccountId"] == DBNull.Value ? null : Convert.ToInt32(row["CostAccountId"]),
            IsStockTracked     = Convert.ToBoolean(row["IsStockTracked"]),
            IsService          = Convert.ToBoolean(row["IsService"]),
            Notes              = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            IsActive           = Convert.ToBoolean(row["IsActive"]),
            CreatedAt          = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy          = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
            UpdatedAt          = row["UpdatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["UpdatedAt"]),
            UpdatedBy          = row["UpdatedBy"] == DBNull.Value ? null : row["UpdatedBy"].ToString(),
            IsDeleted          = Convert.ToBoolean(row["IsDeleted"]),
            DeletedAt          = row["DeletedAt"] == DBNull.Value ? null : Convert.ToDateTime(row["DeletedAt"]),
            DeletedBy          = row["DeletedBy"] == DBNull.Value ? null : row["DeletedBy"].ToString(),
            RowVersion         = Convert.ToInt64(row["RowVersion"])
        };

        public override Product GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Products WHERE Id = @id AND IsDeleted = @d", conn, tx, ("@id", id), ("@d", false));

        public Product GetByCode(string code, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Products WHERE Code = @code AND IsDeleted = @d", conn, tx, ("@code", code), ("@d", false));

        public List<Product> Search(string term, int maxResults) =>
            Query($@"SELECT * FROM Products WHERE IsDeleted = @d AND IsActive = @a AND (Name LIKE @t OR Code LIKE @t OR Barcode LIKE @t)
                     ORDER BY Name {DbFactory.Current.LimitClause(0, maxResults)}",
                null, null, ("@d", false), ("@a", true), ("@t", $"%{term}%"));

        public bool ExistsCode(string code, int? excludeId = null)
        {
            var where = new WhereBuilder().Eq("Code", code).Eq("IsDeleted", false).RawWithParam(p => $"Id != {p}", excludeId);
            return Convert.ToInt64(Scalar($"SELECT COUNT(*) FROM Products {where.Sql}", where.Parameters)) > 0;
        }

        public (List<Product> Items, int Total) GetPaged(
            int page, int pageSize, string searchText = null, bool? isActive = null, int? categoryId = null,
            string sortColumn = "Name", bool sortDescending = false)
        {
            var where = new WhereBuilder()
                .Eq("IsDeleted", false)
                .LikeAny(searchText, "Name", "Code", "Barcode")
                .Eq("IsActive", isActive)
                .Eq("CategoryId", categoryId);

            var column = sortColumn switch { "Code" => "Code", "SalePrice" => "SalePrice", "CreatedAt" => "CreatedAt", _ => "Name" };
            var direction = sortDescending ? "DESC" : "ASC";

            var total = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM Products {where.Sql}", where.Parameters));

            var pageSql = $@"SELECT * FROM Products {where.Sql}
                              ORDER BY {column} {direction}, Id {direction}
                              {DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";

            return (Query(pageSql, null, null, where.Parameters), total);
        }

        private const string InsertSql = @"
            INSERT INTO Products
                (Code, Barcode, Name, NameEn, CategoryId, BrandId, CostPrice, SalePrice, MinPrice, Notes, IsActive, CreatedBy)
            VALUES
                (@code, @barcode, @name, @nameEn, @categoryId, @brandId, @costPrice, @salePrice, @minPrice, @notes, @isActive, @createdBy)";

        public int Insert(Product p, DbConnection conn = null, DbTransaction tx = null) =>
            InsertGetId(InsertSql, conn, tx,
                ("@code", p.Code), ("@barcode", p.Barcode), ("@name", p.Name), ("@nameEn", p.NameEn),
                ("@categoryId", p.CategoryId), ("@brandId", p.BrandId), ("@costPrice", p.CostPrice), ("@salePrice", p.SalePrice),
                ("@minPrice", p.MinPrice), ("@notes", p.Notes ?? ""), ("@isActive", p.IsActive), ("@createdBy", p.CreatedBy));

        private const string UpdateSql = @"
            UPDATE Products SET
                Name = @name, NameEn = @nameEn, CategoryId = @categoryId, BrandId = @brandId, CostPrice = @costPrice, SalePrice = @salePrice,
                MinPrice = @minPrice, Notes = @notes, IsActive = @isActive, UpdatedAt = @now, UpdatedBy = @updatedBy
            WHERE Id = @id";

        public void Update(Product p, DbConnection conn = null, DbTransaction tx = null) =>
            Exec(UpdateSql, conn, tx,
                ("@name", p.Name), ("@nameEn", p.NameEn), ("@categoryId", p.CategoryId), ("@brandId", p.BrandId), ("@costPrice", p.CostPrice),
                ("@salePrice", p.SalePrice), ("@minPrice", p.MinPrice), ("@notes", p.Notes ?? ""), ("@isActive", p.IsActive),
                ("@now", DateTime.Now), ("@updatedBy", p.UpdatedBy), ("@id", p.Id));

        public void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null) =>
            Exec("UPDATE Products SET IsDeleted = @d, DeletedAt = @now, DeletedBy = @by WHERE Id = @id",
                conn, tx, ("@d", true), ("@now", DateTime.Now), ("@by", deletedBy), ("@id", id));
    }
}
