using System.Collections;
using System.Collections.Generic;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.UI.Services
{
    /// <summary>خدمة واجهة Export</summary>
    public interface IExportService : IDocumentExporter
    {
        void ExportToCsv(IEnumerable data, IEnumerable<GridColumn> columns, string filePath);
        void ExportToExcel(IEnumerable data, IEnumerable<GridColumn> columns, string filePath);
        void ExportToPdf(IEnumerable data, IEnumerable<GridColumn> columns, string filePath, string title);
    }
}
