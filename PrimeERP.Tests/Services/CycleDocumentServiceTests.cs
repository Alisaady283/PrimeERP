using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    [Collection("Database")]
    public class CycleDocumentServiceTests
    {
        private readonly TestDatabaseFixture _db;

        public CycleDocumentServiceTests(TestDatabaseFixture db)
        {
            _db = db;
            AppSession.DevMode = true;
        }

        private string SeedProduct()
        {
            var products = _db.Services.GetRequiredService<IProductService>();
            return products.Create(new CreateProductDto
            {
                Name = $"صنف {Guid.NewGuid():N}", CostPrice = 10, SalePrice = 15, IsActive = true
            }).Value.Code;
        }

        private static CreateCycleDocumentDto Doc(string code, int? partyId, decimal qty, decimal price) => new()
        {
            DocDate = DateTime.Today,
            PartyId = partyId,
            Lines = new List<CreateCycleDocumentLineDto>
            {
                new() { ProductCode = code, Qty = qty, UnitPrice = price }
            }
        };

        [Fact]
        public void PurchaseRequestAllowsNoSupplier_ButPurchaseOrderRequiresOne()
        {
            var code = SeedProduct();

            var request = _db.Services.GetRequiredService<IPurchaseRequestService>()
                .Create(Doc(code, null, 5, 0));
            Assert.True(request.IsSuccess, request.ErrorMessage);

            var orderWithout = _db.Services.GetRequiredService<IPurchaseOrderService>()
                .Create(Doc(code, null, 5, 12));
            Assert.False(orderWithout.IsSuccess);

            var orderWith = _db.Services.GetRequiredService<IPurchaseOrderService>()
                .Create(Doc(code, 1, 5, 12));
            Assert.True(orderWith.IsSuccess, orderWith.ErrorMessage);
        }

        [Fact]
        public void CycleDocumentsDoNotTouchStock()
        {
            var products = _db.Services.GetRequiredService<IProductService>();
            var product = products.Create(new CreateProductDto
            { Name = $"صنف {Guid.NewGuid():N}", CostPrice = 10, SalePrice = 15, IsActive = true }).Value;

            var stock = _db.Services.GetRequiredService<IStockService>();
            var before = stock.GetBalance(product.Id, null).Value;

            _db.Services.GetRequiredService<IQuotationService>().Create(Doc(product.Code, null, 9, 20));
            _db.Services.GetRequiredService<ISalesOrderService>().Create(Doc(product.Code, 1, 9, 20));

            Assert.Equal(before, stock.GetBalance(product.Id, null).Value);
        }

        [Fact]
        public void TotalsComeFromLines_AndDeleteRemovesDocument()
        {
            var code = SeedProduct();
            var service = _db.Services.GetRequiredService<IQuotationService>();

            var created = service.Create(Doc(code, null, 4, 25)).Value;
            Assert.Equal(4m, created.TotalQty);
            Assert.Equal(100m, created.Total);

            Assert.True(service.Delete(created.Id).IsSuccess);
            Assert.False(service.GetById(created.Id).IsSuccess);
        }

        [Fact]
        public void EachDocumentKeepsItsOwnNumberingAndList()
        {
            var code = SeedProduct();

            var quote = _db.Services.GetRequiredService<IQuotationService>().Create(Doc(code, null, 1, 5)).Value;
            var order = _db.Services.GetRequiredService<ISalesOrderService>().Create(Doc(code, 1, 1, 5)).Value;

            Assert.StartsWith("Quotation", quote.DocNo);
            Assert.StartsWith("SalesOrder", order.DocNo);

            var quotes = _db.Services.GetRequiredService<IQuotationService>().GetPaged(1, 50).Value.Items;
            Assert.DoesNotContain(quotes, d => d.DocNo == order.DocNo);
        }
    }
}
