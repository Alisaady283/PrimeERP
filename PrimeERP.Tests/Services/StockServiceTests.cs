using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Tests.Helpers;
using PrimeERP.Domain.Entities;
using PrimeERP.Data.Core;
using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Legacy.Common;
using PrimeERP.Application.Legacy.Inventory;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>أرصدة المخزون</summary>
    public class StockServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly IStockMove _stock;
        private readonly int _productId;
        private readonly string _productCode;
        private readonly int _warehouseAId, _warehouseBId;

        public StockServiceTests()
        {
            AppSession.DevMode = true;
            _stock = _db.Services.GetRequiredService<IStockMove>();

            var categories = _db.Services.GetRequiredService<ICategoryService>();
            var category = categories.Create(new Category { Name = "فئة اختبار", ModuleKey = "Products" }).Value;

            var products = _db.Services.GetRequiredService<IProductService>();
            var product = products.Create(new Product { Name = "صنف اختبار", CategoryId = category.Id, CostPrice = 10, SalePrice = 20 }).Value;
            _productId = product.Id;
            _productCode = product.Code;

            var warehouses = _db.Services.GetRequiredService<Lookup<Warehouse>>();
            _warehouseAId = warehouses.Add("مخزن أ");
            _warehouseBId = warehouses.Add("مخزن ب");
        }

        public void Dispose() => _db.Dispose();

        [Fact]
        public void RecordMovement_InThenOut_BalanceReflectsBoth()
        {
            DbContextFactory.RunTransaction(db =>
            {
                var inResult = _stock.RecordMovement(db, _productId, _warehouseAId, MovementType.In, 100, 10, "Test", null, "T-1");
                Assert.True(inResult.IsSuccess);
            });

            Assert.Equal(100, _stock.GetBalance(_productId, _warehouseAId).Value);

            DbContextFactory.RunTransaction(db =>
            {
                var outResult = _stock.RecordMovement(db, _productId, _warehouseAId, MovementType.Out, 30, 10, "Test", null, "T-2");
                Assert.True(outResult.IsSuccess);
            });

            Assert.Equal(70, _stock.GetBalance(_productId, _warehouseAId).Value);
        }

        [Fact]
        public void RecordMovement_OutExceedingBalance_Fails()
        {
            DbContextFactory.RunTransaction(db =>
            {
                var result = _stock.RecordMovement(db, _productId, _warehouseAId, MovementType.Out, 5, 10, "Test", null, "T-1");
                Assert.False(result.IsSuccess);
            });

            Assert.Equal(0, _stock.GetBalance(_productId, _warehouseAId).Value);
        }

        [Fact]
        public void Transfer_MovesBalanceBetweenWarehouses()
        {
            DbContextFactory.RunTransaction(db =>
                _stock.RecordMovement(db, _productId, _warehouseAId, MovementType.In, 50, 10, "Test", null, "T-1"));

            var transferResult = _db.Services.GetRequiredService<IStockTransferService>().Create(new CreateStockTransferDto
            {
                FromWarehouseId = _warehouseAId, ToWarehouseId = _warehouseBId,
                Lines = { new CreateStockTransferLineDto { LineNo = 1, ProductCode = _productCode, Qty = 20 } }
            });
            Assert.True(transferResult.IsSuccess);

            Assert.Equal(30, _stock.GetBalance(_productId, _warehouseAId).Value);
            Assert.Equal(20, _stock.GetBalance(_productId, _warehouseBId).Value);
        }
    }
}
