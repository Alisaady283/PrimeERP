using PrimeERP.Application.Validation;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.PageServices.Common;
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
            var shape = type == MovementType.Adjustment ? Result.Ok() : Check.Valid(qty, new Field<decimal>(q => q, "Str.Qty", Positive: true));
            if (shape.IsFailure) return shape;

            var currentBalance = _movements.GetBalance(productId, warehouseId, db);
            var signedQty = InventoryCosting.Signed(type, qty);

            if (currentBalance + signedQty < 0)
                return Fail("Insufficient", ErrorCode.ValidationFailed);

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

        /// <summary>أثر حركات المستند بإشارته</summary>
        public IEnumerable<(int ProductId, int WarehouseId, decimal Delta)> Effects(string sourceDocType, int sourceDocId, int sign) =>
            _movements.GetBySource(sourceDocType, sourceDocId)
                .Select(m => (m.ProductId, m.WarehouseId, sign * InventoryCosting.Signed(m.MovementType, m.Qty)));

        /// <summary>المخزون لا ينزل تحت الصفر</summary>
        public Result StaysPositive(IEnumerable<(int ProductId, int WarehouseId, decimal Delta)> effects, PrimeDbContext db = null)
        {
            var falling = effects.GroupBy(e => (e.ProductId, e.WarehouseId))
                .Select(g => (Pair: g.Key, Delta: g.Sum(e => e.Delta)))
                .Where(x => x.Delta < 0)
                .ToList();
            if (falling.Count == 0) return Result.Ok();

            var balances = _movements.BalancesOf(falling.Select(x => x.Pair).ToList(), db);
            return falling.Any(x => balances.GetValueOrDefault(x.Pair) + x.Delta < 0)
                ? Fail("Insufficient", ErrorCode.ValidationFailed)
                : Result.Ok();
        }

        /// <summary>حذف حركاته لا يُنزل الرصيد</summary>
        public Result Removable(string sourceDocType, int sourceDocId) =>
            StaysPositive(Effects(sourceDocType, sourceDocId, -1));

        public Result<List<StockMovement>> GetCostingHistory(int productId) =>
            Result.Ok(_movements.GetForCosting(productId));

        /// <summary>تكلفة المرتجع من صرفه الأصلي</summary>
        public (List<decimal> UnitCosts, decimal Total) GetReturnCosts(PrimeDbContext db, string sourceDocType,
            List<(int ProductId, decimal Qty, int SourceId, int SourceLineId)> lines, bool recordsStock)
        {
            var unitCosts = Costs(db, lines, l => l.ProductId, (l, balance) =>
            {
                var unitCost = (l.SourceLineId > 0 ? _movements.GetSourceUnitCost(sourceDocType, l.SourceId, l.ProductId, db) : null)
                               ?? balance.UnitCost;
                var next = recordsStock
                    ? InventoryCosting.Apply(balance, new InventoryCosting.Entry(MovementType.In, l.Qty, unitCost), out _)
                    : balance;
                return Result.Ok((unitCost, next));
            }).Value;

            return (unitCosts, lines.Zip(unitCosts, (l, unitCost) => l.Qty * unitCost).Sum());
        }

        public Result<(List<decimal> Lines, decimal Total)> GetIssueCosts(PrimeDbContext db, List<(int ProductId, decimal Qty)> lines) =>
            Costs(db, lines, l => l.ProductId, (l, balance) =>
                    InventoryCosting.TryIssueCost(balance, l.Qty, out var cost)
                        ? Result.Ok((cost, InventoryCosting.Apply(balance, new InventoryCosting.Entry(MovementType.Out, l.Qty, 0), out _)))
                        : Fail<(decimal, InventoryCosting.Balance)>("Insufficient", ErrorCode.ValidationFailed))
                .Then(costs => Result.Ok((costs, costs.Sum())));

        /// <summary>تكلفة السطور على رصيدٍ جارٍ</summary>
        private Result<List<decimal>> Costs<TLine>(PrimeDbContext db, List<TLine> lines, Func<TLine, int> productOf,
            Func<TLine, InventoryCosting.Balance, Result<(decimal Cost, InventoryCosting.Balance Next)>> step)
        {
            var balances = new Dictionary<int, InventoryCosting.Balance>();
            var costs = new List<decimal>(lines.Count);

            foreach (var line in lines)
            {
                var productId = productOf(line);
                var priced = step(line, balances.TryGetValue(productId, out var balance) ? balance : Replay(db, productId));
                if (priced.IsFailure) return priced.As<List<decimal>>();

                balances[productId] = priced.Value.Next;
                costs.Add(priced.Value.Cost);
            }

            return Result.Ok(costs);
        }

        private InventoryCosting.Balance Replay(PrimeDbContext db, int productId) =>
            InventoryCosting.Replay(_movements.GetForCosting(productId, db)
                .Select(m => new InventoryCosting.Entry(m.MovementType, m.Qty, m.UnitCost)));

    }
}
