using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Reporting
{
    /// <summary>أعمار الديون وسداد الفواتير من توزيع السداد</summary>
    public interface IAgingReportService
    {
        Result<ReportData> CustomerAging(DateTime asOf);
        Result<ReportData> SupplierAging(DateTime asOf);
        Result<ReportData> CustomerInvoices(DateTime from, DateTime to);
        Result<ReportData> SupplierInvoices(DateTime from, DateTime to);
    }

    public class AgingReportService : ReportServiceBase, IAgingReportService
    {
        private readonly IInvoiceRepository<SalesInvoice, SalesInvoiceLine> _sales;
        private readonly IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine> _purchases;
        private readonly IPartyRepository<Customer> _customers;
        private readonly IPartyRepository<Supplier> _suppliers;
        private readonly IVoucherRepository _vouchers;

        public AgingReportService(IInvoiceRepository<SalesInvoice, SalesInvoiceLine> sales, IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine> purchases,
            IPartyRepository<Customer> customers, IPartyRepository<Supplier> suppliers, IVoucherRepository vouchers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _sales = sales; _purchases = purchases; _customers = customers; _suppliers = suppliers; _vouchers = vouchers;
        }

        public Result<ReportData> CustomerAging(DateTime asOf) =>
            Aging(_sales.Open(), _customers.GetAll(activeOnly: false).ToDictionary(c => c.Id, c => (c.Code, c.Name)), asOf);

        public Result<ReportData> SupplierAging(DateTime asOf) =>
            Aging(_purchases.Open(), _suppliers.GetAll(activeOnly: false).ToDictionary(s => s.Id, s => (s.Code, s.Name)), asOf);

        public Result<ReportData> CustomerInvoices(DateTime from, DateTime to) =>
            Invoices(_sales.Between(from, to), i => i.CustomerId, ids => _customers.NamesOf(ids), VoucherKind.Receipt);

        public Result<ReportData> SupplierInvoices(DateTime from, DateTime to) =>
            Invoices(_purchases.Between(from, to), i => i.SupplierId, ids => _suppliers.NamesOf(ids), VoucherKind.Payment);

        /// <summary>طرفٌ في صف، والمتبقي في خانة عمره، مرتّباً من الأقدم دَيناً</summary>
        private Result<ReportData> Aging(List<(int PartyId, DateTime Date, DateTime? DueDate, decimal Remaining)> open,
            IReadOnlyDictionary<int, (string Code, string Name)> parties, DateTime asOf)
        {
            var gate = Gate(); if (gate != null) return gate;
            var rows = open.GroupBy(o => o.PartyId).Select(g =>
            {
                decimal In(AgingBucket bucket) => g.Where(o => AnalyticsCalc.Aging(o.DueDate, o.Date, asOf) == bucket).Sum(o => o.Remaining);
                var party = parties.GetValueOrDefault(g.Key);
                return new AgingRow
                {
                    Code = party.Code, Name = party.Name,
                    NotDue = In(AgingBucket.NotDue), Days30 = In(AgingBucket.Days30), Days60 = In(AgingBucket.Days60), Days90 = In(AgingBucket.Days90),
                    Days120 = In(AgingBucket.Days120), Days150 = In(AgingBucket.Days150), Over150 = In(AgingBucket.Over150),
                    Total = g.Sum(o => o.Remaining)
                };
            })
            .OrderByDescending(r => r.Over150).ThenByDescending(r => r.Days150).ThenByDescending(r => r.Days120)
            .ThenByDescending(r => r.Days90).ThenByDescending(r => r.Days60).ThenByDescending(r => r.Days30).ThenByDescending(r => r.Total)
            .ToList();

            var overdue = rows.Sum(r => r.Total - r.NotDue);
            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new()
                {
                    ["Total"] = $"{LocalizationService.Get("Str.Total")}: {rows.Sum(r => r.Total):N2}",
                    ["Overdue"] = LocalizationService.Get("Str.Aging.OverdueTotal", overdue, AnalyticsCalc.Share(overdue, rows.Sum(r => r.Total))),
                }
            });
        }

        /// <summary>كل فاتورةٍ بمسدَّدها ومتبقيها وتاريخ آخر سداد</summary>
        private Result<ReportData> Invoices<TInvoice>(List<TInvoice> invoices, Func<TInvoice, int> partyOf,
            Func<IEnumerable<int>, Dictionary<int, string>> names, VoucherKind kind) where TInvoice : InvoiceBase
        {
            var gate = Gate(); if (gate != null) return gate;
            var live = invoices.Where(i => i.Status != InvoiceStatus.Cancelled).ToList();
            var partyNames = names(live.Select(partyOf));
            var lastPaid = _vouchers.PaymentsOf(kind, live.Select(i => i.InvoiceNo)).GroupBy(p => p.InvoiceNo).ToDictionary(g => g.Key, g => (Last: g.Max(p => p.Date), Count: g.Count()));

            var rows = live.Select(i => new InvoiceSettlementRow
            {
                InvoiceNo = i.InvoiceNo, Date = i.InvoiceDate.ToString("yyyy-MM-dd"), DueDate = i.DueDate?.ToString("yyyy-MM-dd"),
                Party = partyNames.GetValueOrDefault(partyOf(i)), Net = i.NetTotal, Paid = i.PaidAmount, Remaining = i.RemainingAmount,
                LastPayment = lastPaid.TryGetValue(i.InvoiceNo, out var p) ? p.Last.ToString("yyyy-MM-dd") : null,
                Payments = lastPaid.TryGetValue(i.InvoiceNo, out var c) ? c.Count : 0,
                Status = LocalizationService.Get(i.RemainingAmount <= 0 ? "Str.Aging.Settled" : i.PaidAmount > 0 ? "Str.Aging.Partial" : "Str.Aging.Unpaid"),
            }).ToList();

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new() { ["Total"] = LocalizationService.Get("Str.Aging.InvoiceTotals", rows.Sum(r => r.Net), rows.Sum(r => r.Paid), rows.Sum(r => r.Remaining)) }
            });
        }
    }
}
