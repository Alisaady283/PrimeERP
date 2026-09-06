using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Print;
using PrimeERP.Composition.Definitions;
using PrimeERP.Domain.Contracts;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Services;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>
    /// طباعة أي قائمة معروضة وتصديرها — القائمة والتقرير سواء: عنوان وأعمدة وصفوف. قطعة واحدة
    /// تستوردها الشاشتان بدل نسختين تتفرّقان.
    /// </summary>
    public static class ListOutput
    {
        public static void Print(IServiceProvider services, string title, List<GridColumn> columns, List<object> rows)
        {
            var toast = services.GetRequiredService<IToastService>();
            if (rows.Count == 0) { toast.Info("لا بيانات للطباعة"); return; }

            var orientation = columns.Count > 6 ? PrintOrientation.Landscape : PrintOrientation.Portrait;
            var printable = PrimeERP.Composition.Print.PrintDocuments.Report(new ReportResult { Title = title, Columns = columns, Rows = rows }, orientation);

            var printed = services.GetRequiredService<IPrintService>().PrintPreview(printable);
            if (printed.IsFailure) toast.Error(printed.ErrorMessage);
        }

        /// <summary>الصيغة تُختار من امتداد الملف — نافذة الحفظ نفسها هي القائمة، بلا حوار صيغ إضافي.</summary>
        public static void Export(IServiceProvider services, string title, List<GridColumn> columns, List<object> rows)
        {
            var toast = services.GetRequiredService<IToastService>();
            if (rows.Count == 0) { toast.Info("لا بيانات للتصدير"); return; }

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
                    case ".csv": export.ExportToCsv(rows, columns, dialog.FileName); break;
                    case ".pdf": export.ExportToPdf(rows, columns, dialog.FileName, title); break;
                    default:     export.ExportToExcel(rows, columns, dialog.FileName); break;
                }

                toast.Success($"تم التصدير إلى {System.IO.Path.GetFileName(dialog.FileName)}");
            }
            catch (Exception ex)
            {
                toast.Error($"فشل التصدير: {ex.Message}");
            }
        }
    }
}
