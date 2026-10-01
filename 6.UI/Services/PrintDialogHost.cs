using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PrimeERP.UI.Services
{
    /// <summary>الواجهة المرئية للطباعة</summary>
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

            window.Loaded += (_, _) =>
            {
                try { viewer.FitToWidth(); }
                catch (Exception) { /* المعاينة تبقى بمقاسها الافتراضي لو رفض العارض الملاءمة */ }
            };

            // ⚠️ Show() not ShowDialog() — ARCHITECTURE § المصائد
            window.Show();
        }
    }
}
