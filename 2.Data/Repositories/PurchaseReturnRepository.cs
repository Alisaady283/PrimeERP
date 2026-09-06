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
    // نفس بنية مستودع فاتورة الشراء (رأس+سطور، بلا معاملات تُفتَح هنا).
    public class PurchaseReturnRepository : RepositoryBase<PurchaseReturn>, IPurchaseReturnRepository
    {
        protected override string TableName => "PurchaseReturns";

        public void CreateTable()
        {
            SchemaBuilder.Table("PurchaseReturns")
                .Id()
                .Text("ReturnNo", 30, required: true, unique: true)
                .DateCol("ReturnDate", nullable: false)
                .Int("SupplierId", nullable: false)
                .Int("WarehouseId", nullable: false)
                .Decimal("SubTotal")
                .Decimal("DiscountAmount")
                .Decimal("VatAmount")
                .Decimal("WithholdingAmount")
                .Decimal("NetTotal")
                .Int("JournalEntryId")
                .Text("Notes")
                .Audit()
                .Index("SupplierId")
                .Create();

            SchemaBuilder.Table("PurchaseReturnLines")
                .Id()
                .Int("ReturnId", nullable: false)
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
                .ForeignKey("ReturnId", "PurchaseReturns", "Id")
                .Index("ReturnId")
                .Create();
        }

        protected override PurchaseReturn Map(DataRow row) => new()
        {
            Id             = Convert.ToInt32(row["Id"]),
            ReturnNo       = row["ReturnNo"].ToString(),
            ReturnDate     = Convert.ToDateTime(row["ReturnDate"]),
            SupplierId     = Convert.ToInt32(row["SupplierId"]),
            WarehouseId    = Convert.ToInt32(row["WarehouseId"]),
            SubTotal       = Convert.ToDecimal(row["SubTotal"]),
            DiscountAmount      = Convert.ToDecimal(row["DiscountAmount"]),
            VatAmount      = Convert.ToDecimal(row["VatAmount"]),
            WithholdingAmount      = Convert.ToDecimal(row["WithholdingAmount"]),
            NetTotal       = Convert.ToDecimal(row["NetTotal"]),
            JournalEntryId = row["JournalEntryId"] == DBNull.Value ? null : Convert.ToInt32(row["JournalEntryId"]),
            Notes          = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            CreatedAt      = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy      = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
        };

        private static PurchaseReturnLine MapLine(DataRow row) => new()
        {
            Id          = Convert.ToInt32(row["Id"]),
            ReturnId    = Convert.ToInt32(row["ReturnId"]),
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

        public override PurchaseReturn GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM PurchaseReturns WHERE Id = @id", conn, tx, ("@id", id));

        public List<PurchaseReturnLine> GetLines(int returnId, DbConnection conn = null, DbTransaction tx = null) =>
            QueryAs(MapLine, "SELECT * FROM PurchaseReturnLines WHERE ReturnId = @id ORDER BY LineNo", conn, tx, ("@id", returnId));

        public (List<PurchaseReturn> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? supplierId, string sortColumn, bool sortDescending)
        {
            var where = new WhereBuilder().LikeAny(searchText, "ReturnNo").Eq("SupplierId", supplierId);
            var column = sortColumn switch { "ReturnNo" => "ReturnNo", "NetTotal" => "NetTotal", _ => "ReturnDate" };
            var direction = sortDescending ? "DESC" : "ASC";

            var total = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM PurchaseReturns {where.Sql}", where.Parameters));
            var pageSql = $@"SELECT * FROM PurchaseReturns {where.Sql} ORDER BY {column} {direction}, Id {direction}
                              {DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";

            return (Query(pageSql, null, null, where.Parameters), total);
        }

        public int InsertHeader(DbConnection conn, DbTransaction tx, PurchaseReturn ret) =>
            InsertGetId(@"INSERT INTO PurchaseReturns (ReturnNo, ReturnDate, SupplierId, WarehouseId, SubTotal, DiscountAmount, VatAmount, WithholdingAmount, NetTotal, Notes, CreatedBy)
                          VALUES (@no, @date, @supp, @wh, @sub, @disc, @tax, @wht, @net, @notes, @by)",
                conn, tx,
                ("@no", ret.ReturnNo), ("@date", ret.ReturnDate), ("@supp", ret.SupplierId), ("@wh", ret.WarehouseId),
                ("@sub", ret.SubTotal), ("@disc", ret.DiscountAmount), ("@tax", ret.VatAmount), ("@wht", ret.WithholdingAmount), ("@net", ret.NetTotal), ("@notes", ret.Notes ?? ""), ("@by", ret.CreatedBy));

        public void InsertLine(DbConnection conn, DbTransaction tx, int returnId, PurchaseReturnLine line) =>
            Exec(@"INSERT INTO PurchaseReturnLines (ReturnId, LineNo, ProductId, ProductCode, ProductName, Qty, UnitPrice, DiscountPercent, DiscountAmount, VatPercent, VatAmount, WithholdingPercent, WithholdingAmount, LineTotal, NetAmount, Notes)
                  VALUES (@rid, @lno, @pid, @pcode, @pname, @qty, @price, @discPct, @disc, @taxPct, @tax, @whtPct, @wht, @total, @net, @notes)",
                conn, tx,
                ("@rid", returnId), ("@lno", line.LineNo), ("@pid", line.ProductId), ("@pcode", line.ProductCode), ("@pname", line.ProductName ?? ""),
                ("@qty", line.Qty), ("@price", line.UnitPrice), ("@discPct", line.DiscountPercent), ("@disc", line.DiscountAmount),
                ("@taxPct", line.VatPercent), ("@tax", line.VatAmount), ("@whtPct", line.WithholdingPercent), ("@wht", line.WithholdingAmount),
                ("@total", line.LineTotal), ("@net", line.NetAmount),
                ("@notes", line.Notes ?? ""));

        public void SetJournalEntryId(DbConnection conn, DbTransaction tx, int returnId, int journalEntryId) =>
            Exec("UPDATE PurchaseReturns SET JournalEntryId = @jid WHERE Id = @id", conn, tx, ("@jid", journalEntryId), ("@id", returnId));
    }
}
