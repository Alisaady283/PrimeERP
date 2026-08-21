using System.Collections;
using System.Collections.Generic;
using PrimeERP.Domain.Results;
using PrimeERP.Application.Services.Print;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.UI.Services
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
