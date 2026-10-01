using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Documents
{
    /// <summary>عقد أرصدة المخزون</summary>
    public interface IStockMove
    {
        Result<decimal> GetBalance(int productId, int? warehouseId = null);

        Result<List<(int ProductId, int WarehouseId, decimal Balance)>> GetAllBalances();

        Result<List<Domain.Entities.StockMovement>> GetHistory(int productId, int? warehouseId, int maxResults = 200);

        Result<List<Domain.Entities.StockMovement>> GetMovements(DateTime from, DateTime to, int? warehouseId = null, int maxResults = 500);

        Result RecordMovement(PrimeDbContext db, int productId, int warehouseId, Domain.Enums.MovementType type,
            decimal qty, decimal unitCost, string sourceDocType, int? sourceDocId, string sourceDocNo, DateTime? date = null, string notes = null);

        void RemoveMovements(PrimeDbContext db, string sourceDocType, int sourceDocId);

        Result<(List<decimal> Lines, decimal Total)> GetIssueCosts(PrimeDbContext db, List<(int ProductId, decimal Qty)> lines);

        (List<decimal> UnitCosts, decimal Total) GetReturnCosts(PrimeDbContext db, string sourceDocType,
            List<(int ProductId, decimal Qty, int SourceId, int SourceLineId)> lines, bool recordsStock);

        Result<List<Domain.Entities.StockMovement>> GetCostingHistory(int productId);
    }
}
