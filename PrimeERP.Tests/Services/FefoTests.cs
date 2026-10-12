using System;
using System.Linq;
using PrimeERP.Domain.Calculations;
using Xunit;
using Batch = PrimeERP.Domain.Calculations.InventoryCosting.BatchStock;

namespace PrimeERP.Tests.Services
{
    public class FefoTests
    {
        private static readonly DateTime Today = new(2026, 10, 11);

        [Fact]
        public void TakesTheEarliestExpiryFirst_AndSplitsAcrossBatches()
        {
            var picks = InventoryCosting.Fefo(new[]
            {
                new Batch("B2", Today.AddDays(90), 5),
                new Batch("B1", Today.AddDays(10), 3),
            }, 6, Today);

            Assert.Equal(new[] { ("B1", 3m), ("B2", 3m) }, picks.Select(p => (p.Batch, p.Qty)));
        }

        [Fact]
        public void SkipsExpiredBatches_AndRefusesWhenTheRestIsNotEnough()
        {
            var stock = new[] { new Batch("OLD", Today.AddDays(-1), 10), new Batch("NEW", Today.AddDays(30), 2) };

            Assert.Null(InventoryCosting.Fefo(stock, 3, Today));
            Assert.Equal("NEW", InventoryCosting.Fefo(stock, 2, Today).Single().Batch);
        }
    }
}
