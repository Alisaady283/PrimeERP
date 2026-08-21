using System.Collections;
using System.Collections.Generic;
using PrimeERP.Core.Common;
using PrimeERP.Services.Print;
using PrimeERP.Views.Controls.Display;

namespace PrimeERP.Services
{
    public interface IExportService
    {
        void ExportToCsv(IEnumerable data, IEnumerable<GridColumn> columns, string filePath);
        void ExportToExcel(IEnumerable data, IEnumerable<GridColumn> columns, string filePath);
        void ExportToPdf(IEnumerable data, IEnumerable<GridColumn> columns, string filePath, string title);

        /// <summary>يبني PDF من مستند طباعة كامل (عناوين/أقسام/توقيعات) لا جدول واحد فقط — يستخدمه IPrintService.ExportToPdf.</summary>
        Result ExportPrintableToPdf(IPrintable document, string path);
    }
}
