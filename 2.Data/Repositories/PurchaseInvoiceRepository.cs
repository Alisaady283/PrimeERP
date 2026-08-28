using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Core;
using PrimeERP.Data.Query;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    // نسخة طبق الأصل من SalesInvoiceRepository — رأس+سطور، بلا معاملات تُفتَح هنا، غير قابلة للتعديل/الحذف
    // بعد الإنشاء.
    public class PurchaseInvoiceRepository : RepositoryBase<PurchaseInvoice>, IPurchaseInvoiceRepository
    {
        protected override string TableName => "PurchaseInvoices";

        public void CreateTable()
        {
            SchemaBuilder.Table("PurchaseInvoices")
                .Id()
                .Text("InvoiceNo", 30, required: true, unique: true)
                .DateCol("InvoiceDate", nullable: false)
                .Int("SupplierId", nullable: false)
                .Int("WarehouseId", nullable: false)
                .Decimal("SubTotal")
                .Decimal("TaxAmount")
                .Decimal("NetTotal")
                .Int("Status", nullable: false, defaultValue: (int)InvoiceStatus.Confirmed)
                .Int("JournalEntryId")
                .Text("Notes")
                .Audit()
                .Index("SupplierId")
                .Create();

            SchemaBuilder.Table("PurchaseInvoiceLines")
                .Id()
                .Int("InvoiceId", nullable: false)
                .Int("LineNo", nullable: false, defaultValue: 1)
                .Int("ProductId", nullable: false)
                .Text("ProductCode", 30)
                .Text("ProductName", 200)
                .Decimal("Qty")
                .Decimal("UnitPrice")
                .Decimal("TaxPercent")
                .Decimal("TaxAmount")
                .Decimal("LineTotal")
                .Text("Notes")
                .ForeignKey("InvoiceId", "PurchaseInvoices", "Id")
                .Index("InvoiceId")
                .Create();
        }

        protected override PurchaseInvoice Map(DataRow row) => new()
        {
            Id             = Convert.ToInt32(row["Id"]),
            InvoiceNo      = row["InvoiceNo"].ToString(),
            InvoiceDate    = Convert.ToDateTime(row["InvoiceDate"]),
            SupplierId     = Convert.ToInt32(row["SupplierId"]),
            WarehouseId    = Convert.ToInt32(row["WarehouseId"]),
            SubTotal       = Convert.ToDecimal(row["SubTotal"]),
            TaxAmount      = Convert.ToDecimal(row["TaxAmount"]),
            NetTotal       = Convert.ToDecimal(row["NetTotal"]),
            Status         = (InvoiceStatus)Convert.ToInt32(row["Status"]),
            JournalEntryId = row["JournalEntryId"] == DBNull.Value ? null : Convert.ToInt32(row["JournalEntryId"]),
            Notes          = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            CreatedAt      = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy      = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
        };

        private static PurchaseInvoiceLine MapLine(DataRow row) => new()
        {
            Id          = Convert.ToInt32(row["Id"]),
            InvoiceId   = Convert.ToInt32(row["InvoiceId"]),
            LineNo      = Convert.ToInt32(row["LineNo"]),
            ProductId   = Convert.ToInt32(row["ProductId"]),
            ProductCode = row["ProductCode"].ToString(),
            ProductName = row["ProductName"] == DBNull.Value ? null : row["ProductName"].ToString(),
            Qty         = Convert.ToDecimal(row["Qty"]),
            UnitPrice   = Convert.ToDecimal(row["UnitPrice"]),
            TaxPercent  = Convert.ToDecimal(row["TaxPercent"]),
            TaxAmount   = Convert.ToDecimal(row["TaxAmount"]),
            LineTotal   = Convert.ToDecimal(row["LineTotal"]),
            Notes       = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
        };

        public override PurchaseInvoice GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM PurchaseInvoices WHERE Id = @id", conn, tx, ("@id", id));

        public List<PurchaseInvoiceLine> GetLines(int invoiceId, DbConnection conn = null, DbTransaction tx = null) =>
            QueryAs(MapLine, "SELECT * FROM PurchaseInvoiceLines WHERE InvoiceId = @id ORDER BY LineNo", conn, tx, ("@id", invoiceId));

        public (List<PurchaseInvoice> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? supplierId, string sortColumn, bool sortDescending)
        {
            var where = new WhereBuilder()
                .LikeAny(searchText, "InvoiceNo")
                .Eq("SupplierId", supplierId);

            var column = sortColumn switch { "InvoiceNo" => "InvoiceNo", "NetTotal" => "NetTotal", _ => "InvoiceDate" };
            var direction = sortDescending ? "DESC" : "ASC";

            var total = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM PurchaseInvoices {where.Sql}", where.Parameters));
            var pageSql = $@"SELECT * FROM PurchaseInvoices {where.Sql} ORDER BY {column} {direction}, Id {direction}
                              {DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";

            return (Query(pageSql, null, null, where.Parameters), total);
        }

        public int InsertHeader(DbConnection conn, DbTransaction tx, PurchaseInvoice invoice) =>
            InsertGetId(@"INSERT INTO PurchaseInvoices (InvoiceNo, InvoiceDate, SupplierId, WarehouseId, SubTotal, TaxAmount, NetTotal, Status, Notes, CreatedBy)
                          VALUES (@no, @date, @supp, @wh, @sub, @tax, @net, @status, @notes, @by)",
                conn, tx,
                ("@no", invoice.InvoiceNo), ("@date", invoice.InvoiceDate), ("@supp", invoice.SupplierId), ("@wh", invoice.WarehouseId),
                ("@sub", invoice.SubTotal), ("@tax", invoice.TaxAmount), ("@net", invoice.NetTotal), ("@status", (int)invoice.Status),
                ("@notes", invoice.Notes ?? ""), ("@by", invoice.CreatedBy));

        public void InsertLine(DbConnection conn, DbTransaction tx, int invoiceId, PurchaseInvoiceLine line) =>
            Exec(@"INSERT INTO PurchaseInvoiceLines (InvoiceId, LineNo, ProductId, ProductCode, ProductName, Qty, UnitPrice, TaxPercent, TaxAmount, LineTotal, Notes)
                  VALUES (@iid, @lno, @pid, @pcode, @pname, @qty, @price, @taxPct, @tax, @total, @notes)",
                conn, tx,
                ("@iid", invoiceId), ("@lno", line.LineNo), ("@pid", line.ProductId), ("@pcode", line.ProductCode), ("@pname", line.ProductName ?? ""),
                ("@qty", line.Qty), ("@price", line.UnitPrice), ("@taxPct", line.TaxPercent), ("@tax", line.TaxAmount), ("@total", line.LineTotal),
                ("@notes", line.Notes ?? ""));

        public void SetJournalEntryId(DbConnection conn, DbTransaction tx, int invoiceId, int journalEntryId) =>
            Exec("UPDATE PurchaseInvoices SET JournalEntryId = @jid WHERE Id = @id", conn, tx, ("@jid", journalEntryId), ("@id", invoiceId));
    }
}
