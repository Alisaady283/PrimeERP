import type { DashboardSpec, DocumentSpec, ListSpec, PageSpec, PullSpec, ReportSpec, Source } from "@/features/workspace/spec";
import {
  active, asOf, col, cycleLines, date, enumOf, field, flowState, money, notes, notesLine, num, pick, product, range, stockLines, tradeLines, warehouse,
} from "@/catalog/fields";

const list = (spec: Omit<ListSpec, "type">): ListSpec => ({ type: "list", ...spec });
const doc = (spec: Omit<DocumentSpec, "type">): DocumentSpec => ({ type: "document", ...spec });
const report = (key: string, params: ReportSpec["params"], columns: ReportSpec["columns"], fullCycleOnly = false): ReportSpec =>
  ({ type: "report", key, title: `Str.Module.${key}`, permission: "Reports", params, columns, fullCycleOnly });
const custom = (key: string, permission: string, screen: string, title = `Str.Module.${key}`): PageSpec =>
  ({ type: "custom", key, title, permission, screen });

const nameList = (key: string, extra: ListSpec["columns"] = [], form: ListSpec["form"] = []) =>
  list({ key, title: `Str.Module.${key}`, permission: key, columns: [col("name", "Str.Name"), ...extra, col("isActive", "Str.Active", "bool")], form: [field("name", "Str.Name", "text", { required: true, max: 200 }), ...form, active] });

const party = (key: "Customers" | "Suppliers") =>
  list({
    key, title: `Str.Module.${key}`, permission: key, formTitles: [`Str.${key}.Add`, `Str.${key}.Edit`],
    columns: [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), col("categoryName", "Str.Category"), col("phone", "Str.Phone"),
      money("creditLimit", "Str.CreditLimit", key === "Customers" ? { permission: "Customers.Column.CreditLimit" } : {}), money("balance", "Str.Balance", { total: true }), col("isActive", "Str.Active", "bool")],
    form: [field("name", "Str.Name", "text", { required: true, max: 200, span: 2 }), field("phone", "Str.Phone", "text", { max: 30 }), field("email", "Str.Email", "text", { max: 150 }),
      num("creditLimit", "Str.CreditLimit", { min: 0 }), pick("categoryId", "Str.Category", { categories: key }), notes, active],
  });

const pulls: Record<string, PullSpec[]> = {
  PurchaseOrder: [{ source: "PurchaseRequest", permission: "Purchases.Create", match: { field: "PartyId", header: "partyId" } }],
  SalesOrder: [{ source: "Quotation", permission: "Sales.Create", match: { field: "PartyId", header: "partyId" } }],
  PurchaseInvoices: [{ source: "StockIn", permission: "Purchases.Create", fullCycleOnly: true }],
  SalesInvoices: [{ source: "StockOut", permission: "Sales.Create", fullCycleOnly: true }],
  PurchaseReturns: [{ source: "PurchaseInvoices", permission: "Purchases.Create", match: { field: "SupplierId", header: "supplierId" } }],
  SalesReturns: [{ source: "SalesInvoices", permission: "Sales.Create", match: { field: "CustomerId", header: "customerId" } }],
  StockIn: [{ source: "PurchaseOrder", permission: "Inventory.Create", fullCycleOnly: true }, { source: "SalesReturns", permission: "Inventory.Create", fullCycleOnly: true }],
  StockOut: [{ source: "SalesOrder", permission: "Inventory.Create", fullCycleOnly: true }, { source: "PurchaseReturns", permission: "Inventory.Create", fullCycleOnly: true }],
};

const cycle = (key: string, partyPage: "Customers" | "Suppliers", partyLabel: string, permission: string): DocumentSpec =>
  doc({
    key, title: `Str.Module.${key}`, permission, fullCycleOnly: true, pull: pulls[key],
    columns: [col("docNo", "Str.DocNo", "code"), col("docDate", "Str.InvoiceDate", "date"), col("partyName", partyLabel), money("totalQty", "Str.Qty"), money("total", "Str.Total", { total: true }), col("notes", "Str.Notes")],
    header: [date("docDate", "Str.InvoiceDate", { required: true }), pick("partyId", partyLabel, { page: partyPage }), field("notes", "Str.Notes", "text", { span: 2 })],
    lines: cycleLines,
  });

/** مرتجع نقطة البيع: مستند مرتجع البيع نفسه بمخزن الوردية، يسحب بسياسة المرتجع (PosReturn) ويُحفظ عبر الوردية */
const posReturn = (): DocumentSpec => {
  const base = trade("SalesReturns", "return", "Customers", "Sales");
  return { ...base, key: "PosReturn", title: "Str.Pos.ReturnTab", permission: "Pos", saveTo: "/pos/return",
    header: base.header.filter((f) => f.key !== "warehouseId"),
    pull: (pulls.SalesReturns ?? []).map((p) => ({ ...p, permission: "Pos.View", scope: "PosReturn" })) };
};

const trade = (key: string, kind: "invoice" | "return", partyPage: "Customers" | "Suppliers", permission: string): DocumentSpec => {
  const sales = partyPage === "Customers";
  const no = kind === "invoice" ? "invoiceNo" : "returnNo";
  const at = kind === "invoice" ? "invoiceDate" : "returnDate";
  const partyLabel = sales ? "Str.Customer" : "Str.Supplier";
  return doc({
    key, title: `Str.Module.${key}`, permission, pull: pulls[key],
    columns: [col(no, "Str.InvoiceNo", "code"), col(at, "Str.InvoiceDate", "date"), col(sales ? "customerName" : "supplierName", partyLabel, "text", { width: "minmax(14rem,2fr)" }),
      money("netTotal", "Str.NetTotal", { total: true }), ...(kind === "invoice" ? [money("paidAmount", "Str.Aging.Paid"), money("remainingAmount", "Str.Aging.Remaining", { total: true }), col("statusText", "Str.Status", "status")] : [])],
    header: [date(at, "Str.InvoiceDate", { required: true }), pick(sales ? "customerId" : "supplierId", partyLabel, { page: partyPage }, { required: true }), warehouse(), field("notes", "Str.Notes")],
    lines: tradeLines,
  });
};

const stock = (key: string): DocumentSpec =>
  doc({
    key, title: `Str.Module.${key}`, permission: "Inventory", pull: pulls[key],
    signatures: key === "StockIn" ? ["Str.Print.Storekeeper", "Str.Print.Approval"] : ["Str.Print.Receiver", "Str.Print.Storekeeper", "Str.Print.Approval"],
    columns: [col("docNo", "Str.DocNo", "code"), col("movementDate", "Str.InvoiceDate", "date"), col("warehouseName", "Str.Warehouse"), money("totalQty", "Str.Qty")],
    header: [date("movementDate", "Str.InvoiceDate", { required: true }), warehouse(), field("notes", "Str.Notes", "text", { span: 2 })],
    lines: stockLines,
  });

const journal = (key: string, permission: string, source = false, extra: Partial<DocumentSpec> = {}): DocumentSpec =>
  doc({
    key, title: `Str.Module.${key}`, permission, ...extra,
    columns: [col("entryNo", "Str.EntryNo", "code"), col("entryDate", "Str.EntryDate", "date"), ...(source ? [col("sourceText", "Str.Type")] : []),
      col("description", "Str.Description", "text", { width: "minmax(16rem,2fr)" }), money("totalDebit", "Str.Debit"), money("totalCredit", "Str.Credit"), col("statusText", "Str.Status", "status")],
    header: [date("entryDate", "Str.EntryDate", { required: true }), field("description", "Str.Description", "text", { span: 2, max: 500 })],
    lines: { key: "lines", balance: ["debit", "credit"], fields: [pick("accountCode", "Str.Account", { accounts: true }, { width: "minmax(18rem,2fr)" }), num("debit", "Str.Debit", { total: true }), num("credit", "Str.Credit", { total: true }), notesLine] },
  });

const voucher = (key: "Receipts" | "Payments"): DocumentSpec =>
  doc({
    key, title: `Str.Module.${key}`, permission: key,
    signatures: [...(key === "Payments" ? ["Str.Print.Receiver"] : []), "Str.Print.Accountant", "Str.Print.Approval"],
    columns: [col("voucherNo", "Str.Voucher.No", "code"), col("voucherDate", "Str.Date", "date"), col("partyName", "Str.Party", "text", { width: "minmax(14rem,2fr)" }), col("treasuryName", "Str.Treasury"),
      money("amount", "Str.Amount", { total: true }), col("methodName", "Str.Voucher.Method")],
    header: [
      date("voucherDate", "Str.Date", { required: true }),
      pick("partyId", "Str.Party", { page: key === "Receipts" ? "Customers" : "Suppliers" }),
      pick("method", "Str.Voucher.PaymentMethod", enumOf("Str.Voucher.Method", [[1, "Cash"], [2, "Bank"]]), { required: true, defaultValue: 1 }),
      pick("treasuryId", "Str.Treasury", { page: "Treasuries" }, { required: true }),
      num("amount", "Str.Amount", { required: true, min: 0 }),
      field("reference", "Str.Voucher.Reference"),
      field("notes", "Str.Notes", "text", { span: 2 }),
    ],
    lines: { key: "allocations", fields: [field("invoiceNo", "Str.InvoiceNo"), num("amount", "Str.Amount", { total: true }), notesLine] },
  });

const chequeDoc = (key: "ChequeReceipts" | "ChequeIssues"): DocumentSpec =>
  doc({
    key, title: `Str.Module.${key}`, permission: "Cheques",
    columns: [col("chequeNo", "Str.Field.ChequeNo", "code"), col("bankName", "Str.Cheque.Bank"), col("partyName", "Str.Party"), money("amount", "Str.Amount", { total: true }), col("dueDate", "Str.Cheque.DueDate", "date"), col("statusName", "Str.Status", "status")],
    header: [date("docDate", "Str.Date", { required: true }), field("notes", "Str.Notes", "text", { span: 2 })],
    lines: {
      key: "lines",
      fields: [field("chequeNo", "Str.Field.ChequeNo"), field("bankName", "Str.Cheque.BankName"), num("amount", "Str.Amount", { total: true }),
        pick("partyId", "Str.Party", { page: key === "ChequeReceipts" ? "Customers" : "Suppliers" }), date("dueDate", "Str.Cheque.DueDate"), notesLine],
    },
  });

const movement = (key: string, typePage: string | null, withTreasury = false): DocumentSpec =>
  doc({
    key, title: `Str.Module.${key}`, permission: "HR",
    filters: [pick("employeeId", "Str.Employee", { page: "Employees" })],
    columns: [col("employeeName", "Str.Employee", "text", { width: "minmax(14rem,2fr)" }), col("date", "Str.Date", "date"), col("typeName", "Str.Type"), col("reason", "Str.Description"), money("amount", "Str.Amount", { total: true })],
    header: [
      pick("employeeCode", "Str.Employee", { page: "Employees", by: "code" }, { required: true }),
      date("date", "Str.Date", { required: true }),
      ...(typePage ? [pick("typeId", "Str.Type", { page: typePage }, { required: true })] : []),
      num("amount", "Str.Amount", { required: true, min: 0 }),
      ...(withTreasury ? [pick("treasuryId", "Str.Treasury", { page: "Treasuries" })] : []),
      field("reason", "Str.Description"),
      field("notes", "Str.Notes", "text", { span: 2 }),
    ],
  });

const bomKinds = enumOf("Str.Bom.Kind", [[1, "Component"], [2, "ByProduct"], [3, "Operation"]]);

const production = (key: "ProductionOrders" | "Unbuild"): DocumentSpec =>
  doc({
    key, title: `Str.Module.${key}`, permission: "Manufacturing", noUpdate: true,
    columns: [col("orderNo", "Str.DocNo", "code"), col("orderDate", "Str.InvoiceDate", "date"), col("productName", "Str.Bom.Product", "text", { width: "minmax(14rem,2fr)" }), money("qty", "Str.Qty"),
      money("materialsCost", "Str.Production.Materials"), money("operationsCost", "Str.Production.Operations"), money("totalCost", "Str.Production.Total", { total: true })],
    open: { path: "/production/open", params: [pick("bomId", "Str.Production.Bom", { page: "BillsOfMaterials" }, { required: true }), num("qty", "Str.Qty", { defaultValue: 1 })] },
    header: [date("orderDate", "Str.InvoiceDate", { required: true }), warehouse("rawWarehouseId", "Str.Production.RawWarehouse"), warehouse("finishedWarehouseId", "Str.Production.FinishedWarehouse"),
      field("batchNo", "Str.Batch"), field("expiryDate", "Str.ExpiryDate", "date"), field("notes", "Str.Notes", "text", { span: 2 })],
    lines: { key: "lines", fields: [pick("kind", "Str.Bom.Line", bomKinds, { width: "9rem" }), product(), pick("workCenterId", "Str.Module.WorkCenters", { page: "WorkCenters" }), num("qty", "Str.Qty"), field("batchNo", "Str.Batch"), notesLine] },
  });

const dashboard = (key: string, cards: DashboardSpec["cards"], charts: DashboardSpec["charts"]): DashboardSpec =>
  ({ type: "dashboard", key, title: `Str.Module.${key}`, permission: key, cards, charts });

const tradeDashboard = (key: string, value?: string, balance?: string, returns?: string, flow?: string, aging?: string) =>
  dashboard(key,
    [{ key: "Net", drill: value }, { key: "Count", kind: "count" }, { key: "Average" }, { key: "ReturnRate", kind: "percent", drill: returns, lowerIsBetter: true },
      { key: "Outstanding", drill: aging, lowerIsBetter: true }, { key: "Flow", kind: "percent", drill: flow, fullCycleOnly: true }, { key: "NewParties", kind: "count" },
      { key: "ActiveParties", kind: "count" }, { key: "LostParties", kind: "count", lowerIsBetter: true }],
    [{ key: "Trend", kind: "Area", span: 2, drill: value, smooth: true }, { key: "CollectionRate", kind: "Gauge", drill: aging }, { key: "PartyFlow", kind: "Column", span: 2 },
      { key: "PartySegments", kind: "Donut", tone: "Severity" }, { key: "TopParties", kind: "Bar", drill: balance }, { key: "TopProducts", kind: "Bar" }, { key: "ByCategory", kind: "Donut" },
      { key: "Aging", kind: "Column", span: 3, drill: aging, tone: "Severity" }]);

const flowColumns = (party: string, done: string, last?: string) => [
  col("docNo", "Str.DocNo", "code"), col("date", "Str.Date", "date"), col("party", party), col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }),
  money("qty", "Str.Qty"), money("done", done), ...(last ? [money("closed", last)] : []), money("remaining", "Str.Flow.Remaining"), col("status", "Str.Status", "status"),
];
const flowHead = (party: string) => [col("docNo", "Str.DocNo", "code"), col("date", "Str.Date", "date"), col("party", party), col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), money("qty", "Str.Qty")];
const statement = [col("date", "Str.Date", "date"), col("entryNo", "Str.EntryNo", "code"), col("description", "Str.Description", "text", { width: "minmax(16rem,2fr)" }), money("debit", "Str.Debit"), money("credit", "Str.Credit"), money("runningBalance", "Str.RunningBalance")];
const partyBalance = (charge: string, settle: string) => [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), money("opening", "Str.Reports.OpeningBalance"), money("charged", charge), money("settled", settle), money("closing", "Str.Reports.ClosingBalance")];
const statementLines = [col("statement", "Str.Description", "text", { width: "minmax(20rem,3fr)" }), money("partial", "Str.Reports.Partial"), money("total", "Str.Reports.Whole")];
const aging = (party: string) => [col("code", "Str.Code", "code"), col("name", party, "text", { width: "minmax(14rem,2fr)" }),
  ...["NotDue", "Days30", "Days60", "Days90", "Days120", "Days150", "Over150"].map((k) => money(k.charAt(0).toLowerCase() + k.slice(1), `Str.Analytics.Aging.${k}`)), money("total", "Str.Aging.Balance")];
const settlement = (party: string) => [col("invoiceNo", "Str.InvoiceNo", "code"), col("date", "Str.Date", "date"), col("dueDate", "Str.Aging.DueDate", "date"), col("party", party, "text", { width: "minmax(12rem,2fr)" }),
  money("net", "Str.NetTotal"), money("paid", "Str.Aging.Paid"), money("remaining", "Str.Aging.Remaining"), col("lastPayment", "Str.Aging.LastPayment", "date"), col("payments", "Str.Aging.Payments", "count"), col("status", "Str.Status", "status")];
const projectStatus = enumOf("Str.Project.Status", [[1, "Planned"], [2, "Active"], [3, "OnHold"], [4, "Completed"], [5, "Cancelled"]]);
const taskStatus = enumOf("Str.ProjectTask.Status", [[1, "Todo"], [2, "InProgress"], [3, "Done"], [4, "Blocked"]]);
const quantityCost = (qty: string) => [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), money("qty", qty), money("cost", "Str.Reports.Cost")];

const asset = { page: "Assets" } as const;

export const pages: PageSpec[] = [
  custom("Accounts", "Accounts", "Accounts"),
  journal("Journals", "Journal"),
  journal("OpeningBalances", "Journal", true, {
    kindEditors: [{ property: "source", value: "OpeningStock", page: "OpeningStock" }],
    pageActions: [{ key: "openingStock", label: "Str.Module.OpeningStock", permission: "Inventory.OpeningStock", run: "openingStock" }],
  }),
  report("TrialBalance", range, [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(16rem,2fr)" }),
    money("openingDebit", "Str.Debit", { group: "Str.Reports.OpeningGroup" }), money("openingCredit", "Str.Credit", { group: "Str.Reports.OpeningGroup" }),
    money("periodDebit", "Str.Debit", { group: "Str.Reports.PeriodGroup" }), money("periodCredit", "Str.Credit", { group: "Str.Reports.PeriodGroup" }),
    money("closingDebit", "Str.Debit", { group: "Str.Reports.ClosingGroup" }), money("closingCredit", "Str.Credit", { group: "Str.Reports.ClosingGroup" })]),
  report("AccountStatement", [pick("accountId", "Str.Account", { accounts: true, by: "id" }, { required: true }), ...range], statement),
  report("IncomeStatement", range, statementLines),
  report("BalanceSheet", asOf, statementLines),
  report("CashFlow", range, statementLines),

  cycle("Quotation", "Customers", "Str.Customer", "Sales"),
  cycle("SalesOrder", "Customers", "Str.Customer", "Sales"),
  { ...trade("SalesInvoices", "invoice", "Customers", "Sales"), recordActions: [{ key: "pay", label: "Str.Payment.Request", action: "Create", permission: "Receipts.Create", run: "paymentLink" }, { key: "einvoice", label: "Str.EInvoice.Submit", action: "Edit", run: "submitEInvoice" }] },
  trade("SalesReturns", "return", "Customers", "Sales"),
  posReturn(),
  party("Customers"),
  report("CustomerBalances", range, partyBalance("Str.Reports.Sales", "Str.Reports.Receipts")),
  report("CustomerStatement", [pick("customerId", "Str.Customer", { page: "Customers" }, { required: true }), ...range], statement),
  report("SalesReport", range, [col("invoiceNo", "Str.InvoiceNo", "code"), col("date", "Str.Date", "date"), col("partyName", "Str.Customer", "text", { width: "minmax(14rem,2fr)" }), money("netTotal", "Str.NetTotal")]),
  report("QuotationFollowUp", [...range, flowState], flowColumns("Str.Customer", "Str.Flow.Converted"), true),
  report("SalesOrderFollowUp", [...range, flowState], flowColumns("Str.Customer", "Str.Flow.Delivered", "Str.Flow.Invoiced"), true),
  report("SalesReturnsByInvoice", [...range, flowState], flowColumns("Str.Customer", "Str.Flow.Returned")),
  report("SalesReturnsUnlinked", range, flowHead("Str.Customer")),
  report("CustomerAging", asOf, aging("Str.Customer")),
  report("CustomerInvoiceSettlement", range, settlement("Str.Customer")),

  custom("Pos", "Pos", "Pos"),
  list({ key: "PosShifts", title: "Str.Module.PosShifts", permission: "Pos", readOnly: true,
    columns: [col("openedAt", "Str.Pos.OpenedAt", "date"), col("closedAt", "Str.Pos.ClosedAt", "date"), col("userName", "Str.Pos.Cashier"), money("openingCash", "Str.Pos.Opening"),
      money("cashIn", "Str.Pos.CashIn"), money("cashOut", "Str.Pos.CashOut"), money("countedCash", "Str.Pos.Counted"), money("difference", "Str.Pos.Difference"),
      col("salesCount", "Str.Pos.Sales", "count"), col("returnsCount", "Str.Pos.Returns", "count")] }),
  report("CashierReport", range, [col("cashier", "Str.Pos.Cashier", "text", { width: "minmax(12rem,2fr)" }), col("shifts", "Str.Reports.Shifts", "count"), col("sales", "Str.Pos.Sales", "count"),
    col("returns", "Str.Pos.Returns", "count"), money("collected", "Str.Pos.CashIn"), money("refunded", "Str.Pos.CashOut"), money("difference", "Str.Pos.Difference")]),

  cycle("PurchaseRequest", "Suppliers", "Str.Supplier", "Purchases"),
  cycle("PurchaseOrder", "Suppliers", "Str.Supplier", "Purchases"),
  trade("PurchaseInvoices", "invoice", "Suppliers", "Purchases"),
  trade("PurchaseReturns", "return", "Suppliers", "Purchases"),
  party("Suppliers"),
  report("SupplierBalances", range, partyBalance("Str.Reports.Purchases", "Str.Reports.Payments")),
  report("SupplierStatement", [pick("supplierId", "Str.Supplier", { page: "Suppliers" }, { required: true }), ...range], statement),
  report("PurchaseRequestFollowUp", [...range, flowState], flowColumns("Str.Supplier", "Str.Flow.Ordered"), true),
  report("PurchaseOrderFollowUp", [...range, flowState], flowColumns("Str.Supplier", "Str.Flow.Received", "Str.Flow.Invoiced"), true),
  report("PurchaseReturnsByInvoice", [...range, flowState], flowColumns("Str.Supplier", "Str.Flow.Returned")),
  report("PurchaseReturnsUnlinked", range, flowHead("Str.Supplier")),
  report("SupplierAging", asOf, aging("Str.Supplier")),
  report("SupplierInvoiceSettlement", range, settlement("Str.Supplier")),

  list({ key: "Products", title: "Str.Module.Products", permission: "Products", formTitles: ["Str.Products.Add", "Str.Products.Edit"],
    filters: [pick("categoryId", "Str.Category", { categories: "Products" })],
    gallery: { image: "imageData", title: "name", hint: "code", value: "salePrice" },
    columns: [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(16rem,2fr)" }), col("categoryName", "Str.Category"),
      money("costPrice", "Str.CostPrice", { permission: "Products.Column.CostPrice" }), money("salePrice", "Str.SalePrice"), col("isActive", "Str.Active", "bool")],
    form: [field("name", "Str.Name", "text", { required: true, max: 200, span: 2 }), pick("categoryId", "Str.Category", { categories: "Products" }), pick("brandId", "Str.Brand", { categories: "Brands" }),
      field("barcode", "Str.Barcode", "text", { max: 64 }), num("costPrice", "Str.CostPrice", { min: 0 }), num("salePrice", "Str.SalePrice", { min: 0 }),
      field("hasExpiry", "Str.Product.HasExpiry", "switch"), field("imageData", "Str.Product.Image", "image", { span: 2 }), field("description", "Str.Product.Description", "textarea", { max: 1000 }), notes, active] }),
  custom("Categories", "Categories", "Categories"),
  custom("Brands", "Brands", "Categories"),
  nameList("Units", [col("symbol", "Str.Symbol")], [field("symbol", "Str.Symbol", "text", { max: 20 })]),
  list({ key: "Warehouses", title: "Str.Module.Warehouses", permission: "Warehouses",
    columns: [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), col("location", "Str.Location"), col("isActive", "Str.Active", "bool")],
    form: [field("name", "Str.Name", "text", { required: true, max: 200 }), field("location", "Str.Location"), field("managerName", "Str.ManagerName"), pick("branchId", "Str.Branch", { page: "Branches" }), active] }),
  stock("StockIn"),
  stock("StockOut"),
  doc({ key: "StockTransfer", title: "Str.Module.StockTransfer", permission: "Inventory",
    columns: [col("docNo", "Str.DocNo", "code"), col("movementDate", "Str.InvoiceDate", "date"), col("fromWarehouseName", "Str.FromWarehouse"), col("toWarehouseName", "Str.ToWarehouse")],
    header: [date("movementDate", "Str.InvoiceDate", { required: true }), warehouse("fromWarehouseId", "Str.FromWarehouse"), warehouse("toWarehouseId", "Str.ToWarehouse"), field("notes", "Str.Notes")],
    lines: { key: "lines", fields: [product(), num("qty", "Str.Qty", { defaultValue: 1 }), field("batchNo", "Str.Batch"), notesLine] } }),
  report("StockBalances", range, [col("productCode", "Str.Code", "code"), col("productName", "Str.Product", "text", { width: "minmax(14rem,2fr)" }), col("warehouseName", "Str.Warehouse"),
    money("opening", "Str.Reports.OpeningBalance"), money("in", "Str.Reports.In"), money("out", "Str.Reports.Out"), money("closing", "Str.Reports.ClosingBalance")]),
  report("ItemCard", [pick("productId", "Str.Product", { page: "Products" }, { required: true })], [col("date", "Str.Date", "date"), col("sourceDoc", "Str.SourceDoc"),
    money("inQty", "Str.Reports.InQty"), money("inPrice", "Str.Reports.InPrice"), money("inValue", "Str.Reports.InValue"), money("outQty", "Str.Reports.OutQty"), money("outPrice", "Str.Reports.OutPrice"),
    money("outValue", "Str.Reports.OutValue"), money("balanceQty", "Str.Reports.BalanceQty"), money("balancePrice", "Str.Reports.BalancePrice"), money("balanceValue", "Str.Reports.BalanceValue")]),
  report("StockReport", [...range, pick("warehouseId", "Str.Warehouse", { page: "Warehouses" })], [col("date", "Str.Date", "date"), col("productName", "Str.Product", "text", { width: "minmax(14rem,2fr)" }),
    col("warehouseName", "Str.Warehouse"), col("movementType", "Str.MovementType"), money("qty", "Str.Qty"), money("balanceAfter", "Str.RunningBalance")]),
  ...(["BatchReport", "ExpiryReport"] as const).map((key) => report(key, [], [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), col("warehouse", "Str.Warehouse"),
    col("batch", "Str.Batch"), col("expiry", "Str.ExpiryDate", "date"), money("qty", "Str.Qty"), col("state", "Str.Status", "status")])),
  report("SalesReturnsPending", [...range, flowState], flowColumns("Str.Customer", "Str.Flow.Received"), true),
  report("PurchaseReturnsPending", [...range, flowState], flowColumns("Str.Supplier", "Str.Flow.Issued"), true),

  doc({ key: "BillsOfMaterials", title: "Str.Module.BillsOfMaterials", permission: "Manufacturing",
    columns: [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), col("productName", "Str.Bom.Product"), money("qty", "Str.Qty"), col("isActive", "Str.Active", "bool")],
    header: [field("name", "Str.Name", "text", { required: true, max: 200 }), product("productCode", "Str.Bom.Product"), num("qty", "Str.Bom.Qty", { defaultValue: 1 }), field("isActive", "Str.Active", "switch", { defaultValue: true }), notes],
    lines: { key: "lines", fields: [pick("kind", "Str.Bom.Line", bomKinds, { width: "9rem", defaultValue: 1 }), product(), pick("workCenterId", "Str.Module.WorkCenters", { page: "WorkCenters" }), num("qty", "Str.Qty", { defaultValue: 1 }), num("wastePercent", "Str.Bom.Waste"), notesLine] } }),
  production("ProductionOrders"),
  production("Unbuild"),
  stock("Scrap"),
  list({ key: "WorkCenters", title: "Str.Module.WorkCenters", permission: "Manufacturing",
    columns: [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), money("costPerHour", "Str.WorkCenter.CostPerHour"), col("isActive", "Str.Active", "bool")],
    form: [field("name", "Str.Name", "text", { required: true, max: 200 }), num("costPerHour", "Str.WorkCenter.CostPerHour", { min: 0 }), active] }),
  report("ProductionCostReport", range, [col("orderNo", "Str.DocNo", "code"), col("date", "Str.InvoiceDate", "date"), col("product", "Str.Bom.Product", "text", { width: "minmax(14rem,2fr)" }), money("qty", "Str.Qty"),
    money("materials", "Str.Production.Materials"), money("operations", "Str.Production.Operations"), money("total", "Str.Production.Total"), money("unitCost", "Str.UnitCost")]),
  report("ConsumptionReport", range, quantityCost("Str.Qty")),
  report("WorkCenterLoadReport", range, quantityCost("Str.Reports.Minutes")),
  report("ScrapReport", range, quantityCost("Str.Qty")),

  // المشاريع: المشروع ومهامه وساعاته ومستخلصاته — الساعات والمستخلصات تُقيَّد عند حفظها
  list({ key: "Projects", title: "Str.Module.Projects", permission: "Projects", formTitles: ["Str.Projects.Add", "Str.Projects.Edit"],
    columns: [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), col("customerName", "Str.Project.Customer"), col("managerName", "Str.Project.Manager"),
      col("startDate", "Str.Project.StartDate", "date"), col("endDate", "Str.Project.EndDate", "date"), money("contractValue", "Str.Project.ContractValue", { total: true }), money("budget", "Str.Project.Budget", { total: true }),
      col("statusText", "Str.Project.Status", "status")],
    form: [field("name", "Str.Name", "text", { required: true, max: 200 }), pick("customerId", "Str.Project.Customer", { page: "Customers" }), pick("managerId", "Str.Project.Manager", { page: "Employees" }),
      pick("status", "Str.Project.Status", projectStatus, { required: true, defaultValue: 1 }), field("startDate", "Str.Project.StartDate", "date"), field("endDate", "Str.Project.EndDate", "date"),
      num("contractValue", "Str.Project.ContractValue", { min: 0 }), num("budget", "Str.Project.Budget", { min: 0 }), num("retentionPercent", "Str.Project.RetentionPercent", { min: 0 }), notes, active] }),
  list({ key: "ProjectTasks", title: "Str.Module.ProjectTasks", permission: "Projects", formTitles: ["Str.ProjectTasks.Add", "Str.ProjectTasks.Edit"],
    columns: [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), col("projectName", "Str.Project"), col("employeeName", "Str.ProjectTask.Assignee"),
      col("dueDate", "Str.ProjectTask.DueDate", "date"), col("progress", "Str.ProjectTask.Progress", "percent"), col("statusText", "Str.ProjectTask.Status", "status"), col("isMilestone", "Str.ProjectTask.IsMilestone", "bool")],
    form: [pick("projectId", "Str.Project", { page: "Projects" }, { required: true }), field("name", "Str.Name", "text", { required: true, max: 200 }), pick("employeeId", "Str.ProjectTask.Assignee", { page: "Employees" }),
      pick("status", "Str.ProjectTask.Status", taskStatus, { required: true, defaultValue: 1 }), field("startDate", "Str.Project.StartDate", "date"), field("dueDate", "Str.ProjectTask.DueDate", "date"),
      num("weight", "Str.ProjectTask.Weight", { min: 0, defaultValue: 1 }), num("progress", "Str.ProjectTask.Progress", { min: 0, max: 100 }), field("isMilestone", "Str.ProjectTask.IsMilestone", "switch"),
      num("billingPercent", "Str.ProjectTask.BillingPercent", { min: 0, max: 100 }), notes, active] }),
  list({ key: "ProjectTimesheets", title: "Str.Module.ProjectTimesheets", permission: "Projects", formTitles: ["Str.ProjectTimesheets.Add", "Str.ProjectTimesheets.Edit"],
    columns: [col("code", "Str.Code", "code"), col("date", "Str.Date", "date"), col("projectName", "Str.Project", "text", { width: "minmax(12rem,2fr)" }), col("taskName", "Str.ProjectTask"), col("employeeName", "Str.Employee"),
      col("hours", "Str.ProjectTimesheet.Hours", "number", { total: true }), money("hourlyCost", "Str.ProjectTimesheet.HourlyCost"), money("cost", "Str.ProjectTimesheet.Cost", { total: true })],
    form: [pick("projectId", "Str.Project", { page: "Projects" }, { required: true }), pick("taskId", "Str.ProjectTask", { page: "ProjectTasks" }), pick("employeeId", "Str.Employee", { page: "Employees" }, { required: true }),
      date("date", "Str.Date", { required: true }), num("hours", "Str.ProjectTimesheet.Hours", { required: true, min: 0 }), num("hourlyCost", "Str.ProjectTimesheet.HourlyCost", { min: 0 }), notes] }),
  list({ key: "ProjectBills", title: "Str.Module.ProjectBills", permission: "Projects", formTitles: ["Str.ProjectBills.Add", "Str.ProjectBills.Edit"],
    columns: [col("code", "Str.Code", "code"), col("date", "Str.Date", "date"), col("projectName", "Str.Project", "text", { width: "minmax(12rem,2fr)" }), col("taskName", "Str.ProjectTask"),
      col("percent", "Str.ProjectBill.Percent", "percent"), money("amount", "Str.ProjectBill.Amount", { total: true }), money("retention", "Str.ProjectBill.Retention", { total: true }), money("net", "Str.ProjectBill.Net", { total: true })],
    form: [pick("projectId", "Str.Project", { page: "Projects" }, { required: true }), pick("taskId", "Str.ProjectTask", { page: "ProjectTasks" }), date("date", "Str.Date", { required: true }),
      num("percent", "Str.ProjectBill.Percent", { min: 0, max: 100 }), num("amount", "Str.ProjectBill.Amount", { min: 0 }), notes] }),
  report("ProjectProfitReport", range, [col("code", "Str.Code", "code"), col("name", "Str.Project", "text", { width: "minmax(14rem,2fr)" }), col("customer", "Str.Project.Customer"),
    money("contract", "Str.Project.ContractValue"), money("billed", "Str.Project.Billed"), money("retention", "Str.ProjectBill.Retention"), money("cost", "Str.Project.Cost"), money("profit", "Str.Project.Profit"),
    col("margin", "Str.Project.Margin", "percent"), col("progress", "Str.Project.Progress", "percent"), col("budgetUsed", "Str.Project.BudgetUsed", "percent"), col("hours", "Str.Project.Hours", "number")]),

  list({ key: "Treasuries", title: "Str.Module.Treasuries", permission: "Treasuries",
    columns: [col("code", "Str.Code", "code"), col("accountCode", "Str.Treasury.AccountCode", "code"), col("name", "Str.Treasury.FullName", "text", { width: "minmax(16rem,2fr)" }), money("accountBalance", "Str.Balance", { total: true }), col("kindName", "Str.Type")],
    form: [pick("kind", "Str.Type", { enumOf: [[1, "Str.Treasury.Kind.Fund"], [2, "Str.Treasury.Kind.Bank"]] }, { required: true, defaultValue: 1 }), field("name", "Str.Treasury.FullName", "text", { required: true, max: 200 }),
      field("accountNumber", "Str.Treasury.AccountNumber"), notes, active] }),
  voucher("Receipts"),
  voucher("Payments"),
  chequeDoc("ChequeReceipts"),
  chequeDoc("ChequeIssues"),
  custom("Cheques", "Cheques", "Cheques"),

  list({ key: "Assets", title: "Str.Module.Assets", permission: "Assets",
    filters: [pick("categoryId", "Str.Category", { categories: "AssetCategories" })],
    columns: [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), col("categoryName", "Str.Category"), col("purchaseDate", "Str.PurchaseDate", "date"),
      money("purchaseCost", "Str.Asset.Cost", { total: true }), money("currentValue", "Str.Asset.BookValue", { total: true })],
    form: [field("name", "Str.Name", "text", { required: true, max: 200, span: 2 }), pick("categoryId", "Str.Category", { categories: "AssetCategories" }, { required: true }),
      date("purchaseDate", "Str.PurchaseDate", { required: true }), num("purchaseCost", "Str.PurchaseCost", { required: true, min: 0 }),
      pick("acquisitionMethod", "Str.Asset.Acquisition", enumOf("Str.Asset", [[1, "Cash"], [2, "Bank"], [3, "Supplier"]]), { required: true, defaultValue: 1 }),
      pick("fundingId", "Str.Asset.Funding", { page: "Treasuries" }, { required: true, dependsOn: { field: "acquisitionMethod", sources: { "1": { page: "Treasuries" }, "2": { page: "Treasuries" }, "3": { page: "Suppliers" } } } }),
      num("usefulLifeYears", "Str.Asset.Life", { min: 0 }), num("salvageValue", "Str.Asset.Salvage", { min: 0 }), field("location", "Str.Location"), notes] }),
  custom("AssetCategories", "AssetCategories", "Categories"),
  list({ key: "AssetRevaluations", title: "Str.Module.AssetRevaluations", permission: "Assets", filters: [pick("assetId", "Str.Assets", asset)],
    columns: [col("revaluationDate", "Str.Date", "date"), col("assetCode", "Str.Code", "code"), col("assetName", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), money("oldValue", "Str.Asset.OldValue"),
      money("newValue", "Str.Asset.NewValue"), money("difference", "Str.Asset.Difference", { total: true }), col("kindName", "Str.Asset.RevaluationKind")],
    form: [pick("assetId", "Str.Assets", asset, { required: true, span: 2 }), date("revaluationDate", "Str.Date", { required: true }), num("newValue", "Str.Asset.NewValue", { required: true, min: 0 }),
      num("salvageValue", "Str.Asset.Salvage", { min: 0 }), num("usefulLifeYears", "Str.Asset.RemainingLife", { min: 0 }), notes] }),
  list({ key: "AssetDepreciations", title: "Str.Module.AssetDepreciations", permission: "Assets", filters: [pick("assetId", "Str.Assets", asset)],
    pageActions: [{ key: "depreciation", label: "Str.Module.AssetDepreciations", permission: "Assets.Create", run: "depreciation" }],
    columns: [col("periodDate", "Str.Date", "date"), col("assetCode", "Str.Code", "code"), col("assetName", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), col("notes", "Str.Description"), money("amount", "Str.Amount", { total: true })],
    form: [pick("assetId", "Str.Assets", asset, { required: true, span: 2 }), date("periodDate", "Str.Date", { required: true }), num("amount", "Str.Amount", { required: true, min: 0 }), notes] }),
  list({ key: "AssetDisposals", title: "Str.Module.AssetDisposals", permission: "Assets", filters: [pick("assetId", "Str.Assets", asset)],
    columns: [col("disposalDate", "Str.Date", "date"), col("assetCode", "Str.Code", "code"), col("assetName", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), col("treasuryName", "Str.Treasury"),
      money("salePrice", "Str.Asset.SalePrice"), money("bookValue", "Str.Asset.BookValue"), money("gainOrLoss", "Str.Asset.GainOrLoss", { total: true }), col("kindName", "Str.Asset.DisposalKind")],
    form: [pick("assetId", "Str.Assets", asset, { required: true, span: 2 }), date("disposalDate", "Str.Date", { required: true }),
      pick("settlement", "Str.Asset.Acquisition", enumOf("Str.Asset", [[1, "Cash"], [2, "Bank"], [4, "Account"]]), { required: true, defaultValue: 1 }),
      pick("treasuryId", "Str.Asset.Settlement", { page: "Treasuries" }, { required: true, dependsOn: { field: "settlement", sources: { "1": { page: "Treasuries" }, "2": { page: "Treasuries" }, "4": { accounts: true, by: "id" } } } }),
      num("salePrice", "Str.Asset.SalePrice", { min: 0 }), notes] }),
  report("AssetRegister", [...range, pick("categoryId", "Str.Asset.Type", { categories: "AssetCategories" })], [col("no", "Str.Serial", "count", { width: "4rem" }), col("categoryName", "Str.Asset.Type"),
    col("name", "Str.Name", "text", { width: "minmax(12rem,2fr)" }), col("supplierName", "Str.Supplier"), col("purchaseDate", "Str.Asset.PurchaseDate", "date"), money("purchaseCost", "Str.Asset.Cost"),
    money("additions", "Str.Asset.RevaluationAdditions"), money("reductions", "Str.Asset.Reductions"), col("rate", "Str.Asset.Rate", "number"), money("accumulatedStart", "Str.Asset.AccumulatedStart"),
    money("charge", "Str.Asset.Charge"), money("accumulatedEnd", "Str.Asset.AccumulatedEnd"), money("net", "Str.Asset.NetValue")]),
  report("AssetsByCategory", [], [col("name", "Str.Asset.Type", "text", { width: "minmax(16rem,3fr)" }), money("purchaseCost", "Str.Asset.Cost"), money("revalued", "Str.Asset.Revalued"),
    money("accumulated", "Str.Asset.Accumulated"), money("bookValue", "Str.Asset.BookValue")]),

  list({ key: "Employees", title: "Str.Module.Employees", permission: "HR",
    filters: [pick("departmentId", "Str.Department", { page: "Departments" })],
    columns: [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), col("departmentName", "Str.Department"), col("jobTitleName", "Str.JobTitle"), col("phone", "Str.Phone"),
      money("basicSalary", "Str.Salary", { permission: "HR.Column.Salary" })],
    form: [field("name", "Str.Name", "text", { required: true, max: 200, span: 2 }), pick("departmentId", "Str.Department", { page: "Departments" }), pick("jobTitleId", "Str.JobTitle", { page: "JobTitles" }),
      field("phone", "Str.Phone", "text", { max: 30 }), field("email", "Str.Email", "text", { max: 150 }), date("hireDate", "Str.HireDate"), num("basicSalary", "Str.Salary", { min: 0 }),
      field("workStart", "Str.Employee.WorkStart", "time"), field("workEnd", "Str.Employee.WorkEnd", "time"),
      pick("status", "Str.Status", enumOf("Str.Employee.Status", [[1, "Active"], [2, "Inactive"], [3, "OnLeave"]]), { required: true, defaultValue: 1 }), notes] }),
  custom("HrLists", "HrLists", "HrLists"),
  doc({ key: "Attendances", title: "Str.Module.Attendances", permission: "HR",
    columns: [col("date", "Str.Date", "date"), col("present", "Str.Attendance.Status.Present", "count"), col("absent", "Str.Attendance.Status.Absent", "count"), col("leave", "Str.Attendance.Status.Leave", "count"),
      col("mission", "Str.Attendance.Status.Mission", "count"), col("holiday", "Str.Attendance.Status.Holiday", "count"), col("notes", "Str.Notes")],
    open: { path: "/attendance/open", params: [date("date", "Str.Date")] },
    header: [date("date", "Str.Date", { required: true }), field("notes", "Str.Notes", "text", { span: 2 })],
    lines: { key: "lines", fields: [field("employeeName", "Str.Employee", "text", { readOnly: true, width: "minmax(14rem,2fr)" }),
      pick("status", "Str.Status", enumOf("Str.Attendance.Status", [[1, "Present"], [2, "Absent"], [3, "Leave"], [4, "Mission"], [5, "Holiday"]]), { width: "10rem" }),
      pick("leaveTypeId", "Str.LeaveType", { page: "LeaveTypes" }), field("checkIn", "Str.Attendance.CheckIn", "time", { width: "8rem" }), field("checkOut", "Str.Attendance.CheckOut", "time", { width: "8rem" }), notesLine] } }),
  movement("EmployeeAllowances", "AllowanceTypes"),
  movement("EmployeeDeductions", "DeductionTypes"),
  movement("EmployeeAdvances", null, true),
  doc({ key: "Payroll", title: "Str.Module.Payroll", permission: "HR",
    columns: [col("payrollNo", "Str.DocNo", "code"), col("month", "Str.Month", "count"), col("year", "Str.Year", "count"), col("paymentDate", "Str.PaymentDate", "date"), money("netTotal", "Str.NetTotal", { total: true }), col("statusText", "Str.Status", "status")],
    open: { path: "/payroll/open", params: [num("month", "Str.Month", { defaultValue: new Date().getMonth() + 1 }), num("year", "Str.Year", { defaultValue: new Date().getFullYear() })] },
    header: [num("month", "Str.Month", { readOnly: true }), num("year", "Str.Year", { readOnly: true }), date("paymentDate", "Str.PaymentDate", { required: true }), field("notes", "Str.Notes")],
    lines: { key: "lines", fields: [field("employeeCode", "Str.Employee", "text", { readOnly: true, width: "9rem" }), num("basicSalary", "Str.BasicSalary"), num("allowances", "Str.Allowances"), num("overtime", "Str.Payroll.Overtime"),
      num("deductions", "Str.Deductions"), num("advances", "Str.Payroll.Advances"), num("insurance", "Str.Payroll.Insurance"), num("tax", "Str.Payroll.Tax"), num("netSalary", "Str.Print.NetAmount", { readOnly: true, total: true })] } }),
  report("Payslip", [pick("employeeId", "Str.Employee", { page: "Employees" }, { required: true }), ...range], [col("payrollNo", "Str.Payroll.No", "code"), col("period", "Str.Period", "text", { width: "minmax(10rem,2fr)" }),
    money("basicSalary", "Str.Payroll.Basic"), money("allowances", "Str.Allowances"), money("overtime", "Str.Payroll.Overtime"), money("gross", "Str.Payroll.Gross"), money("deductions", "Str.Payroll.Deductions"),
    money("advances", "Str.Payroll.Advances"), money("insurance", "Str.Payroll.Insurance"), money("tax", "Str.Payroll.Tax"), money("withheld", "Str.Deductions"), money("netSalary", "Str.Payroll.Net")]),
  report("PayrollSummary", range, [col("payrollNo", "Str.Payroll.No", "code"), col("period", "Str.Period", "text", { width: "minmax(12rem,2fr)" }), money("basic", "Str.Payroll.Basic"),
    money("allowances", "Str.Reports.Allowances"), money("deductions", "Str.Reports.Deductions"), money("net", "Str.Reports.Net")]),
  report("AttendanceSummary", range, [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), col("present", "Str.Reports.Present", "count"), col("absent", "Str.Reports.Absent", "count"),
    col("leave", "Str.Reports.Leave", "count"), col("lateMinutes", "Str.Reports.LateMinutes", "count"), col("overtimeMinutes", "Str.Reports.OvertimeMinutes", "count")]),
  report("AdvancesReport", range, [col("date", "Str.InvoiceDate", "date"), col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), money("amount", "Str.Amount"), col("status", "Str.Status", "status")]),

  dashboard("ExecutiveAnalytics",
    [{ key: "Revenue", drill: "SalesReport" }, { key: "GrossProfit", drill: "IncomeStatement" }, { key: "Cash", drill: "Treasuries" }, { key: "Receivables", drill: "CustomerAging", lowerIsBetter: true },
      { key: "Payables", drill: "SupplierAging", lowerIsBetter: true }, { key: "StockValue", drill: "StockBalances" }],
    [{ key: "SalesPurchases", kind: "Column", span: 2 }, { key: "Expenses", kind: "Donut", drill: "IncomeStatement" }, { key: "Margin", kind: "Area", span: 2, drill: "IncomeStatement", smooth: true }, { key: "CashCycle", kind: "Bar" }]),
  tradeDashboard("SalesAnalytics", "SalesReport", "CustomerBalances", "SalesReturnsByInvoice", "QuotationFollowUp", "CustomerAging"),
  tradeDashboard("PurchaseAnalytics", undefined, "SupplierBalances", "PurchaseReturnsByInvoice", "PurchaseOrderFollowUp", "SupplierAging"),
  dashboard("InventoryAnalytics",
    [{ key: "StockValue", drill: "StockBalances" }, { key: "Items", kind: "count" }, { key: "DeadItems", kind: "count", lowerIsBetter: true }, { key: "Dio", kind: "days", lowerIsBetter: true }, { key: "Expiring", kind: "count", drill: "ExpiryReport" }],
    [{ key: "ByWarehouse", kind: "Bar", drill: "StockBalances" }, { key: "ByCategory", kind: "Donut" }, { key: "Abc", kind: "Column" }, { key: "DeadStock", kind: "Bar", span: 2 }, { key: "Speed", kind: "Column", tone: "Improving" }]),
  dashboard("LiquidityAnalytics",
    [{ key: "Cash", drill: "Treasuries" }, { key: "Inflows", drill: "CustomerAging" }, { key: "Outflows", drill: "SupplierAging", lowerIsBetter: true }, { key: "Gap" }, { key: "Coverage", kind: "percent" }],
    [{ key: "Weekly", kind: "Column", span: 2 }, { key: "ByTreasury", kind: "Bar", drill: "Treasuries" }, { key: "Aging", kind: "Column", span: 3, drill: "CustomerAging" }]),
  dashboard("PeopleAnalytics",
    [{ key: "Headcount", kind: "count", drill: "Employees" }, { key: "PayrollCost", drill: "PayrollSummary" }, { key: "AverageSalary" }, { key: "AbsenceRate", kind: "percent", drill: "AttendanceSummary", lowerIsBetter: true },
      { key: "OvertimeHours", kind: "count", drill: "AttendanceSummary" }, { key: "PendingAdvances", drill: "AdvancesReport", lowerIsBetter: true }],
    [{ key: "PayrollTrend", kind: "Column", span: 2, drill: "PayrollSummary" }, { key: "Attendance", kind: "Donut", drill: "AttendanceSummary" }, { key: "HeadcountByDepartment", kind: "Bar", drill: "Employees" },
      { key: "CostByDepartment", kind: "Donut", drill: "PayrollSummary" }, { key: "HiresLeavers", kind: "Column" }]),
  dashboard("ProductionAnalytics",
    [{ key: "Produced", kind: "count", drill: "ProductionCostReport" }, { key: "ProductionCost", drill: "ProductionCostReport", lowerIsBetter: true }, { key: "UnitCost", lowerIsBetter: true },
      { key: "Orders", kind: "count", drill: "ProductionOrders" }, { key: "ScrapRate", kind: "percent", drill: "ScrapReport", lowerIsBetter: true }, { key: "WorkHours", kind: "count", drill: "WorkCenterLoadReport" }],
    [{ key: "CostTrend", kind: "Column", span: 2, drill: "ProductionCostReport" }, { key: "CostMix", kind: "Donut", drill: "ConsumptionReport" }, { key: "TopProducts", kind: "Bar", drill: "ProductionCostReport" },
      { key: "CenterLoad", kind: "Bar", drill: "WorkCenterLoadReport" }, { key: "ScrapTrend", kind: "Area", drill: "ScrapReport", smooth: true }]),

  custom("Settings", "Settings", "Settings"),
  list({ key: "Branches", title: "Str.Module.Branches", permission: "Branches",
    columns: [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), col("isActive", "Str.Active", "bool")],
    form: [field("name", "Str.Name", "text", { required: true, max: 200 }), pick("warehouseId", "Str.Branch.Warehouse", { page: "Warehouses" }), pick("treasuryId", "Str.Branch.Treasury", { page: "Treasuries" }), active] }),
  list({ key: "AlertRules", title: "Str.Module.AlertRules", permission: "AlertRules", formTitles: ["Str.AlertRules.Add", "Str.AlertRules.Edit"],
    columns: [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(12rem,2fr)" }), col("event", "Str.Alert.Event"), col("channels", "Str.Alert.Channels"), col("recipients", "Str.Alert.Recipients"), col("isActive", "Str.Active", "bool")],
    form: [field("name", "Str.Name", "text", { required: true, max: 200 }), pick("event", "Str.Alert.Event", { alertEvents: true }, { required: true }),
      pick("channels", "Str.Alert.Channels", { enumOf: [["InApp", "Str.Alert.Channel.InApp"], ["Email", "Str.Alert.Channel.Email"], ["WhatsApp", "Str.Alert.Channel.WhatsApp"], ["Webhook", "Str.Alert.Channel.Webhook"]] }, { required: true }),
      field("recipients", "Str.Alert.Recipients", "textarea", { max: 1000 }), field("threshold", "Str.Alert.Threshold", "number", { min: 0 }), active] }),
  list({ key: "ApiKeys", title: "Str.Module.ApiKeys", permission: "ApiKeys", formTitles: ["Str.ApiKeys.Add", "Str.ApiKeys.Edit"],
    columns: [col("code", "Str.Code", "code"), col("name", "Str.Name", "text", { width: "minmax(12rem,2fr)" }), col("prefix", "Str.ApiKey.Prefix", "code"), col("expiresAt", "Str.ApiKey.ExpiresAt", "date"), col("lastUsedAt", "Str.ApiKey.LastUsedAt", "date"), col("isActive", "Str.Active", "bool")],
    form: [field("name", "Str.Name", "text", { required: true, max: 200 }), pick("userId", "Str.ApiKey.User", { page: "Users" }, { required: true }), field("expiresAt", "Str.ApiKey.ExpiresAt", "date"), active],
    recordActions: [{ key: "issue", label: "Str.ApiKey.Issue", action: "Edit", run: "issueApiKey" }] }),
  list({ key: "Users", title: "Str.Module.Users", permission: "Users",
    columns: [col("username", "Str.Username", "code"), col("displayName", "Str.DisplayName", "text", { width: "minmax(14rem,2fr)" }), col("roleName", "Str.Role"), col("statusText", "Str.Status", "status")],
    form: [field("username", "Str.Username", "text", { required: true, max: 50, createOnly: true }), field("displayName", "Str.DisplayName", "text", { required: true, max: 100 }),
      pick("roleId", "Str.Role", { page: "Roles" }, { required: true }), pick("branchId", "Str.Branch", { page: "Branches" }), field("password", "Str.Password", "password", { required: true, createOnly: true }), active] }),
  list({ key: "Roles", title: "Str.Module.Roles", permission: "Users",
    columns: [col("nameAr", "Str.Name", "text", { width: "minmax(14rem,2fr)" }), col("name", "Str.RoleNameEn")],
    form: [field("nameAr", "Str.Name", "text", { required: true, max: 100 }), field("name", "Str.RoleNameEn", "text", { max: 100 })] }),
  custom("RolePermissions", "Users", "RolePermissions"),
  custom("UserPermissions", "Users", "UserPermissions"),
];

/** محرّر أرصدة الأصناف */
export const openingStock: DocumentSpec = doc({
  key: "OpeningStock", title: "Str.Module.OpeningStock", permission: "Journal",
  columns: [],
  header: [date("date", "Str.StartDate", { readOnly: true }), warehouse()],
  lines: { key: "lines", fields: [pick("productId", "Str.Product", { page: "Products" }, { required: true, width: "minmax(16rem,2fr)" }), num("qty", "Str.Qty", { defaultValue: 1 }), num("unitCost", "Str.CostPrice"), notesLine] },
});

const named = (prefix: string, names: string[], byName = false): Source => ({ enumOf: names.map((name, i): [number | string, string] => [byName ? name : i, `${prefix}.${name}`]) });
const builderModule = (where?: Record<string, string>): Source => ({ page: "BuilderModules", ...(where ? { where } : {}) });
const builderModuleKey: Source = { page: "BuilderModules", by: "code" };
const builderFilters = [pick("sectionId", "Str.Builder.Sections", { page: "BuilderSections" }), pick("moduleId", "Str.Builder.Modules", builderModule({ sectionId: "@sectionId" }))];

/** صفحات البنّاء */
const builderPages: PageSpec[] = [
  list({ key: "BuilderSections", title: "Str.Builder.Sections", permission: "BuilderSections", formTitles: ["Str.Builder.Section", "Str.Builder.Section"],
    columns: [col("key", "Str.Builder.Key", "code"), col("title", "Str.Builder.Title", "text", { width: "minmax(14rem,2fr)" }), col("titleEn", "Str.Builder.TitleEn"), col("sortOrder", "Str.Builder.Order", "count")],
    form: [field("key", "Str.Builder.Key", "text", { required: true, max: 60 }), field("title", "Str.Builder.Title", "text", { required: true, max: 120 }),
      field("titleEn", "Str.Builder.TitleEn", "text", { max: 120 }), field("iconKey", "Str.Builder.Icon", "text", { max: 60 })] }),
  list({ key: "BuilderModules", title: "Str.Builder.Modules", permission: "BuilderModules", formTitles: ["Str.Builder.Module", "Str.Builder.Module"],
    filters: [pick("sectionId", "Str.Builder.Sections", { page: "BuilderSections" })],
    columns: [col("key", "Str.Builder.Key", "code"), col("title", "Str.Builder.Title", "text", { width: "minmax(13rem,2fr)" }), col("titleEn", "Str.Builder.TitleEn"), col("sectionName", "Str.Builder.Sections"),
      col("kindName", "Str.Builder.Kind"), col("sortOrder", "Str.Builder.Order", "count"), col("tableName", "Str.Builder.Table")],
    form: [pick("kind", "Str.Builder.Kind", named("Str.Builder.Kind", ["Report", "Record", "Movement"]), { required: true }),
      pick("sectionId", "Str.Builder.Sections", { page: "BuilderSections" }, { required: true }),
      field("key", "Str.Builder.Key", "text", { required: true, max: 60, lockOnEdit: true }), field("title", "Str.Builder.Title", "text", { required: true, max: 160 }),
      field("titleEn", "Str.Builder.TitleEn", "text", { max: 160 }),
      field("tableName", "Str.Builder.Table", "text", { max: 60, visibleWhen: { field: "kind", value: 1 } }),
      field("lineTable", "Str.Builder.LineTable", "text", { max: 60, visibleWhen: { field: "kind", value: 2 } }),
      pick("sourceKey", "Str.Builder.ReadsFrom", builderModuleKey, { visibleWhen: { field: "kind", value: 0 } }),
      pick("copiedFrom", "Str.Builder.CopyOf", { pages: true }),
      field("isActive", "Str.Active", "switch", { defaultValue: true })] }),
  list({ key: "BuilderColumns", title: "Str.Builder.Columns", permission: "BuilderColumns", formTitles: ["Str.Builder.Column", "Str.Builder.Column"], filters: builderFilters,
    columns: [col("name", "Str.Builder.ColumnName", "code"), col("header", "Str.Builder.Header", "text", { width: "minmax(13rem,2fr)" }), col("headerEn", "Str.Builder.HeaderEn"), col("moduleName", "Str.Builder.Modules"),
      col("sectionName", "Str.Builder.Sections"), col("dataType", "Str.Builder.Kind"), col("widthPercent", "Str.Builder.WidthPercent", "number"), col("footer", "Str.Builder.Footer")],
    form: [pick("moduleId", "Str.Builder.Modules", builderModule(), { required: true }), field("header", "Str.Builder.Header", "text", { required: true, max: 120 }),
      field("headerEn", "Str.Builder.HeaderEn", "text", { max: 120 }), field("name", "Str.Builder.ColumnName", "text", { required: true, max: 60 }),
      pick("dataType", "Str.Builder.DataType", named("Str.Builder.Type", ["Text", "Number", "Money", "Date", "Bool", "LongText", "Reference"]), { required: true }),
      pick("refModule", "Str.Builder.FromTable", builderModuleKey, { visibleWhen: { field: "dataType", value: 6 } }),
      field("refDisplay", "Str.Builder.DisplayColumn", "text", { max: 60, visibleWhen: { field: "dataType", value: 6 } }),
      pick("aggregate", "Str.Builder.Computed", named("Str.Builder.Agg", ["None", "Sum", "Count", "Avg", "Min", "Max"]), { defaultValue: 0 }),
      pick("aggFrom", "Str.Builder.AggFrom", builderModuleKey), field("aggColumn", "Str.Builder.AggColumn", "text", { max: 60 }), field("aggMatch", "Str.Builder.AggMatch", "text", { max: 60 }),
      field("isRequired", "Str.Builder.Required", "switch"), field("isUnique", "Str.Builder.Unique", "switch"), num("maxLength", "Str.Builder.Length"),
      pick("footer", "Str.Builder.Footer", named("Str.Builder.Agg", ["None", "Sum", "Count", "Average"], true)),
      field("showInGrid", "Str.Builder.InGrid", "switch", { defaultValue: true }), field("showInForm", "Str.Builder.InForm", "switch", { defaultValue: true }),
      field("isLine", "Str.Builder.IsLine", "switch"), num("width", "Str.Builder.Width", { defaultValue: 140 }), num("widthPercent", "Str.Builder.WidthPercent")] }),
  list({ key: "BuilderActions", title: "Str.Builder.Actions", permission: "BuilderActions", formTitles: ["Str.Builder.Action", "Str.Builder.Action"], filters: builderFilters,
    columns: [col("actionKey", "Str.Builder.Button", "text", { width: "minmax(13rem,2fr)" }), col("moduleName", "Str.Builder.Modules"), col("sectionName", "Str.Builder.Sections"), col("onTable", "Str.Builder.OnRow", "bool")],
    form: [pick("moduleId", "Str.Builder.Modules", builderModule(), { required: true }), pick("actionKey", "Str.Builder.Button", { actions: true }, { required: true }),
      field("onTable", "Str.Builder.OnRow", "switch")] }),
  list({ key: "BuilderFilters", title: "Str.Builder.Filters", permission: "BuilderFilters", formTitles: ["Str.Builder.Filter", "Str.Builder.Filter"], filters: builderFilters,
    columns: [col("key", "Str.Builder.Key", "code"), col("label", "Str.Builder.Filter", "text", { width: "minmax(13rem,2fr)" }), col("moduleName", "Str.Builder.Modules"), col("sectionName", "Str.Builder.Sections"), col("kind", "Str.Builder.Kind")],
    form: [pick("moduleId", "Str.Builder.Modules", builderModule(), { required: true }), field("key", "Str.Builder.Key", "text", { required: true, max: 60 }),
      field("label", "Str.Builder.Header", "text", { max: 120 }), field("labelEn", "Str.Builder.HeaderEn", "text", { max: 120 }),
      pick("kind", "Str.Builder.Kind", named("Str.Builder.Filter", ["Combo", "Toggle", "DateRange"], true)), pick("refModule", "Str.Builder.FromTable", builderModuleKey)] }),
  custom("BuilderExport", "BuilderExport", "BuilderExport", "Str.Builder.Export"),
];

export const lookupLists: ListSpec[] = [
  nameList("Departments"),
  nameList("JobTitles"),
  nameList("AllowanceTypes"),
  nameList("DeductionTypes"),
  nameList("LeaveTypes", [col("daysPerYear", "Str.LeaveType.DaysPerYear", "count"), col("isPaid", "Str.LeaveType.IsPaid", "bool")],
    [num("daysPerYear", "Str.LeaveType.DaysPerYear", { min: 0 }), field("isPaid", "Str.LeaveType.IsPaid", "switch")]),
];

pages.push(...builderPages);

export const pageByKey = new Map(pages.map((p) => [p.key, p]));
