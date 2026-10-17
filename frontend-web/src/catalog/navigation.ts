/** الأيقونة باسمها: كل عميلٍ يربطه بمكتبته */
export type SectionIcon = "calculator" | "shopping-bag" | "store" | "shopping-cart" | "boxes" | "factory" | "wallet" | "landmark" | "users" | "bar-chart" | "settings" | "wrench" | "briefcase";

export type Section = { key: string; icon: SectionIcon; pages: string[] };

export const sections: Section[] = [
  { key: "Accounting", icon: "calculator", pages: ["Accounts", "Journals", "OpeningBalances", "TrialBalance", "AccountStatement", "IncomeStatement", "BalanceSheet", "CashFlow"] },
  { key: "Sales", icon: "shopping-bag", pages: ["Quotation", "SalesOrder", "SalesInvoices", "SalesReturns", "Customers", "CustomerBalances", "CustomerStatement", "SalesReport", "QuotationFollowUp", "SalesOrderFollowUp", "SalesReturnsByInvoice", "SalesReturnsUnlinked", "CustomerAging", "CustomerInvoiceSettlement"] },
  { key: "Pos", icon: "store", pages: ["Pos", "PosShifts", "CashierReport"] },
  { key: "Purchases", icon: "shopping-cart", pages: ["PurchaseRequest", "PurchaseOrder", "PurchaseInvoices", "PurchaseReturns", "Suppliers", "SupplierBalances", "SupplierStatement", "PurchaseRequestFollowUp", "PurchaseOrderFollowUp", "PurchaseReturnsByInvoice", "PurchaseReturnsUnlinked", "SupplierAging", "SupplierInvoiceSettlement"] },
  { key: "Inventory", icon: "boxes", pages: ["Products", "Categories", "Brands", "Units", "Warehouses", "StockIn", "StockOut", "StockTransfer", "StockBalances", "ItemCard", "StockReport", "BatchReport", "ExpiryReport", "SalesReturnsPending", "PurchaseReturnsPending"] },
  { key: "Manufacturing", icon: "factory", pages: ["BillsOfMaterials", "ProductionOrders", "Unbuild", "Scrap", "WorkCenters", "ProductionCostReport", "ConsumptionReport", "WorkCenterLoadReport", "ScrapReport"] },
  { key: "Projects", icon: "briefcase", pages: ["Projects", "ProjectTasks", "ProjectTimesheets", "ProjectBills", "ProjectProfitReport"] },
  { key: "Treasury", icon: "wallet", pages: ["Treasuries", "Receipts", "Payments", "ChequeReceipts", "ChequeIssues", "Cheques"] },
  { key: "Assets", icon: "landmark", pages: ["Assets", "AssetCategories", "AssetRevaluations", "AssetDepreciations", "AssetDisposals", "AssetRegister", "AssetsByCategory"] },
  { key: "HR", icon: "users", pages: ["Employees", "HrLists", "Attendances", "EmployeeAllowances", "EmployeeDeductions", "EmployeeAdvances", "Payroll", "Payslip", "PayrollSummary", "AttendanceSummary", "AdvancesReport"] },
  { key: "Reports", icon: "bar-chart", pages: ["ExecutiveAnalytics", "SalesAnalytics", "PurchaseAnalytics", "InventoryAnalytics", "LiquidityAnalytics", "PeopleAnalytics", "ProductionAnalytics"] },
  { key: "Settings", icon: "settings", pages: ["Settings", "Branches", "AlertRules", "ApiKeys", "Users", "Roles", "RolePermissions", "UserPermissions"] },
  { key: "Builder", icon: "wrench", pages: ["BuilderSections", "BuilderModules", "BuilderColumns", "BuilderActions", "BuilderFilters", "BuilderExport"] },
];

export const sectionOf = (page: string) => sections.find((s) => s.pages.includes(page));
