using System.Windows.Documents;

namespace PrimeERP.Application.Legacy.Print
{
    /// <summary>تنفّذه طبقة الواجهة</summary>
    public interface IPrintDialogHost
    {
        bool ShowPrintDialog(FixedDocument document);

        void ShowPreview(FixedDocument document, string title);
    }
}
