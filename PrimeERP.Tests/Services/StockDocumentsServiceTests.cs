using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    public class StockDocumentsServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly int _productId, _warehouseAId, _warehouseBId;
        private readonly string _productCode;

        public StockDocumentsServiceTests()
        {
            AppSession.DevMode = true;

            var categories = _db.Services.GetRequiredService<ICategoryService>();
            var category = categories.Create(new CreateCategoryDto { Name = "فئة", ModuleKey = "Products" }).Value;
            var products = _db.Services.GetRequiredService<IProductService>();
            var product = products.Create(new CreateProductDto { Name = "صنف", CategoryId = category.Id, CostPrice = 10, SalePrice = 25 }).Value;
            _productId = product.Id; _productCode = product.Code;

            var warehouses = _db.Services.GetRequiredService<IWarehouseService>();
            _warehouseAId = warehouses.Create(new CreateWarehouseDto { Name = "مخزن أ" }).Value.Id;
            _warehouseBId = warehouses.Create(new CreateWarehouseDto { Name = "مخزن ب" }).Value.Id;
        }

        public void Dispose() => _db.Dispose();

        [Fact]
        public void StockIn_ThenStockOut_ThenTransfer_BalancesCorrect()
        {
            var stockIn = _db.Services.GetRequiredService<IStockInService>();
            var inResult = stockIn.Create(new CreateStockAdjustmentDto
            {
                MovementDate = DateTime.Today, WarehouseId = _warehouseAId,
                Lines = { new CreateStockAdjustmentLineDto { LineNo = 1, ProductCode = _productCode, Qty = 50, UnitCost = 10 } }
            });
            Assert.True(inResult.IsSuccess, inResult.ErrorMessage);

            var stock = _db.Services.GetRequiredService<IStockService>();
            Assert.Equal(50, stock.GetBalance(_productId, _warehouseAId).Value);

            var stockOut = _db.Services.GetRequiredService<IStockOutService>();
            var outResult = stockOut.Create(new CreateStockAdjustmentDto
            {
                MovementDate = DateTime.Today, WarehouseId = _warehouseAId,
                Lines = { new CreateStockAdjustmentLineDto { LineNo = 1, ProductCode = _productCode, Qty = 10, UnitCost = 10 } }
            });
            Assert.True(outResult.IsSuccess, outResult.ErrorMessage);
            Assert.Equal(40, stock.GetBalance(_productId, _warehouseAId).Value);

            var transfer = _db.Services.GetRequiredService<IStockTransferService>();
            var transferResult = transfer.Create(new CreateStockTransferDto
            {
                MovementDate = DateTime.Today, FromWarehouseId = _warehouseAId, ToWarehouseId = _warehouseBId,
                Lines = { new CreateStockTransferLineDto { LineNo = 1, ProductCode = _productCode, Qty = 15 } }
            });
            Assert.True(transferResult.IsSuccess, transferResult.ErrorMessage);

            Assert.Equal(25, stock.GetBalance(_productId, _warehouseAId).Value);
            Assert.Equal(15, stock.GetBalance(_productId, _warehouseBId).Value);
        }

        [Fact]
        public void StockOut_ExceedingBalance_Fails()
        {
            var stockOut = _db.Services.GetRequiredService<IStockOutService>();
            var result = stockOut.Create(new CreateStockAdjustmentDto
            {
                MovementDate = DateTime.Today, WarehouseId = _warehouseAId,
                Lines = { new CreateStockAdjustmentLineDto { LineNo = 1, ProductCode = _productCode, Qty = 5, UnitCost = 10 } }
            });
            Assert.False(result.IsSuccess);
        }
    }
}
