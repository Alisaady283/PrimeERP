using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Analytics;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>مؤشرات التحليل ولوحاته</summary>
    [Collection("Database")]
    public class AnalyticsTests
    {
        private readonly TestDatabaseFixture _db;

        public AnalyticsTests(TestDatabaseFixture db)
        {
            _db = db;
            AppSession.DevMode = true;
        }

        [Theory]
        [InlineData("ExecutiveAnalytics")]
        [InlineData("SalesAnalytics")]
        [InlineData("PurchaseAnalytics")]
        [InlineData("InventoryAnalytics")]
        [InlineData("LiquidityAnalytics")]
        [InlineData("PeopleAnalytics")]
        [InlineData("ProductionAnalytics")]
        public void EachDashboard_ComputesEveryCardAndChartItDeclares(string key)
        {
            var module = _db.Services.GetRequiredService<IModuleRegistry>().Get(key);
            Assert.NotNull(module?.Dashboard);

            var service = _db.Services.GetRequiredService<IAnalyticsService>();
            var method = typeof(IAnalyticsService).GetMethod(module.Dashboard.Method);
            var result = (Result<AnalyticsData>)method.Invoke(service, new object[] { new DateTime(DateTime.Today.Year, 1, 1), DateTime.Today });

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.All(module.Dashboard.Cards, c => Assert.True(result.Value.Cards.ContainsKey(c.Key), $"{key}: {c.Key}"));
            Assert.All(module.Dashboard.Charts, c => Assert.True(result.Value.Charts.ContainsKey(c.Key), $"{key}: {c.Key}"));
        }

        [Fact]
        public void IndicatorsFollowTheirNamedRules()
        {
            Assert.Equal(25m, AnalyticsCalc.Growth(125, 100));
            Assert.Null(AnalyticsCalc.Growth(10, 0));
            Assert.Equal(36.5m, AnalyticsCalc.DaysOf(100, 1000, 365));
            Assert.Equal(40m, AnalyticsCalc.CashCycle(30, 20, 10));
            Assert.Equal((new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)), AnalyticsCalc.PreviousPeriod(new DateTime(2026, 2, 1), new DateTime(2026, 3, 3)));
            Assert.Equal(3, AnalyticsCalc.Months(new DateTime(2026, 1, 15), new DateTime(2026, 3, 2)).Count);
        }

        [Theory]
        [InlineData(0, AgingBucket.NotDue)]
        [InlineData(15, AgingBucket.Days30)]
        [InlineData(45, AgingBucket.Days60)]
        [InlineData(75, AgingBucket.Days90)]
        [InlineData(100, AgingBucket.Days120)]
        [InlineData(140, AgingBucket.Days150)]
        [InlineData(200, AgingBucket.Over150)]
        public void Aging_CountsDaysPastDue(int overdueDays, AgingBucket expected)
        {
            var asOf = new DateTime(2026, 6, 30);
            Assert.Equal(expected, AnalyticsCalc.Aging(asOf.AddDays(-overdueDays), asOf.AddDays(-200), asOf));
        }

        [Fact]
        public void Abc_GivesTheFewHighValueItemsClassA()
        {
            var classes = AnalyticsCalc.Abc(new[] { ("a", 700m), ("b", 150m), ("c", 100m), ("d", 50m) });

            Assert.Equal(AbcClass.A, classes["a"]);
            Assert.Equal(AbcClass.A, classes["b"]);
            Assert.Equal(AbcClass.B, classes["c"]);
            Assert.Equal(AbcClass.C, classes["d"]);
        }

        [Theory]
        [InlineData(0, MovementSpeed.Dead)]
        [InlineData(0.5, MovementSpeed.Slow)]
        [InlineData(2, MovementSpeed.Normal)]
        [InlineData(6, MovementSpeed.Fast)]
        public void Speed_FromTurnover(double turnover, MovementSpeed expected) =>
            Assert.Equal(expected, AnalyticsCalc.Speed((decimal)turnover));

        [Fact]
        public void DeadStock_IsStockWithNoIssueForNinetyDays()
        {
            var asOf = new DateTime(2026, 6, 30);
            Assert.True(AnalyticsCalc.IsDead(5, null, asOf));
            Assert.True(AnalyticsCalc.IsDead(5, asOf.AddDays(-90), asOf));
            Assert.False(AnalyticsCalc.IsDead(5, asOf.AddDays(-10), asOf));
            Assert.False(AnalyticsCalc.IsDead(0, null, asOf));
        }
    }
}
