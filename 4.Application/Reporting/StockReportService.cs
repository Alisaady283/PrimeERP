using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Reporting
{
    public interface IStockReportService
    {
        Result<ReportData> Balances(DateTime from, DateTime to);
        Result<ReportData> Movements(DateTime from, DateTime to, int? warehouseId);
        Result<ReportData> ItemCard(int productId);
    }

    /// <summary>
    /// تقارير المخزون. كانت أجسامها مكتوبة داخل تسجيل التقارير في 8.Modules تجمع ثلاث خدمات بمنطق
    /// مكتوب هناك — والتجميع منطق أعمال يسكن طبقة التطبيق.
    /// </summary>
    public class StockReportService : IStockReportService
    {
        private readonly IStockService _stock;
        private readonly IProductService _products;
        private readonly IWarehouseService _warehouses;

        public StockReportService(IStockService stock, IProductService products, IWarehouseService warehouses)
        {
            _stock = stock;
            _products = products;
            _warehouses = warehouses;
        }

        public Result<ReportData> Balances(DateTime from, DateTime to)
        {
            var products = _products.GetPaged(1, 5000);
            if (!products.IsSuccess) return Result.Fail<ReportData>(products.ErrorMessage);

            var warehouses = _warehouses.GetAll(true);
            if (!warehouses.IsSuccess) return Result.Fail<ReportData>(warehouses.ErrorMessage);

            // كل الحركات منذ البداية: ما قبل الفترة رصيدٌ أول المدة، وما فيها وارد ومنصرف.
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
            if (productId == 0) return Result.Fail<ReportData>("اختر صنفاً");

            var productResult = _products.GetById(productId);
            if (!productResult.IsSuccess) return Result.Fail<ReportData>("الصنف غير موجود");

            var history = _stock.GetHistory(productResult.Value.Id, null);
            if (!history.IsSuccess) return Result.Fail<ReportData>(history.ErrorMessage);

            var rows = history.Value.Select(m => new ItemCardRow
            {
                Date = m.MovementDate.ToString("yyyy-MM-dd"), MovementType = m.MovementType.ToString(),
                Qty = m.Qty, UnitCost = m.UnitCost, BalanceAfter = m.BalanceAfter, SourceDoc = m.SourceDocNo
            }).ToList();

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new() { ["Product"] = productResult.Value.Name }
            });
        }
    }
}
