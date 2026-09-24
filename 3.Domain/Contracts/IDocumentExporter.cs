using PrimeERP.Domain.Results;

namespace PrimeERP.Domain.Contracts
{
    /// <summary>عقد تصدير مستند قابل للطباعة</summary>
    public interface IDocumentExporter
    {
        Result ExportPrintableToPdf(IPrintable document, string path);
    }
}
