namespace PrimeERP.Composition.Registry
{
    /// <summary>خريطة الشريط الجانبي: مجموعات ومفاتيح الوحدات داخلها. تعريف تركيب لا كود واجهة — MainWindow
    /// يقرأها فقط، واختبار NavigationGroupsTests يطابقها بسجلّ الوحدات فلا تختفي وحدة مسجَّلة بصمت.</summary>
    public static class NavigationMap
    {
        public static readonly (string Text, string IconKey, string[] Keys)[] Groups =
        {
            ("المحاسبة", "IconAccounts", new[] { "Accounts", "Journals", "OpeningBalances" }),
            ("المبيعات", "IconSales", new[] { "Quotation", "SalesOrder", "SalesInvoices", "SalesReturns", "Customers" }),
            ("المشتريات", "IconPurchases", new[] { "PurchaseRequest", "PurchaseOrder", "PurchaseInvoices", "PurchaseReturns", "Suppliers" }),
            ("المخزون", "IconWarehouse", new[] { "Products", "Categories", "Brands", "Units", "Warehouses", "StockIn", "StockOut", "StockTransfer", "GoodsReceipt", "GoodsIssue", "DeliveryNote", "SalesReceipt" }),
            ("الخزينة", "IconAccounts", new[] { "Treasuries", "Receipts", "Payments", "ChequeReceipts", "ChequeIssues", "Cheques" }),
            ("الأصول", "IconAssets", new[] { "Assets", "AssetCategories" }),
            ("الموارد", "IconHR", new[] { "Employees", "Departments", "JobTitles", "Payroll" }),
            ("التقارير", "IconReports", new[] { "TrialBalance", "CustomerBalances", "SupplierBalances", "StockBalances", "AccountStatement", "CustomerStatement", "SupplierStatement", "ItemCard", "IncomeStatement", "BalanceSheet", "CashFlow", "StockReport", "SalesReport" }),
            ("الإعدادات", "IconSettings", new[] { "Settings", "Users", "Roles", "RolePermissions", "UserPermissions" }),
        };
    }
}
