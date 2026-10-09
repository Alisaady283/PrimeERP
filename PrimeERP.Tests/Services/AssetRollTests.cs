using System;
using System.Collections.Generic;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using Xunit;

namespace PrimeERP.Tests.Services
{
    public class AssetRollTests
    {
        private static readonly DateTime From = new(2026, 4, 1);
        private static readonly Asset Machine = new() { PurchaseCost = 12000, UsefulLifeYears = 5 };

        private static List<AssetDepreciation> Charges() => new()
        {
            new() { PeriodDate = new(2026, 2, 28), Amount = 200 },
            new() { PeriodDate = new(2026, 3, 31), Amount = 200 },
            new() { PeriodDate = new(2026, 4, 30), Amount = 200 },
            new() { PeriodDate = new(2026, 5, 31), Amount = 200 },
            new() { PeriodDate = new(2026, 6, 30), Amount = 200 },
        };

        private static List<AssetRevaluation> Revaluations() => new()
        {
            new() { RevaluationDate = new(2026, 5, 1), OldValue = 11400, NewValue = 12400 },
        };

        [Fact]
        public void AnActiveAsset_RollsFromOpeningToClosing()
        {
            var roll = AssetCalc.Roll(Machine, Charges(), Revaluations(), null, From);

            Assert.Equal(new Movement(1000, 0, 20, 400, 600, 1000, 12000), roll);
        }

        [Fact]
        public void AnAssetSoldInThePeriod_LeavesNothing()
        {
            var roll = AssetCalc.Roll(Machine, Charges(), Revaluations(), new AssetDisposal { DisposalDate = new(2026, 6, 30) }, From);

            Assert.Equal(13000, roll.Reductions);
            Assert.Equal(0, roll.AccumulatedEnd);
            Assert.Equal(0, roll.Net);
        }
    }
}
