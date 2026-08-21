using PrimeERP.Domain.Results;

namespace PrimeERP.Domain.Contracts
{
    /// <summary>
    /// عقد تصدير مستند قابل للطباعة لملف — يفصل PrintService (4.Application) عن تنفيذ التصدير الفعلي
    /// (ExportService في 6.UI، يعتمد على أنواع Views.Controls) بلا اعتماد Application على UI مباشرة.
    /// راجع ARCHITECTURE.md § مخالفة PrintService→ExportService لسبب هذا الفصل.
    /// </summary>
    public interface IDocumentExporter
    {
        Result ExportPrintableToPdf(IPrintable document, string path);
    }
}
