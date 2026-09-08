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
    public class StockTransferRepository : RepositoryBase<StockTransferDocument>, IStockTransferRepository
    {
        protected override string TableName => "StockTransferDocuments";

        public void CreateTable()
        {
            SchemaBuilder.Table("StockTransferDocuments")
                .Id().Text("DocNo", 30, required: true, unique: true).DateCol("MovementDate", nullable: false)
                .Int("FromWarehouseId", nullable: false).Int("ToWarehouseId", nullable: false).Text("Notes").Audit().Create();

            SchemaBuilder.Table("StockTransferLines")
                .Id().Int("DocumentId", nullable: false).Int("LineNo", nullable: false, defaultValue: 1)
                .Int("ProductId", nullable: false).Text("ProductCode", 30).Text("ProductName", 200).Decimal("Qty").Text("Notes")
                .ForeignKey("DocumentId", "StockTransferDocuments", "Id").Index("DocumentId").Create();
        }

        protected override StockTransferDocument Map(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["Id"]), DocNo = row["DocNo"].ToString(), MovementDate = Convert.ToDateTime(row["MovementDate"]),
            FromWarehouseId = Convert.ToInt32(row["FromWarehouseId"]), ToWarehouseId = Convert.ToInt32(row["ToWarehouseId"]),
            Notes = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            CreatedAt = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
        };

        private static StockTransferLine MapLine(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["Id"]), DocumentId = Convert.ToInt32(row["DocumentId"]), LineNo = Convert.ToInt32(row["LineNo"]),
            ProductId = Convert.ToInt32(row["ProductId"]), ProductCode = row["ProductCode"].ToString(),
            ProductName = row["ProductName"] == DBNull.Value ? null : row["ProductName"].ToString(),
            Qty = Convert.ToDecimal(row["Qty"]), Notes = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
        };

        public override StockTransferDocument GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM StockTransferDocuments WHERE Id = @id", conn, tx, ("@id", id));

        public List<StockTransferLine> GetLines(int documentId, DbConnection conn = null, DbTransaction tx = null) =>
            QueryAs(MapLine, "SELECT * FROM StockTransferLines WHERE DocumentId = @id ORDER BY LineNo", conn, tx, ("@id", documentId));

        public (List<StockTransferDocument> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending)
        {
            var where = new WhereBuilder().LikeAny(searchText, "DocNo");
            var column = sortColumn == "DocNo" ? "DocNo" : "MovementDate";
            var direction = sortDescending ? "DESC" : "ASC";

            var total = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM StockTransferDocuments {where.Sql}", where.Parameters));
            var pageSql = $@"SELECT * FROM StockTransferDocuments {where.Sql} {OrderBuilder.By(column, sortDescending, "DocNo", "CreatedAt")}
                              {DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";
            return (Query(pageSql, null, null, where.Parameters), total);
        }

        public int InsertHeader(DbConnection conn, DbTransaction tx, StockTransferDocument doc) =>
            InsertGetId("INSERT INTO StockTransferDocuments (DocNo, MovementDate, FromWarehouseId, ToWarehouseId, Notes, CreatedBy) VALUES (@no, @date, @from, @to, @notes, @by)",
                conn, tx, ("@no", doc.DocNo), ("@date", doc.MovementDate), ("@from", doc.FromWarehouseId), ("@to", doc.ToWarehouseId), ("@notes", doc.Notes ?? ""), ("@by", doc.CreatedBy));

        public void InsertLine(DbConnection conn, DbTransaction tx, int documentId, StockTransferLine line) =>
            Exec(@"INSERT INTO StockTransferLines (DocumentId, LineNo, ProductId, ProductCode, ProductName, Qty, Notes)
                  VALUES (@did, @lno, @pid, @pcode, @pname, @qty, @notes)",
                conn, tx, ("@did", documentId), ("@lno", line.LineNo), ("@pid", line.ProductId), ("@pcode", line.ProductCode),
                ("@pname", line.ProductName ?? ""), ("@qty", line.Qty), ("@notes", line.Notes ?? ""));
    }
}
