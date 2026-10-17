using System;
using System.Collections.Generic;
using PrimeERP.Application.Analytics;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Components.Charts;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Modules
{
    /// <summary>لوحات قسم التحليلات: لكلٍّ سؤالٌ إداريٌّ تجيب عنه</summary>
    public static class AnalyticsRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            Register(registry, "ExecutiveAnalytics", nameof(IAnalyticsService.Executive), new()
            {
                Card("Revenue", "IconSales", drill: "SalesReport"),
                Card("GrossProfit", "IconAccounts", drill: "IncomeStatement"),
                Card("Cash", "IconAccounts", drill: "Treasuries"),
                Card("Receivables", "IconCustomers", drill: "CustomerAging", lowerIsBetter: true),
                Card("Payables", "IconSuppliers", drill: "SupplierAging", lowerIsBetter: true),
                Card("StockValue", "IconWarehouse", drill: "StockBalances"),
            }, new()
            {
                Chart("SalesPurchases", ChartKind.Column, span: 2),
                Chart("Expenses", ChartKind.Donut, drill: "IncomeStatement"),
                Chart("Margin", ChartKind.Area, ValueKind.Percent, span: 2, drill: "IncomeStatement", smooth: true),
                Chart("CashCycle", ChartKind.Bar, ValueKind.Days),
            });

            Trade(registry, "SalesAnalytics", nameof(IAnalyticsService.Sales), "IconSales", "IconCustomers",
                "SalesReport", "CustomerBalances", "SalesReturnsByInvoice", "QuotationFollowUp", "CustomerAging");

            Trade(registry, "PurchaseAnalytics", nameof(IAnalyticsService.Purchases), "IconPurchases", "IconSuppliers",
                null, "SupplierBalances", "PurchaseReturnsByInvoice", "PurchaseOrderFollowUp", "SupplierAging");

            Register(registry, "InventoryAnalytics", nameof(IAnalyticsService.Inventory), new()
            {
                Card("StockValue", "IconWarehouse", drill: "StockBalances"),
                Card("Items", "IconProducts", ValueKind.Count),
                Card("DeadItems", "IconWarning", ValueKind.Count, lowerIsBetter: true),
                Card("Dio", "IconCalendar", ValueKind.Days, lowerIsBetter: true),
                Card("Expiring", "IconWarning", ValueKind.Count, drill: "ExpiryReport", shownWhen: s => s.TracksExpiry()),
            }, new()
            {
                Chart("ByWarehouse", ChartKind.Bar, drill: "StockBalances"),
                Chart("ByCategory", ChartKind.Donut),
                Chart("Abc", ChartKind.Column, ValueKind.Percent),
                Chart("DeadStock", ChartKind.Bar, span: 2),
                Chart("Speed", ChartKind.Column, ValueKind.Count, tone: ChartTone.Improving),
            });

            Register(registry, "PeopleAnalytics", nameof(IAnalyticsService.People), new()
            {
                Card("Headcount", "IconUsers", ValueKind.Count, drill: "Employees"),
                Card("PayrollCost", "IconPayroll", drill: "PayrollSummary"),
                Card("AverageSalary", "IconHR"),
                Card("AbsenceRate", "IconCalendar", ValueKind.Percent, drill: "AttendanceSummary", lowerIsBetter: true),
                Card("OvertimeHours", "IconCalendar", ValueKind.Count, drill: "AttendanceSummary"),
                Card("PendingAdvances", "IconPayroll", drill: "AdvancesReport", lowerIsBetter: true),
            }, new()
            {
                Chart("PayrollTrend", ChartKind.Column, span: 2, drill: "PayrollSummary"),
                Chart("Attendance", ChartKind.Donut, ValueKind.Count, drill: "AttendanceSummary"),
                Chart("HeadcountByDepartment", ChartKind.Bar, ValueKind.Count, drill: "Employees"),
                Chart("CostByDepartment", ChartKind.Donut, drill: "PayrollSummary"),
                Chart("HiresLeavers", ChartKind.Column, ValueKind.Count),
            });

            Register(registry, "ProductionAnalytics", nameof(IAnalyticsService.Production), new()
            {
                Card("Produced", "IconProducts", ValueKind.Count, drill: "ProductionCostReport"),
                Card("ProductionCost", "IconWarehouse", drill: "ProductionCostReport", lowerIsBetter: true),
                Card("UnitCost", "IconReports", lowerIsBetter: true),
                Card("Orders", "IconJournal", ValueKind.Count, drill: "ProductionOrders"),
                Card("ScrapRate", "IconWarning", ValueKind.Percent, drill: "ScrapReport", lowerIsBetter: true),
                Card("WorkHours", "IconCalendar", ValueKind.Count, drill: "WorkCenterLoadReport"),
            }, new()
            {
                Chart("CostTrend", ChartKind.Column, span: 2, drill: "ProductionCostReport"),
                Chart("CostMix", ChartKind.Donut, drill: "ConsumptionReport"),
                Chart("TopProducts", ChartKind.Bar, drill: "ProductionCostReport"),
                Chart("CenterLoad", ChartKind.Bar, ValueKind.Count, drill: "WorkCenterLoadReport"),
                Chart("ScrapTrend", ChartKind.Area, drill: "ScrapReport", smooth: true),
            });

            Register(registry, "LiquidityAnalytics", nameof(IAnalyticsService.Liquidity), new()
            {
                Card("Cash", "IconAccounts", drill: "Treasuries"),
                Card("Inflows", "IconCustomers", drill: "CustomerAging"),
                Card("Outflows", "IconSuppliers", drill: "SupplierAging", lowerIsBetter: true),
                Card("Gap", "IconReports"),
                Card("Coverage", "IconCheck", ValueKind.Percent),
            }, new()
            {
                Chart("Weekly", ChartKind.Column, span: 2),
                Chart("ByTreasury", ChartKind.Bar, drill: "Treasuries"),
                Chart("Aging", ChartKind.Column, span: 3, drill: "CustomerAging"),
            });
        }

        /// <summary>المبيعات والمشتريات لوحةٌ واحدة الشكل بمصدرين</summary>
        private static void Trade(IModuleRegistry registry, string key, string method, string icon, string partyIcon,
            string valueDrill, string balanceDrill, string returnsDrill, string flowDrill, string agingDrill) =>
            Register(registry, key, method, new()
            {
                Card("Net", icon, drill: valueDrill),
                Card("Count", "IconJournal", ValueKind.Count),
                Card("Average", "IconReports"),
                Card("ReturnRate", "IconRefresh", ValueKind.Percent, drill: returnsDrill, lowerIsBetter: true),
                Card("Outstanding", partyIcon, drill: agingDrill, lowerIsBetter: true),
                Card("Flow", "IconCheck", ValueKind.Percent, drill: flowDrill, scope: FlowScope.FullCycleOnly),
                Card("NewParties", partyIcon, ValueKind.Count),
                Card("ActiveParties", partyIcon, ValueKind.Count),
                Card("LostParties", "IconWarning", ValueKind.Count, lowerIsBetter: true),
            }, new()
            {
                Chart("Trend", ChartKind.Area, span: 2, drill: valueDrill, smooth: true),
                Chart("CollectionRate", ChartKind.Gauge, ValueKind.Percent, drill: agingDrill),
                Chart("PartyFlow", ChartKind.Column, ValueKind.Count, span: 2),
                Chart("PartySegments", ChartKind.Donut, ValueKind.Count, tone: ChartTone.Severity),
                Chart("TopParties", ChartKind.Bar, drill: balanceDrill),
                Chart("TopProducts", ChartKind.Bar),
                Chart("ByCategory", ChartKind.Donut),
                Chart("Aging", ChartKind.Column, span: 3, drill: agingDrill, tone: ChartTone.Severity),
            });

        private static void Register(IModuleRegistry registry, string key, string method, List<DashboardCard> cards, List<DashboardChart> charts) =>
            registry.Register(new ModuleDefinition
            {
                Key = key, TitleKey = $"Str.Module.{key}", PermissionPrefix = key, LayoutKind = LayoutKind.Dashboard,
                Dashboard = new DashboardDefinition
                {
                    ServiceType = typeof(IAnalyticsService), Method = method, QuestionKey = $"Str.Analytics.{key}.Question",
                    Parameters = new()
                    {
                        new() { Key = "From", LabelKey = "Str.DateFrom", Kind = FieldKind.Date, DefaultValue = new DateTime(DateTime.Today.Year, 1, 1) },
                        new() { Key = "To", LabelKey = "Str.DateTo", Kind = FieldKind.Date, DefaultValue = DateTime.Today },
                    },
                    Cards = cards.ConvertAll(c => c with { TitleKey = $"Str.Analytics.{key}.{c.Key}" }),
                    Charts = charts.ConvertAll(c => c with { TitleKey = $"Str.Analytics.{key}.{c.Key}", QuestionKey = $"Str.Analytics.{key}.{c.Key}.Question" }),
                }
            });

        private static DashboardCard Card(string key, string icon, ValueKind kind = ValueKind.Money, string drill = null,
            FlowScope scope = FlowScope.Both, Func<ISettingsProvider, bool> shownWhen = null, bool lowerIsBetter = false) =>
            new() { Key = key, TitleKey = key, IconKey = icon, Kind = kind, DrillKey = drill, FlowScope = scope, ShownWhen = shownWhen, LowerIsBetter = lowerIsBetter };

        private static DashboardChart Chart(string key, ChartKind kind, ValueKind value = ValueKind.Money, int span = 1, string drill = null, ChartTone tone = ChartTone.Auto, bool smooth = false) =>
            new() { Key = key, TitleKey = key, Kind = kind, Value = value, Span = span, DrillKey = drill, Tone = tone, Smooth = smooth };
    }
}
