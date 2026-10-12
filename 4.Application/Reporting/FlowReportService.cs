using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Reporting
{
    public interface IFlowReportService
    {
        Result<ReportData> PurchaseRequests(DateTime from, DateTime to, int? state);
        Result<ReportData> PurchaseOrders(DateTime from, DateTime to, int? state);
        Result<ReportData> Quotations(DateTime from, DateTime to, int? state);
        Result<ReportData> SalesOrders(DateTime from, DateTime to, int? state);
        Result<ReportData> SalesInvoiceReturns(DateTime from, DateTime to, int? state);
        Result<ReportData> PurchaseInvoiceReturns(DateTime from, DateTime to, int? state);
        Result<ReportData> SalesReturnsPending(DateTime from, DateTime to, int? state);
        Result<ReportData> PurchaseReturnsPending(DateTime from, DateTime to, int? state);
        Result<ReportData> SalesReturnsUnlinked(DateTime from, DateTime to);
        Result<ReportData> PurchaseReturnsUnlinked(DateTime from, DateTime to);
    }

    public class FlowReportService : ReportServiceBase, IFlowReportService
    {
        private readonly IPurchaseRequestRepository _requests;
        private readonly IPurchaseOrderRepository _purchaseOrders;
        private readonly IQuotationRepository _quotations;
        private readonly ISalesOrderRepository _salesOrders;
        private readonly IInvoiceRepository<SalesInvoice, SalesInvoiceLine> _sales;
        private readonly IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine> _purchases;
        private readonly IReturnRepository<SalesReturn, SalesReturnLine> _salesReturns;
        private readonly IReturnRepository<PurchaseReturn, PurchaseReturnLine> _purchaseReturns;
        private readonly IDocumentLinkRepository _links;
        private readonly IPartyRepository<Customer> _customers;
        private readonly IPartyRepository<Supplier> _suppliers;

        public FlowReportService(IPurchaseRequestRepository requests, IPurchaseOrderRepository purchaseOrders, IQuotationRepository quotations,
            ISalesOrderRepository salesOrders, IInvoiceRepository<SalesInvoice, SalesInvoiceLine> sales,
            IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine> purchases, IReturnRepository<SalesReturn, SalesReturnLine> salesReturns,
            IReturnRepository<PurchaseReturn, PurchaseReturnLine> purchaseReturns, IDocumentLinkRepository links,
            IPartyRepository<Customer> customers, IPartyRepository<Supplier> suppliers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _requests = requests; _purchaseOrders = purchaseOrders; _quotations = quotations; _salesOrders = salesOrders;
            _sales = sales; _purchases = purchases; _salesReturns = salesReturns; _purchaseReturns = purchaseReturns;
            _links = links; _customers = customers; _suppliers = suppliers;
        }

        public Result<ReportData> PurchaseRequests(DateTime from, DateTime to, int? state) =>
            Follow(_requests, "PurchaseRequest", _suppliers.NamesOf, from, to, state, "PurchaseOrder");

        public Result<ReportData> PurchaseOrders(DateTime from, DateTime to, int? state) =>
            Follow(_purchaseOrders, "PurchaseOrder", _suppliers.NamesOf, from, to, state, "StockIn", "PurchaseInvoices");

        public Result<ReportData> Quotations(DateTime from, DateTime to, int? state) =>
            Follow(_quotations, "Quotation", _customers.NamesOf, from, to, state, "SalesOrder");

        public Result<ReportData> SalesOrders(DateTime from, DateTime to, int? state) =>
            Follow(_salesOrders, "SalesOrder", _customers.NamesOf, from, to, state, "StockOut", "SalesInvoices");

        public Result<ReportData> SalesInvoiceReturns(DateTime from, DateTime to, int? state) =>
            Follow(_sales, "SalesInvoices", _customers.NamesOf, from, to, state, "SalesReturns");

        public Result<ReportData> PurchaseInvoiceReturns(DateTime from, DateTime to, int? state) =>
            Follow(_purchases, "PurchaseInvoices", _suppliers.NamesOf, from, to, state, "PurchaseReturns");

        public Result<ReportData> SalesReturnsPending(DateTime from, DateTime to, int? state) =>
            Follow(_salesReturns, "SalesReturns", _customers.NamesOf, from, to, state, "StockIn");

        public Result<ReportData> PurchaseReturnsPending(DateTime from, DateTime to, int? state) =>
            Follow(_purchaseReturns, "PurchaseReturns", _suppliers.NamesOf, from, to, state, "StockOut");

        public Result<ReportData> SalesReturnsUnlinked(DateTime from, DateTime to) =>
            Unlinked(_salesReturns, "SalesReturns", _customers.NamesOf, from, to);

        public Result<ReportData> PurchaseReturnsUnlinked(DateTime from, DateTime to) =>
            Unlinked(_purchaseReturns, "PurchaseReturns", _suppliers.NamesOf, from, to);

        private Result<ReportData> Follow(IFlowLines source, string kind, Func<IEnumerable<int>, PrimeDbContext, Dictionary<int, string>> names,
            DateTime from, DateTime to, int? state, string next, string last = null)
        {
            var gate = Gate(); if (gate != null) return gate;
            var lines = source.FlowLines(from, to);
            var ids = lines.Select(l => l.LineId).ToList();
            var done = _links.PulledInto(kind, ids, next);
            var closed = last == null ? null : _links.PulledThrough(kind, ids, next, last);
            var parties = names(lines.Where(l => l.PartyId != null).Select(l => l.PartyId.Value), null);

            var rows = lines.Select(l =>
            {
                var pulled = done.GetValueOrDefault(l.LineId);
                decimal? final = closed?.GetValueOrDefault(l.LineId);
                return (Line: l, Pulled: pulled, Final: final, State: LineCalc.Progress(l.Qty, pulled, final));
            })
            .Where(x => state is not int wanted || wanted < 0 || (int)x.State == wanted)
            .Select(x => Row(x.Line, parties, r =>
            {
                r.Done = x.Pulled;
                r.Closed = x.Final ?? 0;
                r.Remaining = Math.Max(0, x.Line.Qty - x.Pulled);
                r.Status = LocalizationService.Get($"Str.Flow.State.{x.State}");
            })).OrderBy(r => r.Date).ThenBy(r => r.DocNo).ToList();

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new() { ["Total"] = LocalizationService.Get("Str.Flow.OpenLines", rows.Count(r => r.Status != LocalizationService.Get("Str.Flow.State.Complete")), rows.Count) }
            });
        }

        private Result<ReportData> Unlinked(IFlowLines source, string kind, Func<IEnumerable<int>, PrimeDbContext, Dictionary<int, string>> names,
            DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;
            var lines = source.FlowLines(from, to);
            var linked = _links.LinkedTargets(kind, lines.Select(l => l.LineId));
            var free = lines.Where(l => !linked.Contains(l.LineId)).ToList();
            var parties = names(free.Where(l => l.PartyId != null).Select(l => l.PartyId.Value), null);

            var rows = free.Select(l => Row(l, parties, _ => { })).OrderBy(r => r.Date).ThenBy(r => r.DocNo).ToList();
            return Result.Ok(new ReportData { Rows = rows, Totals = new() { ["Total"] = $"{LocalizationService.Get("Str.Total")}: {rows.Sum(r => r.Qty):N2}" } });
        }

        private static FlowRow Row(FlowLine l, IReadOnlyDictionary<int, string> parties, Action<FlowRow> fill)
        {
            var row = new FlowRow
            {
                DocNo = l.DocNo, Date = l.Date.ToString("yyyy-MM-dd"), Party = l.PartyId is int id ? parties.GetValueOrDefault(id) : null,
                Code = l.ProductCode, Name = l.ProductName, Qty = l.Qty
            };
            fill(row);
            return row;
        }
    }
}
