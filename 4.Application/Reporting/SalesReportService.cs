using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Audit;
using System;
using System.Linq;
using PrimeERP.Application.Services.Sales;
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
        private readonly ISalesInvoiceService _invoices;

        public SalesReportService(ISalesInvoiceService invoices, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) => _invoices = invoices;

        public Result<ReportData> Invoices(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;

            var result = _invoices.GetPaged(1, 5000);
            if (!result.IsSuccess) return Result.Fail<ReportData>(result.ErrorMessage);

            var rows = result.Value.Items
                .Where(i => i.InvoiceDate.Date >= from.Date && i.InvoiceDate.Date <= to.Date)
                .Select(i => new SalesReportRow
                {
                    InvoiceNo = i.InvoiceNo, Date = i.InvoiceDate.ToString("yyyy-MM-dd"),
                    PartyName = i.CustomerName, NetTotal = i.NetTotal
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
