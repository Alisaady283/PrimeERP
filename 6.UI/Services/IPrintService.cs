using PrimeERP.Domain.Contracts;
using System.Windows.Documents;
using PrimeERP.Domain.Results;

namespace PrimeERP.UI.Services
{
    /// <summary>عقد الطباعة</summary>
    public interface IPrintService
    {
        IPrintDialogHost DialogHost { get; set; }

        Result Print(IPrintable document, bool showDialog = true);
        Result PrintPreview(IPrintable document);
        Result<FixedDocument> Build(IPrintable document);

        Result<FlowDocument> BuildContent(IPrintable document);
        Result ExportToPdf(IPrintable document, string path);
    }
}
