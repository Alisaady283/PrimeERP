using PrimeERP.Domain.Contracts;
using System.Windows.Documents;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Print
{
    public interface IPrintService
    {
        /// <summary>الواجهة (Host) التي تعرض PrintDialog/نافذة المعاينة فعلياً — الخدمة لا تفتح نوافذ بنفسها.</summary>
        IPrintDialogHost DialogHost { get; set; }

        Result Print(IPrintable document, bool showDialog = true);
        Result PrintPreview(IPrintable document);
        Result<FixedDocument> Build(IPrintable document);

        /// <summary>محتوى المستند قبل تقسيمه لصفحات ثابتة — التقسيم يرسم الصفحات كصور (VisualBrush) فيتعذّر
        /// فحص عناصرها. تُستهلَك في المعاينة والاختبار.</summary>
        Result<FlowDocument> BuildContent(IPrintable document);
        Result ExportToPdf(IPrintable document, string path);
    }
}
