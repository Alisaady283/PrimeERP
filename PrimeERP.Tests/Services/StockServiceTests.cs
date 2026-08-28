using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;
using Xunit;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Tests.Services
{
    public class StockServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly IStockService _stock;
        private readonly int _productId;
        private readonly int _warehouseAId, _warehouseBId;

        public StockServiceTests()
        {
            AppSession.DevMode = true;
            _stock = _db.Services.GetRequiredService<IStockService>();

            var categories = _db.Services.GetRequiredService<ICategoryService>();
            var category = categories.Create(new CreateCategoryDto { Name = "فئة اختبار", ModuleKey = "Products" }).Value;

            var products = _db.Services.GetRequiredService<IProductService>();
            _productId = products.Create(new CreateProductDto { Name = "صنف اختبار", CategoryId = category.Id, CostPrice = 10, SalePrice = 20 }).Value.Id;

            var warehouses = _db.Services.GetRequiredService<IWarehouseService>();
            _warehouseAId = warehouses.Create(new CreateWarehouseDto { Name = "مخزن أ" }).Value.Id;
            _warehouseBId = warehouses.Create(new CreateWarehouseDto { Name = "مخزن ب" }).Value.Id;
        }

        public void Dispose() => _db.Dispose();

        [Fact]
        public void RecordMovement_InThenOut_BalanceReflectsBoth()
        {
            Db.RunTransaction((conn, tx) =>
            {
                var inResult = _stock.RecordMovement(conn, tx, _productId, _warehouseAId, MovementType.In, 100, 10, "Test", null, "T-1");
                Assert.True(inResult.IsSuccess);
            });

            Assert.Equal(100, _stock.GetBalance(_productId, _warehouseAId).Value);

            Db.RunTransaction((conn, tx) =>
            {
                var outResult = _stock.RecordMovement(conn, tx, _productId, _warehouseAId, MovementType.Out, 30, 10, "Test", null, "T-2");
                Assert.True(outResult.IsSuccess);
            });

            Assert.Equal(70, _stock.GetBalance(_productId, _warehouseAId).Value);
        }

        [Fact]
        public void RecordMovement_OutExceedingBalance_Fails()
        {
            Db.RunTransaction((conn, tx) =>
            {
                var result = _stock.RecordMovement(conn, tx, _productId, _warehouseAId, MovementType.Out, 5, 10, "Test", null, "T-1");
                Assert.False(result.IsSuccess);
            });

            Assert.Equal(0, _stock.GetBalance(_productId, _warehouseAId).Value);
        }

        [Fact]
        public void Transfer_MovesBalanceBetweenWarehouses()
        {
            Db.RunTransaction((conn, tx) =>
                _stock.RecordMovement(conn, tx, _productId, _warehouseAId, MovementType.In, 50, 10, "Test", null, "T-1"));

            var transferResult = _stock.Transfer(_productId, _warehouseAId, _warehouseBId, 20);
            Assert.True(transferResult.IsSuccess);

            Assert.Equal(30, _stock.GetBalance(_productId, _warehouseAId).Value);
            Assert.Equal(20, _stock.GetBalance(_productId, _warehouseBId).Value);
        }
    }
}
