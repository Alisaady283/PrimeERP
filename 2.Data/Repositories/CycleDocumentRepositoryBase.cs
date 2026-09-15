using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Core;
using PrimeERP.Data.Query;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Data.Repositories
{
    public interface ICycleDocumentRepository
    {
        void CreateTable();
        CycleDocument GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<CycleDocumentLine> GetLines(int documentId, DbConnection conn = null, DbTransaction tx = null);
        (List<CycleDocument> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending);
        int InsertHeader(DbConnection conn, DbTransaction tx, CycleDocument doc);
        int InsertLine(DbConnection conn, DbTransaction tx, int documentId, CycleDocumentLine line);
        void DeleteDocument(int id);
        void DeleteDocument(DbConnection conn, DbTransaction tx, int id);
    }

    public interface IPurchaseRequestRepository : ICycleDocumentRepository { }
    public interface IPurchaseOrderRepository : ICycleDocumentRepository { }
    public interface IQuotationRepository : ICycleDocumentRepository { }
    public interface ISalesOrderRepository : ICycleDocumentRepository { }

    // نفس شكل StockAdjustmentRepositoryBase: الفرق اسما الجدولين فقط، وطرف بدل مخزن وسعر بدل تكلفة.
    public abstract class CycleDocumentRepositoryBase : RepositoryBase<CycleDocument>, ICycleDocumentRepository
    {
        private readonly string _headerTable, _lineTable;
        protected CycleDocumentRepositoryBase(string headerTable, string lineTable) { _headerTable = headerTable; _lineTable = lineTable; }
        protected override string TableName => _headerTable;

        public void CreateTable()
        {
            SchemaBuilder.Table(_headerTable)
                .Id().Text("DocNo", 30, required: true, unique: true).DateCol("DocDate", nullable: false)
                .Int("PartyId").Text("Notes").Audit().Create();

            SchemaBuilder.Table(_lineTable)
                .Id().Int("DocumentId", nullable: false).Int("LineNo", nullable: false, defaultValue: 1)
                .Int("ProductId", nullable: false).Text("ProductCode", 30).Text("ProductName", 200)
                .Decimal("Qty").Decimal("UnitPrice").Text("Notes")
                .ForeignKey("DocumentId", _headerTable, "Id").Index("DocumentId").Create();
        }

        protected override CycleDocument Map(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["Id"]), DocNo = row["DocNo"].ToString(), DocDate = Convert.ToDateTime(row["DocDate"]),
            PartyId = row["PartyId"] == DBNull.Value ? null : Convert.ToInt32(row["PartyId"]),
            Notes = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            CreatedAt = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
        };

        private static CycleDocumentLine MapLine(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["Id"]), DocumentId = Convert.ToInt32(row["DocumentId"]), LineNo = Convert.ToInt32(row["LineNo"]),
            ProductId = Convert.ToInt32(row["ProductId"]), ProductCode = row["ProductCode"].ToString(),
            ProductName = row["ProductName"] == DBNull.Value ? null : row["ProductName"].ToString(),
            Qty = Convert.ToDecimal(row["Qty"]), UnitPrice = Convert.ToDecimal(row["UnitPrice"]),
            Notes = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
        };

        public override CycleDocument GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne($"SELECT * FROM {_headerTable} WHERE Id = @id", conn, tx, ("@id", id));

        public List<CycleDocumentLine> GetLines(int documentId, DbConnection conn = null, DbTransaction tx = null) =>
            QueryAs(MapLine, $"SELECT * FROM {_lineTable} WHERE DocumentId = @id ORDER BY LineNo", conn, tx, ("@id", documentId));

        public (List<CycleDocument> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending)
        {
            var where = new WhereBuilder().LikeAny(searchText, "DocNo");
            var column = sortColumn == "DocNo" ? "DocNo" : "DocDate";
            return Page(where, page, pageSize, OrderBuilder.By(column, sortDescending, "DocNo", "CreatedAt"));
        }

        public int InsertHeader(DbConnection conn, DbTransaction tx, CycleDocument doc) =>
            InsertGetId($"INSERT INTO {_headerTable} (DocNo, DocDate, PartyId, Notes, CreatedBy) VALUES (@no, @date, @party, @notes, @by)",
                conn, tx, ("@no", doc.DocNo), ("@date", doc.DocDate), ("@party", (object)doc.PartyId ?? DBNull.Value),
                ("@notes", doc.Notes ?? ""), ("@by", doc.CreatedBy));

        public int InsertLine(DbConnection conn, DbTransaction tx, int documentId, CycleDocumentLine line) =>
            InsertGetId($@"INSERT INTO {_lineTable} (DocumentId, LineNo, ProductId, ProductCode, ProductName, Qty, UnitPrice, Notes)
                    VALUES (@did, @lno, @pid, @pcode, @pname, @qty, @price, @notes)",
                conn, tx, ("@did", documentId), ("@lno", line.LineNo), ("@pid", line.ProductId), ("@pcode", line.ProductCode),
                ("@pname", line.ProductName ?? ""), ("@qty", line.Qty), ("@price", line.UnitPrice), ("@notes", line.Notes ?? ""));

        public void DeleteDocument(int id) => Db.RunTransaction((conn, tx) => DeleteDocument(conn, tx, id));

        protected override string LineTable => _lineTable;

        public void DeleteDocument(DbConnection conn, DbTransaction tx, int id) => HardDelete(id, conn, tx);
    }

    public class PurchaseRequestRepository : CycleDocumentRepositoryBase, IPurchaseRequestRepository
    {
        public PurchaseRequestRepository() : base("PurchaseRequestDocuments", "PurchaseRequestLines") { }
    }

    public class PurchaseOrderRepository : CycleDocumentRepositoryBase, IPurchaseOrderRepository
    {
        public PurchaseOrderRepository() : base("PurchaseOrderDocuments", "PurchaseOrderLines") { }
    }

    public class QuotationRepository : CycleDocumentRepositoryBase, IQuotationRepository
    {
        public QuotationRepository() : base("QuotationDocuments", "QuotationLines") { }
    }

    public class SalesOrderRepository : CycleDocumentRepositoryBase, ISalesOrderRepository
    {
        public SalesOrderRepository() : base("SalesOrderDocuments", "SalesOrderLines") { }
    }
}
