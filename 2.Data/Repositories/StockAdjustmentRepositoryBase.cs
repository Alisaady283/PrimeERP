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
    // أساس مشترك لـStockInRepository/StockOutRepository — نفس الشكل حرفياً، الفرق فقط اسما الجدولين
    // (يُمرَّران في المُنشئ) — بلا تكرار كامل لملفين متطابقين.
    public abstract class StockAdjustmentRepositoryBase : RepositoryBase<StockAdjustment>
    {
        private readonly string _headerTable, _lineTable;
        protected StockAdjustmentRepositoryBase(string headerTable, string lineTable) { _headerTable = headerTable; _lineTable = lineTable; }
        protected override string TableName => _headerTable;

        public void CreateTable()
        {
            SchemaBuilder.Table(_headerTable)
                .Id().Text("DocNo", 30, required: true, unique: true).DateCol("MovementDate", nullable: false)
                .Int("WarehouseId", nullable: false).Text("Notes").Audit().Create();

            SchemaBuilder.Table(_lineTable)
                .Id().Int("DocumentId", nullable: false).Int("LineNo", nullable: false, defaultValue: 1)
                .Int("ProductId", nullable: false).Text("ProductCode", 30).Text("ProductName", 200)
                .Decimal("Qty").Decimal("UnitCost").Text("Notes")
                .ForeignKey("DocumentId", _headerTable, "Id").Index("DocumentId").Create();
        }

        protected override StockAdjustment Map(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["Id"]), DocNo = row["DocNo"].ToString(), MovementDate = Convert.ToDateTime(row["MovementDate"]),
            WarehouseId = Convert.ToInt32(row["WarehouseId"]), Notes = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            CreatedAt = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
        };

        private static StockAdjustmentLine MapLine(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["Id"]), DocumentId = Convert.ToInt32(row["DocumentId"]), LineNo = Convert.ToInt32(row["LineNo"]),
            ProductId = Convert.ToInt32(row["ProductId"]), ProductCode = row["ProductCode"].ToString(),
            ProductName = row["ProductName"] == DBNull.Value ? null : row["ProductName"].ToString(),
            Qty = Convert.ToDecimal(row["Qty"]), UnitCost = Convert.ToDecimal(row["UnitCost"]),
            Notes = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
        };

        public override StockAdjustment GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne($"SELECT * FROM {_headerTable} WHERE Id = @id", conn, tx, ("@id", id));

        public List<StockAdjustmentLine> GetLines(int documentId, DbConnection conn = null, DbTransaction tx = null) =>
            QueryAs(MapLine, $"SELECT * FROM {_lineTable} WHERE DocumentId = @id ORDER BY LineNo", conn, tx, ("@id", documentId));

        public (List<StockAdjustment> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending)
        {
            var where = new WhereBuilder().LikeAny(searchText, "DocNo");
            var column = sortColumn == "DocNo" ? "DocNo" : "MovementDate";
            var direction = sortDescending ? "DESC" : "ASC";

            var total = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM {_headerTable} {where.Sql}", where.Parameters));
            var pageSql = $@"SELECT * FROM {_headerTable} {where.Sql} ORDER BY {column} {direction}, Id {direction}
                              {DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";
            return (Query(pageSql, null, null, where.Parameters), total);
        }

        public int InsertHeader(DbConnection conn, DbTransaction tx, StockAdjustment doc) =>
            InsertGetId($"INSERT INTO {_headerTable} (DocNo, MovementDate, WarehouseId, Notes, CreatedBy) VALUES (@no, @date, @wh, @notes, @by)",
                conn, tx, ("@no", doc.DocNo), ("@date", doc.MovementDate), ("@wh", doc.WarehouseId), ("@notes", doc.Notes ?? ""), ("@by", doc.CreatedBy));

        public void InsertLine(DbConnection conn, DbTransaction tx, int documentId, StockAdjustmentLine line) =>
            Exec($@"INSERT INTO {_lineTable} (DocumentId, LineNo, ProductId, ProductCode, ProductName, Qty, UnitCost, Notes)
                    VALUES (@did, @lno, @pid, @pcode, @pname, @qty, @cost, @notes)",
                conn, tx, ("@did", documentId), ("@lno", line.LineNo), ("@pid", line.ProductId), ("@pcode", line.ProductCode),
                ("@pname", line.ProductName ?? ""), ("@qty", line.Qty), ("@cost", line.UnitCost), ("@notes", line.Notes ?? ""));
    }

    public class StockInRepository : StockAdjustmentRepositoryBase, IStockInRepository
    {
        public StockInRepository() : base("StockInDocuments", "StockInLines") { }
    }

    public class StockOutRepository : StockAdjustmentRepositoryBase, IStockOutRepository
    {
        public StockOutRepository() : base("StockOutDocuments", "StockOutLines") { }
    }
}
