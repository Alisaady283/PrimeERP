using System.Windows.Documents;
using PrimeERP.Core.Common;

namespace PrimeERP.Services.Print
{
    public interface IPrintService
    {
        /// <summary>الواجهة (Host) التي تعرض PrintDialog/نافذة المعاينة فعلياً — الخدمة لا تفتح نوافذ بنفسها.</summary>
        IPrintDialogHost DialogHost { get; set; }

        Result Print(IPrintable document, bool showDialog = true);
        Result PrintPreview(IPrintable document);
        Result<FixedDocument> Build(IPrintable document);
        Result ExportToPdf(IPrintable document, string path);
    }
}
