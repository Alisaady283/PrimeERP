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

        Result Transfer(int productId, int fromWarehouseId, int toWarehouseId, decimal qty, string notes = null);
    }
}
