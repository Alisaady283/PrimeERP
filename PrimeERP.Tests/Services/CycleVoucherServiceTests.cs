using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Tests.Helpers;
using PrimeERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Legacy.Inventory;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>سندات الدورة</summary>
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

            var product = products.Create(new CreateProductDto
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
        public void GoodsReceiptAddsStock_AndDeliveryNoteRemovesIt()
        {
            var (productId, code, warehouseId) = Seed();
            var stock = _db.Services.GetRequiredService<IStockMove>();

            var receipt = _db.Services.GetRequiredService<IGoodsReceiptService>()
                .Create(Doc(code, warehouseId, 12));
            Assert.True(receipt.IsSuccess, receipt.ErrorMessage);
            Assert.Equal(12m, stock.GetBalance(productId, warehouseId).Value);

            var delivery = _db.Services.GetRequiredService<IDeliveryNoteService>()
                .Create(Doc(code, warehouseId, 5));
            Assert.True(delivery.IsSuccess, delivery.ErrorMessage);
            Assert.Equal(7m, stock.GetBalance(productId, warehouseId).Value);
        }

        [Fact]
        public void GoodsIssueRemovesStock_AndSalesReceiptReturnsIt()
        {
            var (productId, code, warehouseId) = Seed();
            var stock = _db.Services.GetRequiredService<IStockMove>();

            _db.Services.GetRequiredService<IGoodsReceiptService>().Create(Doc(code, warehouseId, 20));

            var issue = _db.Services.GetRequiredService<IGoodsIssueService>().Create(Doc(code, warehouseId, 8));
            Assert.True(issue.IsSuccess, issue.ErrorMessage);
            Assert.Equal(12m, stock.GetBalance(productId, warehouseId).Value);

            var back = _db.Services.GetRequiredService<ISalesReceiptService>().Create(Doc(code, warehouseId, 3));
            Assert.True(back.IsSuccess, back.ErrorMessage);
            Assert.Equal(15m, stock.GetBalance(productId, warehouseId).Value);
        }

        [Fact]
        public void EachVoucherKeepsItsOwnNumberingAndDocuments()
        {
            var (_, code, warehouseId) = Seed();

            var receipt = _db.Services.GetRequiredService<IGoodsReceiptService>().Create(Doc(code, warehouseId, 1)).Value;
            var delivery = _db.Services.GetRequiredService<IDeliveryNoteService>().Create(Doc(code, warehouseId, 1)).Value;

            Assert.StartsWith("GoodsReceipt", receipt.DocNo);
            Assert.StartsWith("DeliveryNote", delivery.DocNo);

            var receipts = _db.Services.GetRequiredService<IGoodsReceiptService>().GetPaged(1, 50).Value.Items;
            Assert.DoesNotContain(receipts, d => d.DocNo == delivery.DocNo);
        }
    }
}
