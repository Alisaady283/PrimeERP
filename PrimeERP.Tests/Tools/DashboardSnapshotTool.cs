using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Data.Core;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Tools
{
    /// <summary>صورٌ للوحات التحليل كما تُعرض لمراجعة التصميم: يعمل فقط إن حُدِّد PRIMEERP_SNAPSHOT_DB و PRIMEERP_SNAPSHOT_OUT</summary>
    public class DashboardSnapshotTool
    {
        [Fact]
        public void Capture()
        {
            var db = Environment.GetEnvironmentVariable("PRIMEERP_SNAPSHOT_DB");
            var output = Environment.GetEnvironmentVariable("PRIMEERP_SNAPSHOT_OUT");
            if (string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(output) || !File.Exists(db)) return;
            var identity = Environment.GetEnvironmentVariable("PRIMEERP_SNAPSHOT_IDENTITY");
            var keys = (Environment.GetEnvironmentVariable("PRIMEERP_SNAPSHOT_KEYS") ?? "ExecutiveAnalytics,SalesAnalytics").Split(',');
            Directory.CreateDirectory(output);

            AppSession.DevMode = true;
            DbConfig.Use(new DbConfig { Provider = DatabaseProvider.Sqlite, FilePath = db });
            var services = TestDatabaseFixture.BuildServices();
            UIServices.Initialize(services);

            WpfApplicationFixture.Run(() =>
            {
                if (!string.IsNullOrWhiteSpace(identity))
                {
                    var main = typeof(PrimeERP.App.Bootstrap.DependencyInjection).Assembly.GetName().Name;
                    var merged = System.Windows.Application.Current.Resources.MergedDictionaries;
                    var current = merged.First(d => d.Source?.OriginalString.Contains("/Colors/") == true);
                    merged[merged.IndexOf(current)] = new ResourceDictionary { Source = new Uri($"pack://application:,,,/{main};component/5.Design/Colors/{identity}.xaml", UriKind.Absolute) };
                }

                foreach (var key in keys)
                {
                    var page = PageRenderer.Render(services.GetRequiredService<IModuleRegistry>().Get(key), services);
                    var window = new Window
                    {
                        Content = page, Width = 1440, Height = int.TryParse(Environment.GetEnvironmentVariable("PRIMEERP_SNAPSHOT_HEIGHT"), out var h) ? h : 1500, Left = -20000, Top = 0, ShowInTaskbar = false, WindowStyle = WindowStyle.None,
                        FlowDirection = FlowDirection.RightToLeft
                    };
                    window.SetResourceReference(Window.BackgroundProperty, "PageWash");
                    var scroll = ((System.Windows.Controls.Panel)page).Children.OfType<System.Windows.Controls.ScrollViewer>().FirstOrDefault();
                    var target = scroll?.Content as FrameworkElement ?? (FrameworkElement)window.Content;
                    target.FlowDirection = FlowDirection.RightToLeft;
                    if (target is System.Windows.Controls.Panel content && content.Background == null) content.Background = ((System.Windows.Controls.Panel)page).Background;
                    window.Show();
                    Wait(TimeSpan.FromMilliseconds(1800));
                    var png = VisualSnapshot.Png(target);
                    File.WriteAllBytes(Path.Combine(output, $"{key}{(string.IsNullOrWhiteSpace(identity) ? "" : "-" + identity)}.png"), Convert.FromBase64String(png));
                    if (Environment.GetEnvironmentVariable("PRIMEERP_SNAPSHOT_PRINT") == "1"
                        && ((System.Windows.Controls.Panel)page).Children.OfType<PrimeERP.UI.Components.Layout.PageHeader>().FirstOrDefault()?.ActionsContent is PrimeERP.UI.Components.Actions.ActionToolbar toolbar)
                    {
                        var print = services.GetRequiredService<IPrintService>();
                        var capture = new PageCapture();
                        print.DialogHost = capture;
                        toolbar.ButtonsSource.First().Command.Execute(null);
                        if (capture.Page != null) File.WriteAllBytes(Path.Combine(output, $"{key}-print.png"), Convert.FromBase64String(VisualSnapshot.Png(capture.Page)));
                        Wait(TimeSpan.FromMilliseconds(1200));
                        var after = ((System.Windows.Controls.Panel)page).Children.OfType<System.Windows.Controls.ScrollViewer>().First().Content as FrameworkElement;
                        File.WriteAllBytes(Path.Combine(output, $"{key}-after.png"), Convert.FromBase64String(VisualSnapshot.Png(after)));
                    }
                    window.Close();
                }
            });
        }

        /// <summary>يلتقط أول صفحةٍ من معاينة الطباعة بدل فتح نافذتها</summary>
        private class PageCapture : IPrintDialogHost
        {
            public FrameworkElement Page { get; private set; }
            public bool ShowPrintDialog(System.Windows.Documents.FixedDocument document) => false;
            public void ShowPreview(System.Windows.Documents.FixedDocument document, string title)
            {
                var page = document.Pages[0].Child;
                page.Measure(new Size(page.Width, page.Height));
                page.Arrange(new Rect(0, 0, page.Width, page.Height));
                page.UpdateLayout();
                Page = page;
            }
        }

        private static void Wait(TimeSpan delay)
        {
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer { Interval = delay };
            timer.Tick += (_, __) => { timer.Stop(); frame.Continue = false; };
            timer.Start();
            Dispatcher.PushFrame(frame);
        }
    }
}
