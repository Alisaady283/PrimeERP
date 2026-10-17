using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Application.PageServices.Documents;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.PageServices.Purchasing;
using PrimeERP.Application.PageServices.Sales;
using PrimeERP.Domain.Contracts;

namespace PrimeERP.Application.Services.Documents
{
    /// <summary>ظ…ط³طھظ†ط¯ط§طھ ط§ظ„ظ…طµط¯ط± ظپظٹ ط³ظ„ط³ظ„ط© ط§ظ„ط³ط­ط¨: ظ†ظˆط¹ ط§ظ„ظ…ط³طھظ†ط¯ â†گ ط®ط¯ظ…طھظ‡طŒ ظˆظ‚ط±ط§ط،ط© ط§ظ„ظ…ط³طھظ†ط¯ ط¨ط³ط·ظˆط±ظ‡ â€” ظ„ظƒظ„ ظ…ط¶ظٹظپ</summary>
    public class SourceDocuments : IPullSourceReader
    {
        private static readonly FrozenDictionary<string, Type> Services = new Dictionary<string, Type>
        {
            ["PurchaseRequest"]  = typeof(IPurchaseRequestService),
            ["Quotation"]        = typeof(IQuotationService),
            ["PurchaseOrder"]    = typeof(IPurchaseOrderService),
            ["SalesOrder"]       = typeof(ISalesOrderService),
            ["StockIn"]          = typeof(IStockInService),
            ["StockOut"]         = typeof(IStockOutService),
            ["PurchaseInvoices"] = typeof(IPurchaseInvoiceService),
            ["SalesInvoices"]    = typeof(ISalesInvoiceService),
            ["SalesReturns"]     = typeof(ISalesReturnService),
            ["PurchaseReturns"]  = typeof(IPurchaseReturnService),
        }.ToFrozenDictionary();

        private readonly IServiceProvider _services;

        public SourceDocuments(IServiceProvider services) => _services = services;

        public object ServiceOf(string sourceKind) =>
            sourceKind != null && Services.TryGetValue(sourceKind, out var type) ? _services.GetService(type) : null;

        public ISourceDocument Read(string sourceKind, int id)
        {
            var service = ServiceOf(sourceKind);
            return service == null ? null : ValueOf(Invoke(service, "GetById", id)) as ISourceDocument;
        }

        public decimal GetSourceLineQty(string sourceType, int sourceId, int sourceLineId) =>
            Read(sourceType, sourceId)?.Lines.FirstOrDefault(l => l.Id == sourceLineId)?.Qty ?? 0m;

        public string DocNoOf(string docType, int docId) => Read(docType, docId)?.DocNo;

        public static object Invoke(object service, string method, params object[] args) =>
            service.GetType().GetMethods()
                .First(m => m.Name == method && m.GetParameters().Length == args.Length)
                .Invoke(service, args);

        public static object ValueOf(object result) => result?.GetType().GetProperty("Value")?.GetValue(result);
    }
}
