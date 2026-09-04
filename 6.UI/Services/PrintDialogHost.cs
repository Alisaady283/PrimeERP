using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using PrimeERP.Application.Services.Print;

namespace PrimeERP.UI.Services
{
    /// <summary>الواجهة المرئية للطباعة — PrintService يبني المستند ولا يفتح نوافذ، وهذه تفتحها. المعاينة
    /// أولاً هي الافتراضي: DocumentViewer نفسه يحمل زر الطباعة، فلا نطبع بالخطأ قبل رؤية الورق.</summary>
    public class PrintDialogHost : IPrintDialogHost
    {
        public bool ShowPrintDialog(FixedDocument document)
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true) return false;

            dialog.PrintDocument(document.DocumentPaginator, document.ToString());
            return true;
        }

        public void ShowPreview(FixedDocument document, string title)
        {
            var viewer = new DocumentViewer { Document = document };
            var window = new Window
            {
                Title = title,
                Content = viewer,
                Width = 900,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FlowDirection = FlowDirection.RightToLeft,
                Owner = System.Windows.Application.Current?.MainWindow
            };

            // ⚠️ Show() لا ShowDialog() — ShowDialog تُعلَّق في بيئة هذا الجهاز (توقف 10، ARCHITECTURE.md).
            window.Show();
        }
    }
}
