using PrimeERP.Application.Validation;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Legacy.Common;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Documents
{
    /// <summary>أرصدة المخزون وحركته</summary>
    public class StockMove : ServiceBase, IStockMove
    {
        protected override string PermissionPrefix => "Inventory";
        protected override string StringPrefix => "Str.Stock";
        protected override string EntityName => "StockMovements";

        private readonly IStockMovementRepository _movements;
        private readonly INumberSequenceService _numbers;

        public StockMove(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IStockMovementRepository movements, INumberSequenceService numbers) : base(permissions, settings, localization, audit)
        {
            _movements = movements;
            _numbers = numbers;
        }

        public Result<decimal> GetBalance(int productId, int? warehouseId = null) =>
            Result.Ok(_movements.GetBalance(productId, warehouseId));

        public Result<List<(int ProductId, int WarehouseId, decimal Balance)>> GetAllBalances() =>
            Result.Ok(_movements.GetAllBalances());

        public Result<List<Domain.Entities.StockMovement>> GetHistory(int productId, int? warehouseId, int maxResults = 200) =>
            Result.Ok(_movements.GetHistory(productId, warehouseId, maxResults));

        public Result<List<Domain.Entities.StockMovement>> GetMovements(DateTime from, DateTime to, int? warehouseId = null, int maxResults = 500) =>
            Result.Ok(_movements.GetMovements(from, to, warehouseId, maxResults));

        public Result RecordMovement(PrimeDbContext db, int productId, int warehouseId, MovementType type,
            decimal qty, decimal unitCost, string sourceDocType, int? sourceDocId, string sourceDocNo, DateTime? date = null, string notes = null)
        {
            if (type != MovementType.Adjustment && qty <= 0) return Fail(Localization.Get("Str.Document.QtyPositive"), ErrorCode.ValidationFailed);

            var currentBalance = _movements.GetBalance(productId, warehouseId, db);
            var signedQty = type == MovementType.Out ? -qty : qty;

            if (currentBalance + signedQty < 0)
                return Fail(Msg("Insufficient"), ErrorCode.ValidationFailed);

            var movement = new StockMovement
            {
                MovementNo = _numbers.Next(db, "StockMovement"), MovementDate = date ?? DateTime.Now,
                ProductId = productId, WarehouseId = warehouseId, MovementType = type, Qty = qty, UnitCost = unitCost,
                TotalCost = Math.Abs(qty) * unitCost, BalanceAfter = currentBalance + signedQty,
                SourceDocType = sourceDocType, SourceDocId = sourceDocId, SourceDocNo = sourceDocNo, Notes = notes
            };

            _movements.Insert(movement, db);
            return Result.Ok();
        }

        public void RemoveMovements(PrimeDbContext db, string sourceDocType, int sourceDocId) =>
            _movements.DeleteBySource(db, sourceDocType, sourceDocId);

        public decimal? SourceUnitCost(PrimeDbContext db, string sourceDocType, int sourceDocId, int productId) =>
            _movements.GetSourceUnitCost(sourceDocType, sourceDocId, productId, db);

        public Result<List<StockMovement>> GetCostingHistory(int productId) =>
            Result.Ok(_movements.GetForCosting(productId));

        public decimal CurrentUnitCost(PrimeDbContext db, int productId) =>
            InventoryCosting.Replay(_movements.GetForCosting(productId, db)
                .Select(m => new InventoryCosting.Entry(m.MovementType, m.Qty, m.UnitCost))).UnitCost;

        public Result<List<decimal>> GetIssueCosts(PrimeDbContext db, List<(int ProductId, decimal Qty)> lines)
        {
            var balances = new Dictionary<int, InventoryCosting.Balance>();
            var costs = new List<decimal>(lines.Count);

            foreach (var (productId, qty) in lines)
            {
                if (!balances.TryGetValue(productId, out var balance))
                {
                    balance = InventoryCosting.Replay(_movements.GetForCosting(productId, db)
                        .Select(m => new InventoryCosting.Entry(m.MovementType, m.Qty, m.UnitCost)));
                    balances[productId] = balance;
                }

                if (!InventoryCosting.TryIssueCost(balance, qty, out var cost))
                    return Fail<List<decimal>>(Msg("Insufficient"), ErrorCode.ValidationFailed);

                balances[productId] = InventoryCosting.Apply(balance,
                    new InventoryCosting.Entry(MovementType.Out, qty, 0), out _);

                costs.Add(cost);
            }

            return Result.Ok(costs);
        }

    }
}
