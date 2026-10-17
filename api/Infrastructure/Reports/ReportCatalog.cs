using System.Collections.Frozen;
using PrimeERP.Application.Reporting;

namespace PrimeERP.Api.Infrastructure.Reports
{
    /// <summary>تقريرٌ يخدمه الخادم: خدمته ودالّتها ومعاملاتها بترتيبها</summary>
    public sealed record ReportEntry(string Key, Type Service, string Method, string[] Arguments);

    /// <summary>دليل التقارير — قائمة السماح: لا تُستدعى دالّةٌ ليست فيه</summary>
    public static class ReportCatalog
    {
        private static readonly string[] Range = { "From", "To" };

        private static ReportEntry R<TService>(string key, string method, params string[] arguments) =>
            new(key, typeof(TService), method, arguments.Length == 0 ? Range : arguments);

        private static ReportEntry Plain<TService>(string key, string method) => new(key, typeof(TService), method, Array.Empty<string>());

        private static readonly FrozenDictionary<string, ReportEntry> Reports = new[]
        {
            R<IFinancialStatementService>("TrialBalance", nameof(IFinancialStatementService.TrialBalance)),
            R<IFinancialStatementService>("IncomeStatement", nameof(IFinancialStatementService.IncomeStatement)),
            R<IFinancialStatementService>("BalanceSheet", nameof(IFinancialStatementService.BalanceSheet), "AsOf"),
            R<IFinancialStatementService>("CashFlow", nameof(IFinancialStatementService.CashFlow)),

            R<IPartyReportService>("CustomerBalances", nameof(IPartyReportService.CustomerBalances)),
            R<IPartyReportService>("SupplierBalances", nameof(IPartyReportService.SupplierBalances)),
            R<IPartyReportService>("CustomerStatement", nameof(IPartyReportService.CustomerStatement), "CustomerId", "From", "To"),
            R<IPartyReportService>("SupplierStatement", nameof(IPartyReportService.SupplierStatement), "SupplierId", "From", "To"),
            R<IPartyReportService>("AccountStatement", nameof(IPartyReportService.AccountStatement), "AccountId", "From", "To"),

            R<IStockReportService>("StockBalances", nameof(IStockReportService.Balances)),
            R<IStockReportService>("StockReport", nameof(IStockReportService.Movements), "From", "To", "WarehouseId"),
            R<IStockReportService>("ItemCard", nameof(IStockReportService.ItemCard), "ProductId"),
            Plain<IStockReportService>("BatchReport", nameof(IStockReportService.Batches)),
            Plain<IStockReportService>("ExpiryReport", nameof(IStockReportService.Expiry)),

            R<ISalesReportService>("SalesReport", nameof(ISalesReportService.Invoices)),
            R<IPayslipReportService>("Payslip", nameof(IPayslipReportService.Payslip), "EmployeeId", "From", "To"),
            R<IHrReportService>("PayrollSummary", nameof(IHrReportService.PayrollSummary)),
            R<IHrReportService>("AttendanceSummary", nameof(IHrReportService.AttendanceSummary)),
            R<IHrReportService>("AdvancesReport", nameof(IHrReportService.Advances)),
            R<IPosReportService>("CashierReport", nameof(IPosReportService.Cashiers)),

            R<IManufacturingReportService>("ProductionCostReport", nameof(IManufacturingReportService.ProductionCost)),
            R<IManufacturingReportService>("ConsumptionReport", nameof(IManufacturingReportService.Consumption)),
            R<IManufacturingReportService>("WorkCenterLoadReport", nameof(IManufacturingReportService.WorkCenterLoad)),
            R<IManufacturingReportService>("ScrapReport", nameof(IManufacturingReportService.Scrap)),

            R<IAssetReportService>("AssetRegister", nameof(IAssetReportService.Register), "From", "To", "CategoryId"),
            Plain<IAssetReportService>("AssetsByCategory", nameof(IAssetReportService.ByCategory)),

            R<IFlowReportService>("PurchaseRequestFollowUp", nameof(IFlowReportService.PurchaseRequests), "From", "To", "State"),
            R<IFlowReportService>("PurchaseOrderFollowUp", nameof(IFlowReportService.PurchaseOrders), "From", "To", "State"),
            R<IFlowReportService>("QuotationFollowUp", nameof(IFlowReportService.Quotations), "From", "To", "State"),
            R<IFlowReportService>("SalesOrderFollowUp", nameof(IFlowReportService.SalesOrders), "From", "To", "State"),
            R<IFlowReportService>("SalesReturnsByInvoice", nameof(IFlowReportService.SalesInvoiceReturns), "From", "To", "State"),
            R<IFlowReportService>("PurchaseReturnsByInvoice", nameof(IFlowReportService.PurchaseInvoiceReturns), "From", "To", "State"),
            R<IFlowReportService>("SalesReturnsPending", nameof(IFlowReportService.SalesReturnsPending), "From", "To", "State"),
            R<IFlowReportService>("PurchaseReturnsPending", nameof(IFlowReportService.PurchaseReturnsPending), "From", "To", "State"),
            R<IFlowReportService>("SalesReturnsUnlinked", nameof(IFlowReportService.SalesReturnsUnlinked)),
            R<IFlowReportService>("PurchaseReturnsUnlinked", nameof(IFlowReportService.PurchaseReturnsUnlinked)),

            R<IAgingReportService>("CustomerAging", nameof(IAgingReportService.CustomerAging), "AsOf"),
            R<IAgingReportService>("SupplierAging", nameof(IAgingReportService.SupplierAging), "AsOf"),
            R<IAgingReportService>("CustomerInvoiceSettlement", nameof(IAgingReportService.CustomerInvoices)),
            R<IAgingReportService>("SupplierInvoiceSettlement", nameof(IAgingReportService.SupplierInvoices)),
        }.ToFrozenDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);

        public static bool TryGet(string key, out ReportEntry entry) => Reports.TryGetValue(key ?? "", out entry);
    }
}
