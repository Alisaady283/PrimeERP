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
    // نفس بنية مستودع فاتورة البيع (رأس+سطور، بلا معاملات تُفتَح هنا).
    public class SalesReturnRepository : RepositoryBase<SalesReturn>, ISalesReturnRepository
    {
        protected override string TableName => "SalesReturns";

        public void CreateTable()
        {
            SchemaBuilder.Table("SalesReturns")
                .Id()
                .Text("ReturnNo", 30, required: true, unique: true)
                .DateCol("ReturnDate", nullable: false)
                .Int("CustomerId", nullable: false)
                .Int("WarehouseId", nullable: false)
                .Decimal("SubTotal")
                .Decimal("TaxAmount")
                .Decimal("NetTotal")
                .Int("JournalEntryId")
                .Text("Notes")
                .Audit()
                .Index("CustomerId")
                .Create();

            SchemaBuilder.Table("SalesReturnLines")
                .Id()
                .Int("ReturnId", nullable: false)
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
                .ForeignKey("ReturnId", "SalesReturns", "Id")
                .Index("ReturnId")
                .Create();
        }

        protected override SalesReturn Map(DataRow row) => new()
        {
            Id             = Convert.ToInt32(row["Id"]),
            ReturnNo       = row["ReturnNo"].ToString(),
            ReturnDate     = Convert.ToDateTime(row["ReturnDate"]),
            CustomerId     = Convert.ToInt32(row["CustomerId"]),
            WarehouseId    = Convert.ToInt32(row["WarehouseId"]),
            SubTotal       = Convert.ToDecimal(row["SubTotal"]),
            TaxAmount      = Convert.ToDecimal(row["TaxAmount"]),
            NetTotal       = Convert.ToDecimal(row["NetTotal"]),
            JournalEntryId = row["JournalEntryId"] == DBNull.Value ? null : Convert.ToInt32(row["JournalEntryId"]),
            Notes          = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            CreatedAt      = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy      = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
        };

        private static SalesReturnLine MapLine(DataRow row) => new()
        {
            Id          = Convert.ToInt32(row["Id"]),
            ReturnId    = Convert.ToInt32(row["ReturnId"]),
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

        public override SalesReturn GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM SalesReturns WHERE Id = @id", conn, tx, ("@id", id));

        public List<SalesReturnLine> GetLines(int returnId, DbConnection conn = null, DbTransaction tx = null) =>
            QueryAs(MapLine, "SELECT * FROM SalesReturnLines WHERE ReturnId = @id ORDER BY LineNo", conn, tx, ("@id", returnId));

        public (List<SalesReturn> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? customerId, string sortColumn, bool sortDescending)
        {
            var where = new WhereBuilder().LikeAny(searchText, "ReturnNo").Eq("CustomerId", customerId);
            var column = sortColumn switch { "ReturnNo" => "ReturnNo", "NetTotal" => "NetTotal", _ => "ReturnDate" };
            var direction = sortDescending ? "DESC" : "ASC";

            var total = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM SalesReturns {where.Sql}", where.Parameters));
            var pageSql = $@"SELECT * FROM SalesReturns {where.Sql} ORDER BY {column} {direction}, Id {direction}
                              {DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";

            return (Query(pageSql, null, null, where.Parameters), total);
        }

        public int InsertHeader(DbConnection conn, DbTransaction tx, SalesReturn ret) =>
            InsertGetId(@"INSERT INTO SalesReturns (ReturnNo, ReturnDate, CustomerId, WarehouseId, SubTotal, TaxAmount, NetTotal, Notes, CreatedBy)
                          VALUES (@no, @date, @cust, @wh, @sub, @tax, @net, @notes, @by)",
                conn, tx,
                ("@no", ret.ReturnNo), ("@date", ret.ReturnDate), ("@cust", ret.CustomerId), ("@wh", ret.WarehouseId),
                ("@sub", ret.SubTotal), ("@tax", ret.TaxAmount), ("@net", ret.NetTotal), ("@notes", ret.Notes ?? ""), ("@by", ret.CreatedBy));

        public void InsertLine(DbConnection conn, DbTransaction tx, int returnId, SalesReturnLine line) =>
            Exec(@"INSERT INTO SalesReturnLines (ReturnId, LineNo, ProductId, ProductCode, ProductName, Qty, UnitPrice, TaxPercent, TaxAmount, LineTotal, Notes)
                  VALUES (@rid, @lno, @pid, @pcode, @pname, @qty, @price, @taxPct, @tax, @total, @notes)",
                conn, tx,
                ("@rid", returnId), ("@lno", line.LineNo), ("@pid", line.ProductId), ("@pcode", line.ProductCode), ("@pname", line.ProductName ?? ""),
                ("@qty", line.Qty), ("@price", line.UnitPrice), ("@taxPct", line.TaxPercent), ("@tax", line.TaxAmount), ("@total", line.LineTotal),
                ("@notes", line.Notes ?? ""));

        public void SetJournalEntryId(DbConnection conn, DbTransaction tx, int returnId, int journalEntryId) =>
            Exec("UPDATE SalesReturns SET JournalEntryId = @jid WHERE Id = @id", conn, tx, ("@jid", journalEntryId), ("@id", returnId));
    }
}
