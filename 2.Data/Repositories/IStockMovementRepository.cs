using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IStockMovementRepository
    {
        void CreateTable();
        int Insert(StockMovement m, DbConnection conn = null, DbTransaction tx = null);

        /// <summary>يمحو أثر مستندٍ من المخزون — الحركة تحمل مصدرها، فحذفه استعلامٌ واحد.</summary>
        void DeleteBySource(DbConnection conn, DbTransaction tx, string sourceDocType, int sourceDocId);
        decimal GetBalance(int productId, int? warehouseId, DbConnection conn = null, DbTransaction tx = null);
        List<StockMovement> GetHistory(int productId, int? warehouseId, int maxResults);

        /// <summary>رصيد كل تركيبة صنف+مخزن ظهرت لها حركة على الإطلاق — استعلام GROUP BY واحد، تستخدمه StockBalancesReport.</summary>
        List<(int ProductId, int WarehouseId, decimal Balance)> GetAllBalances();

        /// <summary>كل الحركات بين تاريخين (بلا فلترة صنف) — تستخدمه StockReport.</summary>
        List<StockMovement> GetMovements(System.DateTime from, System.DateTime to, int? warehouseId, int maxResults);

        /// <summary>كل حركات الصنف بترتيب ورودها — مادّة المتوسط المرجَّح. بلا حدٍّ لعددها: حركةٌ محذوفة
        /// من الحساب تُفسده، والترتيب بالتاريخ ثم بالمعرّف قطعيٌّ ولو تطابق التاريخان.</summary>
        List<StockMovement> GetForCosting(int productId, DbConnection conn = null, DbTransaction tx = null);

        /// <summary>تكلفة وحدة الصنف في حركة مستندٍ بعينه — يعرف بها المرتجع تكلفة ما استُلم أو صُرف.</summary>
        decimal? GetSourceUnitCost(string sourceDocType, int sourceDocId, int productId,
            DbConnection conn = null, DbTransaction tx = null);
    }
}
