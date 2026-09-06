using System;
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

            // المقاس من مقاس الورقة نفسها لا رقم ثابت: الفاتورة العرضيّة أوسع من التقرير الطولي، ورقم
            // واحد لهما يقصّ الأولى أو يترك فراغاً في الثاني. والحدّ الأعلى مساحة الشاشة المتاحة.
            var page = document.DocumentPaginator.PageSize;
            var area = SystemParameters.WorkArea;
            const double Chrome = 90;

            var window = new Window
            {
                Title = title,
                Content = viewer,
                Width = Math.Min(page.Width + Chrome, area.Width * 0.95),
                Height = Math.Min(page.Height + Chrome, area.Height * 0.95),
                MinWidth = 720,
                MinHeight = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FlowDirection = FlowDirection.RightToLeft,
                Owner = System.Windows.Application.Current?.MainWindow
            };

            // عرض الصفحة كاملاً بلا قصّ أياً كان اتجاهها، والمستخدم يكبّر بعدها كما يشاء.
            window.Loaded += (_, _) =>
            {
                try { viewer.FitToWidth(); }
                catch (Exception) { /* المعاينة تبقى بمقاسها الافتراضي لو رفض العارض الملاءمة */ }
            };

            // ⚠️ Show() لا ShowDialog() — ShowDialog تُعلَّق في بيئة هذا الجهاز (توقف 10، ARCHITECTURE.md).
            window.Show();
        }
    }
}
