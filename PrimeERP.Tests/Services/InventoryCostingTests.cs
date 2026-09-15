using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Rules;
using Xunit;
using Entry = PrimeERP.Domain.Rules.InventoryCosting.Entry;

namespace PrimeERP.Tests.Services
{
    /// <summary>المتوسط المرجَّح المتحرّك — القاعدة النقيّة وحدها، بلا قاعدة بيانات.</summary>
    public class InventoryCostingTests
    {
        private static Entry In(decimal qty, decimal cost) => new(MovementType.In, qty, cost);
        private static Entry Out(decimal qty) => new(MovementType.Out, qty, 0);

        private static readonly Entry[] ThreePurchases =
        {
            In(50, 50),   // 2500
            In(50, 80),   // 4000
            In(50, 75),   // 3750
        };

        [Fact]
        public void EachPurchase_MovesTheAverage()
        {
            var balance = new InventoryCosting.Balance(0, 0);

            balance = InventoryCosting.Apply(balance, ThreePurchases[0], out _);
            Assert.Equal(50m, balance.Qty);
            Assert.Equal(2500m, balance.Value);
            Assert.Equal(50m, balance.UnitCost);

            balance = InventoryCosting.Apply(balance, ThreePurchases[1], out _);
            Assert.Equal(100m, balance.Qty);
            Assert.Equal(6500m, balance.Value);
            Assert.Equal(65m, balance.UnitCost);      // (2500 + 4000) ÷ 100

            balance = InventoryCosting.Apply(balance, ThreePurchases[2], out _);
            Assert.Equal(150m, balance.Qty);
            Assert.Equal(10250m, balance.Value);
        }

        [Fact]
        public void SellingSeventyFive_CostsTheAverageOfTheMoment()
        {
            var balance = InventoryCosting.Replay(ThreePurchases);

            Assert.True(InventoryCosting.TryIssueCost(balance, 75, out var cost));

            // 10250 ÷ 150 = 68.333…  ×75 = 5125
            Assert.Equal(5125m, cost);
        }

        [Fact]
        public void WhatRemainsAfterTheSale_KeepsTheSameAverage()
        {
            var balance = InventoryCosting.Replay(ThreePurchases.Append(Out(75)));

            Assert.Equal(75m, balance.Qty);
            Assert.Equal(5125m, balance.Value);
            Assert.Equal(10250m / 150m, balance.UnitCost);
        }

        [Fact]
        public void SellingEverything_EmptiesTheValueExactly()
        {
            // كسر التقريب لا يترك قيمةً بلا كمية: صرفُ الكل يأخذ القيمة كاملةً.
            var balance = InventoryCosting.Replay(new[] { In(3, 10) }.Append(Out(3)));

            Assert.Equal(0m, balance.Qty);
            Assert.Equal(0m, balance.Value);
            Assert.Equal(0m, balance.UnitCost);
        }

        [Fact]
        public void AThirdOfAnAwkwardValue_LeavesNoResidue()
        {
            var balance = InventoryCosting.Replay(new[] { In(3, 100m / 3m) }.Append(Out(3)));

            Assert.Equal(0m, balance.Qty);
            Assert.Equal(0m, balance.Value);
        }

        [Fact]
        public void AReturnedSale_ReentersAtTheCostItLeftWith()
        {
            // مرتجع بيع يعود بتكلفة صرفه (50)، فلا يلوّث المتوسط.
            var balance = InventoryCosting.Replay(new[] { In(50, 50), Out(30), In(30, 50) });

            Assert.Equal(50m, balance.Qty);
            Assert.Equal(2500m, balance.Value);
            Assert.Equal(50m, balance.UnitCost);
        }

        [Fact]
        public void MoreThanAvailable_IsRefusedAndCostsNothing()
        {
            var balance = InventoryCosting.Replay(new[] { In(5, 20) });

            Assert.False(InventoryCosting.TryIssueCost(balance, 6, out var cost));
            Assert.Equal(0m, cost);
        }

        [Fact]
        public void APositiveAdjustmentAdds_ANegativeOneIssues()
        {
            var balance = InventoryCosting.Replay(new[]
            {
                In(50, 50),
                new Entry(MovementType.Adjustment, 50, 80),
                new Entry(MovementType.Adjustment, -50, 0),
            });

            Assert.Equal(50m, balance.Qty);
            Assert.Equal(3250m, balance.Value);   // 6500 ناقص 50×65
            Assert.Equal(65m, balance.UnitCost);
        }

        [Fact]
        public void AnEmptyHistory_IsAZeroBalance()
        {
            var balance = InventoryCosting.Replay(new List<Entry>());

            Assert.Equal(0m, balance.Qty);
            Assert.Equal(0m, balance.UnitCost);
        }
    }
}
