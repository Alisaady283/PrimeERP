using System.Windows.Documents;

namespace PrimeERP.Application.Services.Print
{
    /// <summary>تنفّذه طبقة الواجهة</summary>
    public interface IPrintDialogHost
    {
        bool ShowPrintDialog(FixedDocument document);

        void ShowPreview(FixedDocument document, string title);
    }
}
