using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Inventory
{
    /// <summary>عقد أرصدة المخزون</summary>
    public interface IStockService
    {
        Result<decimal> GetBalance(int productId, int? warehouseId = null);

        Result<List<(int ProductId, int WarehouseId, decimal Balance)>> GetAllBalances();

        Result<List<Domain.Entities.StockMovement>> GetHistory(int productId, int? warehouseId, int maxResults = 200);

        Result<List<Domain.Entities.StockMovement>> GetMovements(DateTime from, DateTime to, int? warehouseId = null, int maxResults = 500);

        Result RecordMovement(PrimeDbContext db, int productId, int warehouseId, Domain.Enums.MovementType type,
            decimal qty, decimal unitCost, string sourceDocType, int? sourceDocId, string sourceDocNo, DateTime? date = null, string notes = null);

        void RemoveMovements(PrimeDbContext db, string sourceDocType, int sourceDocId);

        Result Transfer(int productId, int fromWarehouseId, int toWarehouseId, decimal qty, string notes = null);

        Result<List<decimal>> GetIssueCosts(PrimeDbContext db, List<(int ProductId, decimal Qty)> lines);

        decimal? SourceUnitCost(PrimeDbContext db, string sourceDocType, int sourceDocId, int productId);

        decimal CurrentUnitCost(PrimeDbContext db, int productId);

        Result<List<Domain.Entities.StockMovement>> GetCostingHistory(int productId);
    }
}
