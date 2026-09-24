using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Reporting;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>تقرير حركة الصنف بالمتوسط المرجَّح</summary>
    public class ItemCardReportTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly IStockReportService _report;
        private readonly int _productId, _warehouseId;

        public ItemCardReportTests()
        {
            AppSession.DevMode = true;
            _report = _db.Services.GetRequiredService<IStockReportService>();

            var category = _db.Services.GetRequiredService<ICategoryService>()
                .Create(new CreateCategoryDto { Name = "فئة", ModuleKey = "Products" }).Value;

            _productId = _db.Services.GetRequiredService<IProductService>()
                .Create(new CreateProductDto { Name = "صنف", CategoryId = category.Id, CostPrice = 1, SalePrice = 2 }).Value.Id;

            _warehouseId = _db.Services.GetRequiredService<IWarehouseService>()
                .Create(new CreateWarehouseDto { Name = "مخزن" }).Value.Id;
        }

        public void Dispose() => _db.Dispose();

        private void Move(MovementType type, decimal qty, decimal unitCost, int day)
        {
            var stock = _db.Services.GetRequiredService<IStockService>();
            DbContextFactory.RunTransaction(db => stock.RecordMovement(db, _productId, _warehouseId,
                type, qty, unitCost, "Test", null, $"T{day}", new DateTime(2026, 1, day)));
        }

        private List<ItemCardRow> Card()
        {
            var result = _report.ItemCard(_productId);
            Assert.True(result.IsSuccess, result.ErrorMessage);
            return result.Value.Rows.Cast<ItemCardRow>().ToList();
        }

        private void Purchases()
        {
            Move(MovementType.In, 50, 50, 1);
            Move(MovementType.In, 50, 80, 2);
            Move(MovementType.In, 50, 75, 3);
        }

        [Fact]
        public void EachPurchaseRow_ShowsItsInAndTheRunningBalance()
        {
            Purchases();
            var rows = Card();

            Assert.Equal(3, rows.Count);

            Assert.Equal(50m, rows[0].InQty);
            Assert.Equal(50m, rows[0].InPrice);
            Assert.Equal(2500m, rows[0].InValue);
            Assert.Equal(50m, rows[0].BalanceQty);
            Assert.Equal(50m, rows[0].BalancePrice);
            Assert.Equal(2500m, rows[0].BalanceValue);

            Assert.Equal(4000m, rows[1].InValue);
            Assert.Equal(100m, rows[1].BalanceQty);
            Assert.Equal(65m, rows[1].BalancePrice);
            Assert.Equal(6500m, rows[1].BalanceValue);

            Assert.Equal(3750m, rows[2].InValue);
            Assert.Equal(150m, rows[2].BalanceQty);
            Assert.Equal(10250m, rows[2].BalanceValue);
        }

        [Fact]
        public void TheSaleRow_CostsTheAverageOfItsMoment()
        {
            Purchases();
            Move(MovementType.Out, 75, 0, 4);

            var sale = Card().Last();

            Assert.Equal(0m, sale.InQty);
            Assert.Equal(75m, sale.OutQty);
            Assert.Equal(5125m, sale.OutValue);          // 75 × (10250 ÷ 150)
            Assert.Equal(10250m / 150m, sale.OutPrice);

            Assert.Equal(75m, sale.BalanceQty);
            Assert.Equal(5125m, sale.BalanceValue);
            Assert.Equal(10250m / 150m, sale.BalancePrice);
        }

        [Fact]
        public void TheSalePriceWritten_DoesNotChangeTheCost()
        {
            Purchases();
            Move(MovementType.Out, 75, 999, 4);

            Assert.Equal(5125m, Card().Last().OutValue);
        }

        [Fact]
        public void AnEmptyCard_HasNoRows()
        {
            Assert.Empty(Card());
        }
    }
}
