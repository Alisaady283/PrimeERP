using PrimeERP.Domain.Contracts;
using System.Collections;
using System.Collections.Generic;
using PrimeERP.Domain.Results;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.UI.Services
{
    /// <summary>خدمة واجهة Export</summary>
    public interface IExportService
    {
        void ExportToCsv(IEnumerable data, IEnumerable<GridColumn> columns, string filePath);
        void ExportToExcel(IEnumerable data, IEnumerable<GridColumn> columns, string filePath);
        void ExportToPdf(IEnumerable data, IEnumerable<GridColumn> columns, string filePath, string title);

        Result ExportPrintableToPdf(IPrintable document, string path);
    }
}
