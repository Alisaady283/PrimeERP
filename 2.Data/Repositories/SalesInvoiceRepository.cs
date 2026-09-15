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
    // بنفس بنية JournalRepository — رأس+سطور، بلا معاملات تُفتَح هنا (SalesInvoiceService تفتحها وتُمرِّر
    // (conn,tx))، بلا حساب إجماليات (الخدمة تحسبها قبل InsertHeader). الفاتورة غير قابلة للتعديل/الحذف بعد
    // الإنشاء (تُرحَّل فوراً) — UPDATE/DELETE غير مُعرَّضين هنا عمداً.
    public class SalesInvoiceRepository : RepositoryBase<SalesInvoice>, ISalesInvoiceRepository
    {
        protected override string TableName => "SalesInvoices";

        public void CreateTable()
        {
            SchemaBuilder.Table("SalesInvoices")
                .Id()
                .Text("InvoiceNo", 30, required: true, unique: true)
                .DateCol("InvoiceDate", nullable: false)
                .Int("CustomerId", nullable: false)
                .Int("WarehouseId", nullable: false)
                .Decimal("SubTotal")
                .Decimal("DiscountAmount")
                .Decimal("VatAmount")
                .Decimal("WithholdingAmount")
                .Decimal("NetTotal")
                .Int("Status", nullable: false, defaultValue: (int)InvoiceStatus.Confirmed)
                .Int("JournalEntryId")
                .Text("Notes")
                .Audit()
                .Index("CustomerId")
                .Create();

            SchemaBuilder.Table("SalesInvoiceLines")
                .Id()
                .Int("InvoiceId", nullable: false)
                .Int("LineNo", nullable: false, defaultValue: 1)
                .Int("ProductId", nullable: false)
                .Text("ProductCode", 30)
                .Text("ProductName", 200)
                .Decimal("Qty")
                .Decimal("UnitPrice")
                .Decimal("DiscountPercent")
                .Decimal("DiscountAmount")
                .Decimal("VatPercent")
                .Decimal("VatAmount")
                .Decimal("WithholdingPercent")
                .Decimal("WithholdingAmount")
                .Decimal("LineTotal")
                .Decimal("NetAmount")
                .Text("Notes")
                .ForeignKey("InvoiceId", "SalesInvoices", "Id")
                .Index("InvoiceId")
                .Create();
        }

        protected override SalesInvoice Map(DataRow row) => new()
        {
            Id           = Convert.ToInt32(row["Id"]),
            InvoiceNo    = row["InvoiceNo"].ToString(),
            InvoiceDate  = Convert.ToDateTime(row["InvoiceDate"]),
            CustomerId   = Convert.ToInt32(row["CustomerId"]),
            WarehouseId  = Convert.ToInt32(row["WarehouseId"]),
            SubTotal     = Convert.ToDecimal(row["SubTotal"]),
            DiscountAmount    = Convert.ToDecimal(row["DiscountAmount"]),
            VatAmount    = Convert.ToDecimal(row["VatAmount"]),
            WithholdingAmount    = Convert.ToDecimal(row["WithholdingAmount"]),
            NetTotal     = Convert.ToDecimal(row["NetTotal"]),
            Status       = (InvoiceStatus)Convert.ToInt32(row["Status"]),
            JournalEntryId = row["JournalEntryId"] == DBNull.Value ? null : Convert.ToInt32(row["JournalEntryId"]),
            Notes        = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            CreatedAt    = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy    = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
        };

        private static SalesInvoiceLine MapLine(DataRow row) => new()
        {
            Id          = Convert.ToInt32(row["Id"]),
            InvoiceId   = Convert.ToInt32(row["InvoiceId"]),
            LineNo      = Convert.ToInt32(row["LineNo"]),
            ProductId   = Convert.ToInt32(row["ProductId"]),
            ProductCode = row["ProductCode"].ToString(),
            ProductName = row["ProductName"] == DBNull.Value ? null : row["ProductName"].ToString(),
            Qty         = Convert.ToDecimal(row["Qty"]),
            UnitPrice   = Convert.ToDecimal(row["UnitPrice"]),
            DiscountPercent  = Convert.ToDecimal(row["DiscountPercent"]),
            DiscountAmount  = Convert.ToDecimal(row["DiscountAmount"]),
            VatPercent  = Convert.ToDecimal(row["VatPercent"]),
            VatAmount  = Convert.ToDecimal(row["VatAmount"]),
            WithholdingPercent  = Convert.ToDecimal(row["WithholdingPercent"]),
            WithholdingAmount  = Convert.ToDecimal(row["WithholdingAmount"]),
            LineTotal  = Convert.ToDecimal(row["LineTotal"]),
            NetAmount  = Convert.ToDecimal(row["NetAmount"]),
            Notes       = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
        };

        // الحذف من RepositoryBase — هنا إعلان جدول السطور ومفتاحه فقط.
        protected override string LineTable => "SalesInvoiceLines";
        protected override string LineForeignKey => "InvoiceId";

        public void DeleteDocument(DbConnection conn, DbTransaction tx, int id) => HardDelete(id, conn, tx);

        public override SalesInvoice GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM SalesInvoices WHERE Id = @id", conn, tx, ("@id", id));

        public List<SalesInvoiceLine> GetLines(int invoiceId, DbConnection conn = null, DbTransaction tx = null) =>
            QueryAs(MapLine, "SELECT * FROM SalesInvoiceLines WHERE InvoiceId = @id ORDER BY LineNo", conn, tx, ("@id", invoiceId));

        public (List<SalesInvoice> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? customerId, string sortColumn, bool sortDescending)
        {
            var where = new WhereBuilder()
                .LikeAny(searchText, "InvoiceNo")
                .Eq("CustomerId", customerId);

            var column = sortColumn switch { "InvoiceNo" => "InvoiceNo", "NetTotal" => "NetTotal", _ => "InvoiceDate" };
            return Page(where, page, pageSize, OrderBuilder.By(column, sortDescending, "InvoiceNo", "CreatedAt"));
        }

        public int InsertHeader(DbConnection conn, DbTransaction tx, SalesInvoice invoice) =>
            InsertGetId(@"INSERT INTO SalesInvoices (InvoiceNo, InvoiceDate, CustomerId, WarehouseId, SubTotal, DiscountAmount, VatAmount, WithholdingAmount, NetTotal, Status, Notes, CreatedBy)
                          VALUES (@no, @date, @cust, @wh, @sub, @disc, @tax, @wht, @net, @status, @notes, @by)",
                conn, tx,
                ("@no", invoice.InvoiceNo), ("@date", invoice.InvoiceDate), ("@cust", invoice.CustomerId), ("@wh", invoice.WarehouseId),
                ("@sub", invoice.SubTotal), ("@disc", invoice.DiscountAmount), ("@tax", invoice.VatAmount), ("@wht", invoice.WithholdingAmount), ("@net", invoice.NetTotal), ("@status", (int)invoice.Status),
                ("@notes", invoice.Notes ?? ""), ("@by", invoice.CreatedBy));

        public int InsertLine(DbConnection conn, DbTransaction tx, int invoiceId, SalesInvoiceLine line) =>
            InsertGetId(@"INSERT INTO SalesInvoiceLines (InvoiceId, LineNo, ProductId, ProductCode, ProductName, Qty, UnitPrice, DiscountPercent, DiscountAmount, VatPercent, VatAmount, WithholdingPercent, WithholdingAmount, LineTotal, NetAmount, Notes)
                  VALUES (@iid, @lno, @pid, @pcode, @pname, @qty, @price, @discPct, @disc, @taxPct, @tax, @whtPct, @wht, @total, @net, @notes)",
                conn, tx,
                ("@iid", invoiceId), ("@lno", line.LineNo), ("@pid", line.ProductId), ("@pcode", line.ProductCode), ("@pname", line.ProductName ?? ""),
                ("@qty", line.Qty), ("@price", line.UnitPrice), ("@discPct", line.DiscountPercent), ("@disc", line.DiscountAmount),
                ("@taxPct", line.VatPercent), ("@tax", line.VatAmount), ("@whtPct", line.WithholdingPercent), ("@wht", line.WithholdingAmount),
                ("@total", line.LineTotal), ("@net", line.NetAmount),
                ("@notes", line.Notes ?? ""));

        public void SetJournalEntryId(DbConnection conn, DbTransaction tx, int invoiceId, int journalEntryId) =>
            Exec("UPDATE SalesInvoices SET JournalEntryId = @jid WHERE Id = @id", conn, tx, ("@jid", journalEntryId), ("@id", invoiceId));
    }
}
