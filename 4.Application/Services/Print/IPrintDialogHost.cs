using System.Windows.Documents;

namespace PrimeERP.Application.Services.Print
{
    /// <summary>
    /// تنفّذه طبقة الواجهة (نفس نمط IDialogService) — PrintService يبني المستند فقط، هذا يعرضه.
    /// يمنع الخدمة من فتح PrintDialog/نافذة معاينة بنفسها (خدمة لا تعرف الواجهة).
    /// </summary>
    public interface IPrintDialogHost
    {
        /// <summary>يفتح PrintDialog النظامي ويطبع المستند لو المستخدم أكّد — يرجع false لو ألغى.</summary>
        bool ShowPrintDialog(FixedDocument document);

        /// <summary>يعرض نافذة معاينة قبل الطباعة الفعلية.</summary>
        void ShowPreview(FixedDocument document, string title);
    }
}
