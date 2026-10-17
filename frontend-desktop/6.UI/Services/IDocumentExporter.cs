using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Results;

namespace PrimeERP.UI.Services
{
    /// <summary>تصدير مستند قابل للطباعة</summary>
    public interface IDocumentExporter
    {
        Result ExportPrintableToPdf(IPrintable document, string path);
    }
}
