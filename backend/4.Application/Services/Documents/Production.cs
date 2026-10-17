using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Documents
{
    public record ProductionRun(string Source, int DocumentId, string DocumentNo, DateTime Date, string Description,
        ProductionOrderLine Finished, decimal Qty, int RawWarehouseId, int FinishedWarehouseId,
        IReadOnlyList<ProductionOrderLine> Components, IReadOnlyList<ProductionOrderLine> ByProducts,
        IReadOnlyList<decimal> OperationCosts, string InventoryAccount, string OverheadAccount);

    public record ProductionCost(decimal Materials, decimal Operations, decimal Total, int? EntryId);

    public class Production
    {
        private readonly IStockMove _stock;
        private readonly Entries _journals;

        public Production(IStockMove stock, Entries journals)
        {
            _stock = stock;
            _journals = journals;
        }

        public Result<ProductionCost> Produce(PrimeDbContext db, ProductionRun run)
        {
            var issued = _stock.GetIssueCosts(db, run.Components.Select(l => (l.ProductId, l.Qty)).ToList());
            if (issued.IsFailure) return issued.As<ProductionCost>();

            for (var i = 0; i < run.Components.Count; i++)
            {
                var line = run.Components[i];
                line.UnitCost = InventoryCosting.UnitCostOf(issued.Value.Lines[i], line.Qty);
                var moved = Move(db, run, line, run.RawWarehouseId, MovementType.Out, line.Qty, line.UnitCost);
                if (moved.IsFailure) return moved.As<ProductionCost>();
            }

            foreach (var line in run.ByProducts)
            {
                var moved = Move(db, run, line, run.FinishedWarehouseId, MovementType.In, line.Qty, line.UnitCost);
                if (moved.IsFailure) return moved.As<ProductionCost>();
            }

            var operations = run.OperationCosts.Sum();
            var total = InventoryCosting.Produced(issued.Value.Total, operations, run.ByProducts.Sum(l => l.Qty * l.UnitCost));
            run.Finished.UnitCost = InventoryCosting.UnitCostOf(total, run.Qty);
            var finished = Move(db, run, run.Finished, run.FinishedWarehouseId, MovementType.In, run.Qty, run.Finished.UnitCost);
            if (finished.IsFailure) return finished.As<ProductionCost>();

            int? entry = operations > 0
                ? Posting.Entry(_journals, db, run.Date, run.Description, run.Source, run.InventoryAccount, run.OverheadAccount, operations)
                : null;

            return Result.Ok(new ProductionCost(issued.Value.Total, operations, total, entry));
        }

        public Result<ProductionCost> Unbuild(PrimeDbContext db, ProductionRun run)
        {
            var issued = _stock.GetIssueCosts(db, new List<(int, decimal)> { (run.Finished.ProductId, run.Qty) });
            if (issued.IsFailure) return issued.As<ProductionCost>();

            run.Finished.UnitCost = InventoryCosting.UnitCostOf(issued.Value.Total, run.Qty);
            var issuedOut = Move(db, run, run.Finished, run.FinishedWarehouseId, MovementType.Out, run.Qty, run.Finished.UnitCost);
            if (issuedOut.IsFailure) return issuedOut.As<ProductionCost>();

            var shares = InventoryCosting.Distribute(issued.Value.Total, run.Components.Select(l => l.Qty * l.UnitCost).ToList());
            for (var i = 0; i < run.Components.Count; i++)
            {
                var line = run.Components[i];
                line.UnitCost = InventoryCosting.UnitCostOf(shares[i], line.Qty);
                var moved = Move(db, run, line, run.RawWarehouseId, MovementType.In, line.Qty, line.UnitCost);
                if (moved.IsFailure) return moved.As<ProductionCost>();
            }

            return Result.Ok(new ProductionCost(issued.Value.Total, 0, issued.Value.Total, null));
        }

        private Result Move(PrimeDbContext db, ProductionRun run, ProductionOrderLine line, int warehouseId, MovementType type, decimal qty, decimal unitCost) =>
            _stock.Move(db, line, warehouseId, type, qty, unitCost, run.Source, run.DocumentId, run.DocumentNo, run.Date);
    }
}
