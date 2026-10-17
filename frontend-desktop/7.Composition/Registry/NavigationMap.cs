using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Composition.Registry
{
    /// <summary>خريطة الشريط الجانبي</summary>
    public static class NavigationMap
    {
        public static readonly (string Key, string IconKey, string[] Modules)[] Coded =
        {
            ("Accounting", "IconAccounts", new[] { "Accounts", "Journals", "OpeningBalances", "TrialBalance", "AccountStatement", "IncomeStatement", "BalanceSheet", "CashFlow" }),
            ("Sales", "IconSales", new[] { "Quotation", "SalesOrder", "SalesInvoices", "SalesReturns", "Customers", "CustomerBalances", "CustomerStatement", "SalesReport", "QuotationFollowUp", "SalesOrderFollowUp", "SalesReturnsByInvoice", "SalesReturnsUnlinked", "CustomerAging", "CustomerInvoiceSettlement" }),
            ("Pos", "IconSales", new[] { "Pos", "PosShifts", "CashierReport" }),
            ("Purchases", "IconPurchases", new[] { "PurchaseRequest", "PurchaseOrder", "PurchaseInvoices", "PurchaseReturns", "Suppliers", "SupplierBalances", "SupplierStatement", "PurchaseRequestFollowUp", "PurchaseOrderFollowUp", "PurchaseReturnsByInvoice", "PurchaseReturnsUnlinked", "SupplierAging", "SupplierInvoiceSettlement" }),
            ("Inventory", "IconWarehouse", new[] { "Products", "Categories", "Brands", "Units", "Warehouses", "StockIn", "StockOut", "StockTransfer", "StockBalances", "ItemCard", "StockReport", "BatchReport", "ExpiryReport", "SalesReturnsPending", "PurchaseReturnsPending" }),
            ("Manufacturing", "IconWarehouse", new[] { "BillsOfMaterials", "ProductionOrders", "Unbuild", "Scrap", "WorkCenters", "ProductionCostReport", "ConsumptionReport", "WorkCenterLoadReport", "ScrapReport" }),
            ("Projects", "IconBuilding", new[] { "Projects", "ProjectTasks", "ProjectTimesheets", "ProjectBills", "ProjectProfitReport" }),
            ("Treasury", "IconAccounts", new[] { "Treasuries", "Receipts", "Payments", "ChequeReceipts", "ChequeIssues", "Cheques" }),
            ("Assets", "IconAssets", new[] { "Assets", "AssetCategories", "AssetRevaluations", "AssetDepreciations", "AssetDisposals", "AssetRegister", "AssetsByCategory" }),
            ("HR", "IconHR", new[] { "Employees", "HrLists", "Attendances", "EmployeeAllowances", "EmployeeDeductions", "EmployeeAdvances", "Payroll", "Payslip", "PayrollSummary", "AttendanceSummary", "AdvancesReport" }),
            ("Reports", "IconReports", new[] { "ExecutiveAnalytics", "SalesAnalytics", "PurchaseAnalytics", "InventoryAnalytics", "LiquidityAnalytics", "PeopleAnalytics", "ProductionAnalytics" }),
            ("Settings", "IconSettings", new[] { "Settings", "Branches", "AlertRules", "ApiKeys", "Users", "Roles", "RolePermissions", "UserPermissions" }),
            ("Builder", "IconSettings", new[] { "BuilderSections", "BuilderModules", "BuilderColumns", "BuilderActions", "BuilderFilters", "BuilderExport" }),
        };

        public static readonly string[] Protected = { "Accounting", "Settings", "Builder" };

        /// <summary>أقسامٌ تشملها كل نسخة</summary>
        public static readonly string[] Always = { "Accounting" };

        /// <summary>الأقسام بعناوينها</summary>
        public static IEnumerable<(string Key, string Text, string IconKey, string[] Modules)> Sections() =>
            Coded.Select(c => (c.Key, LocalizationService.Get($"Str.Nav.{c.Key}"), c.IconKey, c.Modules));

        public static (string Key, string Text, string IconKey, string[] Keys)[] Groups(
            IReadOnlyList<(string Key, string Title, string IconKey, string[] ModuleKeys)> built = null)
        {
            if (built == null || built.Count == 0)
                return Sections().ToArray();

            var seeded = built.Select(b => b.Key).ToHashSet();

            return Sections().Where(c => Protected.Contains(c.Key) && !seeded.Contains(c.Key))
                .Concat(built.Select(b => (b.Key, b.Title, b.IconKey, b.ModuleKeys)))
                .ToArray();
        }
    }
}
