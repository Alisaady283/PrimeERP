using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Audit;
using System;
using System.Linq;
using PrimeERP.Application.PageServices.Sales;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Reporting
{
    /// <summary>تقرير المبيعات</summary>
    public interface ISalesReportService
    {
        Result<ReportData> Invoices(DateTime from, DateTime to);
    }

    public class SalesReportService : ReportServiceBase, ISalesReportService
    {
        private readonly IInvoiceRepository<SalesInvoice, SalesInvoiceLine> _invoices;
        private readonly IPartyRepository<Customer> _customers;

        public SalesReportService(IInvoiceRepository<SalesInvoice, SalesInvoiceLine> invoices, IPartyRepository<Customer> customers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _invoices = invoices;
            _customers = customers;
        }

        public Result<ReportData> Invoices(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;

            var invoices = _invoices.Between(from, to);
            var names = _customers.NamesOf(invoices.Select(i => i.CustomerId));
            var rows = invoices
                .Select(i => new SalesReportRow
                {
                    InvoiceNo = i.InvoiceNo, Date = i.InvoiceDate.ToString("yyyy-MM-dd"),
                    PartyName = names.GetValueOrDefault(i.CustomerId), NetTotal = i.NetTotal
                })
                .ToList();

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new() { ["Total"] = $"{LocalizationService.Get("Str.Total")}: {rows.Sum(r => r.NetTotal):N2}" }
            });
        }
    }
}
