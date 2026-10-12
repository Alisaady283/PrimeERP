using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Tests.Helpers;
using PrimeERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>إذنا الإضافة والصرف</summary>
    [Collection("Database")]
    public class CycleVoucherServiceTests
    {
        private readonly TestDatabaseFixture _db;

        public CycleVoucherServiceTests(TestDatabaseFixture db)
        {
            _db = db;
            AppSession.DevMode = true;
        }

        private (int productId, string code, int warehouseId) Seed()
        {
            var products = _db.Services.GetRequiredService<IProductService>();
            var warehouses = _db.Services.GetRequiredService<Lookup<Warehouse>>();

            var warehouseId = warehouses.Add($"مخزن {Guid.NewGuid():N}");

            var product = products.Create(new Product
            {
                Name = $"صنف {Guid.NewGuid():N}", CostPrice = 10, SalePrice = 15, IsActive = true
            }).Value;

            return (product.Id, product.Code, warehouseId);
        }

        private static CreateStockAdjustmentDto Doc(string code, int warehouseId, decimal qty) => new()
        {
            MovementDate = DateTime.Today,
            WarehouseId = warehouseId,
            Lines = new List<CreateStockAdjustmentLineDto>
            {
                new() { ProductCode = code, Qty = qty, UnitCost = 10 }
            }
        };

        [Fact]
        public void StockInAddsStock_AndStockOutRemovesIt()
        {
            var (productId, code, warehouseId) = Seed();
            var stock = _db.Services.GetRequiredService<IStockMove>();

            var receipt = _db.Services.GetRequiredService<IStockInService>()
                .Create(Doc(code, warehouseId, 12));
            Assert.True(receipt.IsSuccess, receipt.ErrorMessage);
            Assert.Equal(12m, stock.GetBalance(productId, warehouseId).Value);

            var delivery = _db.Services.GetRequiredService<IStockOutService>()
                .Create(Doc(code, warehouseId, 5));
            Assert.True(delivery.IsSuccess, delivery.ErrorMessage);
            Assert.Equal(7m, stock.GetBalance(productId, warehouseId).Value);
        }

        [Fact]
        public void StockOutRemovesStock_AndStockInReturnsIt()
        {
            var (productId, code, warehouseId) = Seed();
            var stock = _db.Services.GetRequiredService<IStockMove>();

            _db.Services.GetRequiredService<IStockInService>().Create(Doc(code, warehouseId, 20));

            var issue = _db.Services.GetRequiredService<IStockOutService>().Create(Doc(code, warehouseId, 8));
            Assert.True(issue.IsSuccess, issue.ErrorMessage);
            Assert.Equal(12m, stock.GetBalance(productId, warehouseId).Value);

            var back = _db.Services.GetRequiredService<IStockInService>().Create(Doc(code, warehouseId, 3));
            Assert.True(back.IsSuccess, back.ErrorMessage);
            Assert.Equal(15m, stock.GetBalance(productId, warehouseId).Value);
        }

        [Fact]
        public void EachVoucherKeepsItsOwnNumberingAndDocuments()
        {
            var (_, code, warehouseId) = Seed();

            var receipt = _db.Services.GetRequiredService<IStockInService>().Create(Doc(code, warehouseId, 1)).Value;
            var delivery = _db.Services.GetRequiredService<IStockOutService>().Create(Doc(code, warehouseId, 1)).Value;

            Assert.Matches("^IN[0-9]+$", receipt.DocNo);
            Assert.Matches("^OUT[0-9]+$", delivery.DocNo);

            var receipts = _db.Services.GetRequiredService<IStockInService>().GetPaged(1, 50).Value.Items;
            Assert.DoesNotContain(receipts, d => d.DocNo == delivery.DocNo);
        }
    }
}
