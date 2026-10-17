using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Domain.Contracts;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Services;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>طباعة أي قائمة معروضة وتصديرها</summary>
    public static class ListOutput
    {
        public static void Print(IServiceProvider services, string title, List<GridColumn> columns, List<object> rows, Dictionary<string, string> totals = null)
        {
            var toast = services.GetRequiredService<IToastService>();
            if (rows.Count == 0) { toast.Info(LocalizationService.Get("Str.Output.NoPrintData")); return; }

            var printable = ReportDocument(title, columns, rows, totals);

            var printed = services.GetRequiredService<IPrintService>().PrintPreview(printable);
            if (printed.IsFailure) toast.Error(printed.ErrorMessage);
        }

        public static void Export(IServiceProvider services, string title, List<GridColumn> columns, List<object> rows, Dictionary<string, string> totals = null)
        {
            var toast = services.GetRequiredService<IToastService>();
            if (rows.Count == 0) { toast.Info(LocalizationService.Get("Str.Output.NoExportData")); return; }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"{title}-{DateTime.Today:yyyy-MM-dd}",
                Filter = "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv|PDF (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != true) return;

            var export = services.GetRequiredService<IExportService>();
            try
            {
                switch (System.IO.Path.GetExtension(dialog.FileName).ToLowerInvariant())
                {
                    case ".pdf":
                        var printable = ReportDocument(title, columns, rows, totals);
                        var result = services.GetRequiredService<IPrintService>().ExportToPdf(printable, dialog.FileName);
                        if (result.IsFailure) { toast.Error(result.ErrorMessage); return; }
                        break;
                    default:     export.ExportToExcel(rows, columns, dialog.FileName); break;
                }

                toast.Success(LocalizationService.Get("Str.Output.Exported", System.IO.Path.GetFileName(dialog.FileName)));
            }
            catch (Exception ex)
            {
                toast.Error(LocalizationService.Get("Str.Output.ExportFailed", ex.Message));
            }
        }
            private static PrimeERP.Domain.Contracts.IPrintable ReportDocument(
                string title,
                List<GridColumn> columns,
                List<object> rows,
                Dictionary<string, string> totals)
            {
                var orientation = columns.Count > 6
                    ? PrintOrientation.Landscape
                    : PrintOrientation.Portrait;

                return PrimeERP.Composition.Print.PrintDocuments.Report(
                    new ReportResult
                    {
                        Title = title,
                        Columns = columns,
                        Rows = rows,
                        Totals = totals
                    },
                    orientation);
            }
    }
}
