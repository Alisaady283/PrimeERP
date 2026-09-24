using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Domain.Rules;
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
        private readonly IStockService _stock;
        private readonly IProductService _products;
        private readonly IWarehouseService _warehouses;

        public StockReportService(IStockService stock, IProductService products, IWarehouseService warehouses, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
        : base(permissions, settings, localization, audit)
        {
            _stock = stock;
            _products = products;
            _warehouses = warehouses;
        }

        public Result<ReportData> Balances(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;

            var products = _products.GetPaged(1, 5000);
            if (!products.IsSuccess) return Result.Fail<ReportData>(products.ErrorMessage);

            var warehouses = _warehouses.GetAll(true);
            if (!warehouses.IsSuccess) return Result.Fail<ReportData>(warehouses.ErrorMessage);

            var history = _stock.GetMovements(new DateTime(1900, 1, 1), to, null, int.MaxValue);
            if (!history.IsSuccess) return Result.Fail<ReportData>(history.ErrorMessage);

            var productNames = products.Value.Items.ToDictionary(x => x.Id, x => (x.Code, x.Name));
            var warehouseNames = warehouses.Value.ToDictionary(w => w.Id, w => w.Name);

            var rows = history.Value
                .GroupBy(m => (m.ProductId, m.WarehouseId))
                .Select(g =>
                {
                    decimal Signed(Domain.Entities.StockMovement m) =>
                        m.MovementType == MovementType.Out ? -m.Qty : m.Qty;

                    var before = g.Where(m => m.MovementDate < from).Sum(Signed);
                    var inside = g.Where(m => m.MovementDate >= from && m.MovementDate <= to).ToList();
                    var received = inside.Where(m => Signed(m) > 0).Sum(Signed);
                    var issued = -inside.Where(m => Signed(m) < 0).Sum(Signed);

                    var product = productNames.TryGetValue(g.Key.ProductId, out var pn) ? pn : ("", "");
                    return new StockBalanceRow
                    {
                        ProductCode = product.Item1,
                        ProductName = product.Item2,
                        WarehouseName = warehouseNames.TryGetValue(g.Key.WarehouseId, out var wn) ? wn : "",
                        Opening = before,
                        In = received,
                        Out = issued,
                        Closing = before + received - issued,
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
                    ["Opening"] = $"أول المدة: {rows.Sum(r => r.Opening):N2}",
                    ["In"] = $"الوارد: {rows.Sum(r => r.In):N2}",
                    ["Out"] = $"المنصرف: {rows.Sum(r => r.Out):N2}",
                    ["Closing"] = $"آخر المدة: {rows.Sum(r => r.Closing):N2}",
                }
            });
        }

        public Result<ReportData> Movements(DateTime from, DateTime to, int? warehouseId)
        {
            var gate = Gate(); if (gate != null) return gate;

            var movements = _stock.GetMovements(from, to, warehouseId);
            if (!movements.IsSuccess) return Result.Fail<ReportData>(movements.ErrorMessage);

            var warehouses = _warehouses.GetAll();
            var warehouseNames = warehouses.IsSuccess ? warehouses.Value.ToDictionary(x => x.Id, x => x.Name) : new();

            var rows = movements.Value.Select(m =>
            {
                var product = _products.GetById(m.ProductId);
                return new StockMovementRow
                {
                    Date = m.MovementDate.ToString("yyyy-MM-dd"),
                    ProductCode = product.IsSuccess ? product.Value.Code : null,
                    ProductName = product.IsSuccess ? product.Value.Name : null,
                    WarehouseName = warehouseNames.TryGetValue(m.WarehouseId, out var wn) ? wn : "-",
                    MovementType = m.MovementType.ToString(), Qty = m.Qty, BalanceAfter = m.BalanceAfter
                };
            }).ToList();

            return Result.Ok(new ReportData { Rows = rows });
        }

        public Result<ReportData> ItemCard(int productId)
        {
            var gate = Gate(); if (gate != null) return gate;

            if (productId == 0) return Result.Fail<ReportData>("اختر صنفاً");

            var productResult = _products.GetById(productId);
            if (!productResult.IsSuccess) return Result.Fail<ReportData>("الصنف غير موجود");

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
