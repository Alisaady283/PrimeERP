using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>تتبّع السحب — المتبقي يتناقص مع كل سحب، والسحب الكامل يُصفّره، وحذف المستند الهدف يعيده.</summary>
    [Collection("Database")]
    public class DocumentLinkServiceTests
    {
        private readonly IDocumentLinkService _service;

        public DocumentLinkServiceTests(TestDatabaseFixture db)
        {
            AppSession.DevMode = true;
            _service = db.Services.GetRequiredService<IDocumentLinkService>();
        }

        private static DocumentLink Link(int sourceLineId, int targetId, decimal qty) => new()
        {
            SourceType = "PurchaseOrder", SourceId = 100, SourceNo = "PO-1", SourceLineId = sourceLineId,
            TargetType = "GoodsReceipt", TargetId = targetId, TargetLineId = targetId * 10,
            PulledQty = qty
        };

        [Fact]
        public void PartialPull_LeavesRemainder_AndFullPullZeroesIt()
        {
            const int lineId = 5001;

            Assert.Equal(10m, _service.GetRemainingQty("PurchaseOrder", lineId, 10m));

            _service.RecordPull(new[] { Link(lineId, 1, 4m) });
            Assert.Equal(6m, _service.GetRemainingQty("PurchaseOrder", lineId, 10m));

            _service.RecordPull(new[] { Link(lineId, 2, 6m) });
            Assert.Equal(0m, _service.GetRemainingQty("PurchaseOrder", lineId, 10m));
        }

        [Fact]
        public void RemainingNeverGoesNegative_WhenPulledExceedsOriginal()
        {
            const int lineId = 5002;
            _service.RecordPull(new[] { Link(lineId, 3, 12m) });

            Assert.Equal(0m, _service.GetRemainingQty("PurchaseOrder", lineId, 10m));
        }

        [Fact]
        public void RemovePull_RestoresRemaining_ForThatTargetOnly()
        {
            const int lineId = 5003;
            _service.RecordPull(new[] { Link(lineId, 4, 3m), Link(lineId, 5, 2m) });
            Assert.Equal(5m, _service.GetRemainingQty("PurchaseOrder", lineId, 10m));

            _service.RemovePull("GoodsReceipt", 4);

            Assert.Equal(8m, _service.GetRemainingQty("PurchaseOrder", lineId, 10m));
        }

        [Fact]
        public void ZeroQuantityLinks_AreNotRecorded()
        {
            const int lineId = 5004;
            _service.RecordPull(new[] { Link(lineId, 6, 0m) });

            Assert.Equal(10m, _service.GetRemainingQty("PurchaseOrder", lineId, 10m));
        }

        [Fact]
        public void GetChain_ReturnsBothDirections()
        {
            _service.RecordPull(new[] { Link(5005, 7, 1m) });

            var target = _service.GetChain("GoodsReceipt", 7).Value;
            Assert.Contains(target, c => c.DocType == "PurchaseOrder" && c.DocId == 100);

            var source = _service.GetChain("PurchaseOrder", 100).Value;
            Assert.Contains(source, c => c.DocType == "GoodsReceipt");
        }
    }
}
