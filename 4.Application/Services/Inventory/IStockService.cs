using System;
using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Inventory
{
    public interface IStockService
    {
        Result<decimal> GetBalance(int productId, int? warehouseId = null);

        /// <summary>رصيد كل تركيبة صنف+مخزن غير صفرية — تخدم StockBalancesReport (لا Repository مباشرة من 8.Modules).</summary>
        Result<List<(int ProductId, int WarehouseId, decimal Balance)>> GetAllBalances();

        /// <summary>سجل حركة صنف واحد — تخدم ItemCardReport.</summary>
        Result<List<Domain.Entities.StockMovement>> GetHistory(int productId, int? warehouseId, int maxResults = 200);

        /// <summary>كل الحركات بين تاريخين بلا فلترة صنف — تخدم StockReport.</summary>
        Result<List<Domain.Entities.StockMovement>> GetMovements(DateTime from, DateTime to, int? warehouseId = null, int maxResults = 500);

        // qty دائماً موجبة لـIn/Out، بأي إشارة لـAdjustment — Transfer ليست نوعاً هنا (راجع Transfer أدناه).
        Result RecordMovement(DbConnection conn, DbTransaction tx, int productId, int warehouseId, Domain.Enums.MovementType type,
            decimal qty, decimal unitCost, string sourceDocType, int? sourceDocId, string sourceDocNo, DateTime? date = null, string notes = null);

        /// <summary>يمحو أثر مستندٍ من المخزون عند حذفه — المستند يمرّر نوعه ورقمه.</summary>
        void RemoveMovements(DbConnection conn, DbTransaction tx, string sourceDocType, int sourceDocId);

        Result Transfer(int productId, int fromWarehouseId, int toWarehouseId, decimal qty, string notes = null);

        /// <summary>
        /// تكلفة صرف سطورٍ بالمتوسط المرجَّح — إجمالاً لكل سطر، بالترتيب المُعطى. الرصيد يتحرّك مع كل
        /// سطر، فسطران لنفس الصنف في مستندٍ واحد يُسعَّر ثانيهما بمتوسط ما بقي بعد أوّلهما.
        /// </summary>
        Result<List<decimal>> GetIssueCosts(DbConnection conn, DbTransaction tx,
            List<(int ProductId, decimal Qty)> lines);

        /// <summary>تكلفة وحدة الصنف كما سُجّلت في حركة مستندٍ بعينه — يعرف بها المرتجع تكلفة أصله.</summary>
        decimal? SourceUnitCost(DbConnection conn, DbTransaction tx, string sourceDocType, int sourceDocId, int productId);

        /// <summary>متوسط تكلفة الصنف الآن — ملاذُ ما لا أصل له، فلا يدخل المخزون بسعر بيع.</summary>
        decimal CurrentUnitCost(DbConnection conn, DbTransaction tx, int productId);

        /// <summary>كل حركات الصنف بترتيب ورودها وبلا حدّ عدد — مادّة الرصيد الجاري في تقرير حركة الصنف.
        /// GetHistory لا تصلح له: مرتَّبةٌ تنازلياً ومحدودةٌ بعددٍ أقصى، فالرصيد الجاري فيها مقلوبٌ ومبتور.</summary>
        Result<List<Domain.Entities.StockMovement>> GetCostingHistory(int productId);
    }
}
