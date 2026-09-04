using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Pull;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>سلوك السحب كما طُلب حرفياً: ما سُحب لا يظهر مرة أخرى، وعند حذف المستند الهدف يعود المتبقي.
    /// كل ذلك بلا عمود حالة — المتبقي محسوب من DocumentLinks لا مخزَّن.</summary>
    [Collection("Database")]
    public class PullServiceTests
    {
        private readonly TestDatabaseFixture _db;

        public PullServiceTests(TestDatabaseFixture db)
        {
            _db = db;
            AppSession.DevMode = true;
        }

        private static readonly PullSource FromQuotation = new()
        {
            SourceKind = "Quotation",
            Label = "سحب من عرض سعر",
            MatchFields = new List<string>()
        };

        private string SeedProduct()
        {
            var products = _db.Services.GetRequiredService<IProductService>();
            return products.Create(new CreateProductDto
            { Name = $"صنف {Guid.NewGuid():N}", CostPrice = 10, SalePrice = 15, IsActive = true }).Value.Code;
        }

        private CycleDocumentDetailDto SeedQuotation(string code, decimal qty)
        {
            var result = _db.Services.GetRequiredService<IQuotationService>().Create(new CreateCycleDocumentDto
            {
                DocDate = DateTime.Today,
                Lines = new List<CreateCycleDocumentLineDto> { new() { ProductCode = code, Qty = qty, UnitPrice = 20 } }
            });
            Assert.True(result.IsSuccess, result.ErrorMessage);
            return result.Value;
        }

        private PullCandidate FindCandidate(int sourceId)
        {
            var available = _db.Services.GetRequiredService<IPullService>().GetAvailable(FromQuotation, new Dictionary<string, object>());
            Assert.True(available.IsSuccess, available.ErrorMessage);
            return available.Value.FirstOrDefault(c => c.SourceId == sourceId);
        }

        private int PullInto(CycleDocumentDetailDto quotation, decimal qty)
        {
            var line = quotation.Lines.Single();
            var order = _db.Services.GetRequiredService<ISalesOrderService>().Create(new CreateCycleDocumentDto
            {
                DocDate = DateTime.Today,
                PartyId = 1,
                Lines = new List<CreateCycleDocumentLineDto>
                {
                    new()
                    {
                        ProductCode = line.ProductCode, Qty = qty, UnitPrice = line.UnitPrice,
                        SourceType = "Quotation", SourceId = quotation.Id, SourceNo = quotation.DocNo, SourceLineId = line.Id
                    }
                }
            });
            Assert.True(order.IsSuccess, order.ErrorMessage);
            return order.Value.Id;
        }

        [Fact]
        public void PartialPull_LeavesOnlyTheRemainderAvailable()
        {
            var quotation = SeedQuotation(SeedProduct(), 10);
            Assert.Equal(10, FindCandidate(quotation.Id).Lines.Single().RemainingQty);

            PullInto(quotation, 4);

            var afterPull = FindCandidate(quotation.Id);
            Assert.Equal(6, afterPull.Lines.Single().RemainingQty);
            Assert.Equal(4, afterPull.Lines.Single().PulledQty);
        }

        [Fact]
        public void FullyPulledDocument_DisappearsFromPullList()
        {
            var quotation = SeedQuotation(SeedProduct(), 7);
            PullInto(quotation, 7);

            Assert.Null(FindCandidate(quotation.Id));
        }

        [Fact]
        public void DeletingTargetDocument_ReturnsTheQuantityToTheSource()
        {
            var quotation = SeedQuotation(SeedProduct(), 9);
            var orderId = PullInto(quotation, 9);
            Assert.Null(FindCandidate(quotation.Id));

            var deleted = _db.Services.GetRequiredService<ISalesOrderService>().Delete(orderId);
            Assert.True(deleted.IsSuccess, deleted.ErrorMessage);

            var restored = FindCandidate(quotation.Id);
            Assert.NotNull(restored);
            Assert.Equal(9, restored.Lines.Single().RemainingQty);
            Assert.Equal(0, restored.Lines.Single().PulledQty);
        }

        [Fact]
        public void PullingMoreThanRemaining_IsRejectedByTheService()
        {
            var quotation = SeedQuotation(SeedProduct(), 5);
            PullInto(quotation, 3);

            var line = quotation.Lines.Single();
            var overPull = _db.Services.GetRequiredService<ISalesOrderService>().Create(new CreateCycleDocumentDto
            {
                DocDate = DateTime.Today,
                PartyId = 1,
                Lines = new List<CreateCycleDocumentLineDto>
                {
                    new()
                    {
                        ProductCode = line.ProductCode, Qty = 4, UnitPrice = line.UnitPrice,
                        SourceType = "Quotation", SourceId = quotation.Id, SourceNo = quotation.DocNo, SourceLineId = line.Id
                    }
                }
            });

            Assert.False(overPull.IsSuccess);
            Assert.Contains("المتبقي", overPull.ErrorMessage);
        }
    }
}
