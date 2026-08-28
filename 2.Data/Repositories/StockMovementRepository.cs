using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    // سجل حركة مخزون — ذاكرة فقط لا تُعدَّل ولا تُحذَف (بلا SoftDelete/Concurrency، تصحيح الأخطاء عبر حركة
    // عكسية جديدة لا تعديل حركة قائمة، نفس مبدأ قيود اليومية المرحَّلة). الرصيد الجاري (BalanceAfter) يُحسَب
    // في StockService قبل الإدراج، لا هنا.
    public class StockMovementRepository : RepositoryBase<StockMovement>, IStockMovementRepository
    {
        protected override string TableName => "StockMovements";

        public void CreateTable() =>
            SchemaBuilder.Table("StockMovements")
                .Id()
                .Text("MovementNo", 30, required: true)
                .DateCol("MovementDate", nullable: false)
                .Int("ProductId", nullable: false)
                .Int("WarehouseId", nullable: false)
                .Int("MovementType", nullable: false)
                .Decimal("Qty")
                .Decimal("UnitCost")
                .Decimal("TotalCost")
                .Decimal("BalanceAfter")
                .Text("SourceDocType", 30)
                .Int("SourceDocId")
                .Text("SourceDocNo", 30)
                .Text("Notes")
                .Audit()
                .Index("ProductId")
                .Index("WarehouseId")
                .Create();

        private const string InsertSql = @"
            INSERT INTO StockMovements
                (MovementNo, MovementDate, ProductId, WarehouseId, MovementType, Qty, UnitCost, TotalCost, BalanceAfter,
                 SourceDocType, SourceDocId, SourceDocNo, Notes, CreatedBy)
            VALUES
                (@no, @date, @productId, @warehouseId, @type, @qty, @unitCost, @totalCost, @balanceAfter,
                 @sourceType, @sourceId, @sourceNo, @notes, @createdBy)";

        public int Insert(StockMovement m, DbConnection conn = null, DbTransaction tx = null) =>
            InsertGetId(InsertSql, conn, tx,
                ("@no", m.MovementNo), ("@date", m.MovementDate), ("@productId", m.ProductId), ("@warehouseId", m.WarehouseId),
                ("@type", (int)m.MovementType), ("@qty", m.Qty), ("@unitCost", m.UnitCost), ("@totalCost", m.TotalCost),
                ("@balanceAfter", m.BalanceAfter), ("@sourceType", m.SourceDocType), ("@sourceId", m.SourceDocId),
                ("@sourceNo", m.SourceDocNo), ("@notes", m.Notes ?? ""), ("@createdBy", m.CreatedBy));

        // In(+)/Adjustment(±Qty نفسها)/Out(-) — Transfer لا تُخزَّن كنوع صريح، بل حركتا In/Out منفصلتان (راجع StockService.Transfer).
        public decimal GetBalance(int productId, int? warehouseId, DbConnection conn = null, DbTransaction tx = null)
        {
            var sql = $@"SELECT COALESCE(SUM(CASE MovementType WHEN {(int)MovementType.Out} THEN -Qty ELSE Qty END), 0) AS Balance
                         FROM StockMovements WHERE ProductId = @p {(warehouseId != null ? "AND WarehouseId = @w" : "")}";

            return QueryOneAs(row => Convert.ToDecimal(row["Balance"]), sql, conn, tx,
                warehouseId != null ? new (string, object)[] { ("@p", productId), ("@w", warehouseId.Value) } : new (string, object)[] { ("@p", productId) });
        }

        public List<(int ProductId, int WarehouseId, decimal Balance)> GetAllBalances() =>
            QueryAs(row => (
                Convert.ToInt32(row["ProductId"]), Convert.ToInt32(row["WarehouseId"]), Convert.ToDecimal(row["Balance"])),
                $@"SELECT ProductId, WarehouseId,
                          COALESCE(SUM(CASE MovementType WHEN {(int)MovementType.Out} THEN -Qty ELSE Qty END), 0) AS Balance
                   FROM StockMovements GROUP BY ProductId, WarehouseId HAVING Balance != 0");

        public List<StockMovement> GetMovements(DateTime from, DateTime to, int? warehouseId, int maxResults) =>
            Query($@"SELECT * FROM StockMovements WHERE MovementDate >= @f AND MovementDate <= @t {(warehouseId != null ? "AND WarehouseId = @w" : "")}
                     ORDER BY MovementDate DESC, Id DESC {DbFactory.Current.LimitClause(0, maxResults)}",
                null, null, warehouseId != null
                    ? new (string, object)[] { ("@f", from), ("@t", to), ("@w", warehouseId.Value) }
                    : new (string, object)[] { ("@f", from), ("@t", to) });

        public List<StockMovement> GetHistory(int productId, int? warehouseId, int maxResults) =>
            Query($@"SELECT * FROM StockMovements WHERE ProductId = @p {(warehouseId != null ? "AND WarehouseId = @w" : "")}
                     ORDER BY Id DESC LIMIT {maxResults}",
                null, null, warehouseId != null ? new (string, object)[] { ("@p", productId), ("@w", warehouseId.Value) } : new (string, object)[] { ("@p", productId) });

        protected override StockMovement Map(DataRow row) => new()
        {
            Id            = Convert.ToInt32(row["Id"]),
            MovementNo    = row["MovementNo"].ToString(),
            MovementDate  = Convert.ToDateTime(row["MovementDate"]),
            ProductId     = Convert.ToInt32(row["ProductId"]),
            WarehouseId   = Convert.ToInt32(row["WarehouseId"]),
            MovementType  = (MovementType)Convert.ToInt32(row["MovementType"]),
            Qty           = Convert.ToDecimal(row["Qty"]),
            UnitCost      = Convert.ToDecimal(row["UnitCost"]),
            TotalCost     = Convert.ToDecimal(row["TotalCost"]),
            BalanceAfter  = Convert.ToDecimal(row["BalanceAfter"]),
            SourceDocType = row["SourceDocType"] == DBNull.Value ? null : row["SourceDocType"].ToString(),
            SourceDocId   = row["SourceDocId"] == DBNull.Value ? null : Convert.ToInt32(row["SourceDocId"]),
            SourceDocNo   = row["SourceDocNo"] == DBNull.Value ? null : row["SourceDocNo"].ToString(),
            Notes         = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            CreatedAt     = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy     = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
        };
    }
}
