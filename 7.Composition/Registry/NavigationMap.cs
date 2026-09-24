using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Composition.Registry
{
    /// <summary>خريطة الشريط الجانبي</summary>
    public static class NavigationMap
    {
        public static readonly (string Key, string Text, string IconKey, string[] Modules)[] Coded =
        {
            ("Accounting", "المحاسبة", "IconAccounts", new[] { "Accounts", "Journals", "OpeningBalances" }),
            ("Sales", "المبيعات", "IconSales", new[] { "Quotation", "SalesOrder", "SalesInvoices", "SalesReturns", "Customers" }),
            ("Purchases", "المشتريات", "IconPurchases", new[] { "PurchaseRequest", "PurchaseOrder", "PurchaseInvoices", "PurchaseReturns", "Suppliers" }),
            ("Inventory", "المخزون", "IconWarehouse", new[] { "Products", "Categories", "Brands", "Units", "Warehouses", "StockIn", "StockOut", "StockTransfer", "GoodsReceipt", "GoodsIssue", "DeliveryNote", "SalesReceipt" }),
            ("Treasury", "الخزينة", "IconAccounts", new[] { "Treasuries", "Receipts", "Payments", "ChequeReceipts", "ChequeIssues", "Cheques" }),
            ("Assets", "الأصول", "IconAssets", new[] { "Assets", "AssetCategories", "AssetRevaluations", "AssetDepreciations", "AssetDisposals" }),
            ("HR", "الموارد", "IconHR", new[] { "Employees", "Departments", "JobTitles", "Attendances", "EmployeeAllowances", "EmployeeDeductions", "Payroll" }),
            ("Reports", "التقارير", "IconReports", new[] { "TrialBalance", "CustomerBalances", "SupplierBalances", "StockBalances", "AccountStatement", "CustomerStatement", "SupplierStatement", "ItemCard", "Payslip", "IncomeStatement", "BalanceSheet", "CashFlow", "StockReport", "SalesReport", "AssetRegister", "AssetsByCategory" }),
            ("Settings", "الإعدادات", "IconSettings", new[] { "Settings", "Users", "Roles", "RolePermissions", "UserPermissions" }),
            ("Builder", "وحدة البناء", "IconSettings", new[] { "BuilderSections", "BuilderModules", "BuilderColumns", "BuilderActions", "BuilderFilters", "BuilderExport" }),
        };

        public static readonly string[] Protected = { "Accounting", "Settings", "Builder" };

        public static (string Key, string Text, string IconKey, string[] Keys)[] Groups(
            IReadOnlyList<(string Key, string Title, string IconKey, string[] ModuleKeys)> built = null)
        {
            if (built == null || built.Count == 0)
                return Coded.Select(c => (c.Key, c.Text, c.IconKey, c.Modules)).ToArray();

            var seeded = built.Select(b => b.Key).ToHashSet();

            return Coded.Where(c => Protected.Contains(c.Key) && !seeded.Contains(c.Key))
                .Select(c => (c.Key, c.Text, c.IconKey, c.Modules))
                .Concat(built.Select(b => (b.Key, b.Title, b.IconKey, b.ModuleKeys)))
                .ToArray();
        }
    }
}
