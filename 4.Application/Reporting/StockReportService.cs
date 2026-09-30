using PrimeERP.Application.Validation;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Domain.Entities;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Legacy.Inventory;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Reporting
{
    /// <summary>تقارير المخزون</summary>
    public interface IStockReportService
    {
        Result<ReportData> Balances(DateTime from, DateTime to);
        Result<ReportData> Movements(DateTime from, DateTime to, int? warehouseId);
        Result<ReportData> ItemCard(int productId);
    }

    public class StockReportService : ReportServiceBase, IStockReportService
    {
        private readonly IStockMove _stock;
        private readonly IProductService _products;
        private readonly ILookupRepository<Warehouse> _warehouses;
        private readonly IProductRepository _productRows;
        private readonly IStockMovementRepository _movements;

        public StockReportService(IStockMove stock, IProductService products, ILookupRepository<Warehouse> warehouses, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            IProductRepository productRows, IStockMovementRepository movements)
        : base(permissions, settings, localization, audit)
        {
            _productRows = productRows;
            _movements = movements;
            _stock = stock;
            _products = products;
            _warehouses = warehouses;
        }

        public Result<ReportData> Balances(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;


            var grid = _movements.BalanceGrid(from, to);
            var productNames = _productRows.GetByIds(grid.Select(g => g.ProductId)).ToDictionary(x => x.Id, x => (x.Code, x.Name));
            var warehouseNames = _warehouses.GetAll(true).ToDictionary(w => w.Id, w => w.Name);

            var rows = grid
                .Select(g =>
                {
                    var product = productNames.TryGetValue(g.ProductId, out var pn) ? pn : ("", "");
                    return new StockBalanceRow
                    {
                        ProductCode = product.Item1,
                        ProductName = product.Item2,
                        WarehouseName = warehouseNames.TryGetValue(g.WarehouseId, out var wn) ? wn : "",
                        Opening = g.Opening,
                        In = g.In,
                        Out = g.Out,
                        Closing = g.Opening + g.In - g.Out,
                    };
                })
                .Where(r => r.Opening != 0 || r.In != 0 || r.Out != 0 || r.Closing != 0)
                .OrderBy(r => r.ProductCode)
                .ToList();

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new()
                {
                    ["Opening"] = Msg("OpeningIs", rows.Sum(r => r.Opening)),
                    ["In"] = Msg("InIs", rows.Sum(r => r.In)),
                    ["Out"] = Msg("OutIs", rows.Sum(r => r.Out)),
                    ["Closing"] = Msg("ClosingIs", rows.Sum(r => r.Closing)),
                }
            });
        }

        public Result<ReportData> Movements(DateTime from, DateTime to, int? warehouseId)
        {
            var gate = Gate(); if (gate != null) return gate;

            var movements = _stock.GetMovements(from, to, warehouseId);
            if (!movements.IsSuccess) return Result.Fail<ReportData>(movements.ErrorMessage);

            var warehouseNames = _warehouses.GetAll(true).ToDictionary(x => x.Id, x => x.Name);

            var products = _productRows.GetByIds(movements.Value.Select(m => m.ProductId)).ToDictionary(p => p.Id);
            var rows = movements.Value.Select(m =>
            {
                var product = products.GetValueOrDefault(m.ProductId);
                return new StockMovementRow
                {
                    Date = m.MovementDate.ToString("yyyy-MM-dd"),
                    ProductCode = product?.Code,
                    ProductName = product?.Name,
                    WarehouseName = warehouseNames.TryGetValue(m.WarehouseId, out var wn) ? wn : "-",
                    MovementType = m.MovementType.ToString(), Qty = m.Qty, BalanceAfter = m.BalanceAfter
                };
            }).ToList();

            return Result.Ok(new ReportData { Rows = rows });
        }

        public Result<ReportData> ItemCard(int productId)
        {
            var gate = Gate(); if (gate != null) return gate;

            if (productId == 0) return Result.Fail<ReportData>(Msg("PickProduct"));

            var productResult = _products.GetById(productId);
            if (!productResult.IsSuccess) return Result.Fail<ReportData>(Localization.Get("Str.Product.NotFound"));

            var history = _stock.GetCostingHistory(productResult.Value.Id);
            if (!history.IsSuccess) return Result.Fail<ReportData>(history.ErrorMessage);

            var balance = new InventoryCosting.Balance(0, 0);
            var rows = new List<ItemCardRow>();

            foreach (var movement in history.Value)
            {
                var entry = new InventoryCosting.Entry(movement.MovementType, movement.Qty, movement.UnitCost);
                var incoming = InventoryCosting.IsIncoming(entry);
                var qty = movement.Qty < 0 ? -movement.Qty : movement.Qty;

                balance = InventoryCosting.Apply(balance, entry, out var value);

                rows.Add(new ItemCardRow
                {
                    Date = movement.MovementDate.ToString("yyyy-MM-dd"),
                    MovementType = movement.MovementType.ToString(),
                    SourceDoc = movement.SourceDocNo,

                    InQty  = incoming ? qty : 0,
                    InPrice = incoming ? InventoryCosting.UnitCostOf(value, qty) : 0,
                    InValue = incoming ? value : 0,

                    OutQty  = incoming ? 0 : qty,
                    OutPrice = incoming ? 0 : InventoryCosting.UnitCostOf(value, qty),
                    OutValue = incoming ? 0 : value,

                    BalanceQty = balance.Qty,
                    BalancePrice = balance.UnitCost,
                    BalanceValue = balance.Value
                });
            }

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new() { ["Product"] = productResult.Value.Name }
            });
        }
    }
}
