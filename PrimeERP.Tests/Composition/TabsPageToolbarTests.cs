using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.UI.Components.Actions;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    public class TabsPageToolbarTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public void Dispose() => _db.Dispose();

        [Theory]
        [InlineData("Departments")]
        [InlineData("HrLists")]
        public void TheToolbar_ShowsEveryButton(string key)
        {
            WpfApplicationFixture.Run(() =>
            {
                PrimeERP.UI.Services.UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();

                var element = PageRenderer.Render(_db.Services.GetRequiredService<IModuleRegistry>().Get(key), _db.Services);
                var window = new Window { Content = element, Width = 1300, Height = 800, ShowInTaskbar = false, WindowStyle = WindowStyle.None, ShowActivated = false };
                window.Show();
                for (var i = 0; i < 20; i++) Pump();

                var trace = State(element, "start");
                if (element is PrimeERP.UI.Components.Display.AppTabControl tabs)
                    foreach (var index in new[] { 1, 2, 0 })
                    {
                        tabs.SelectedIndex = index;
                        for (var i = 0; i < 20; i++) Pump();
                        trace += State(element, $"tab{index}");
                    }

                var toolbar = Find<ActionToolbar>(element);
                Assert.NotNull(toolbar);
                var overflow = (FrameworkElement)toolbar.FindName("overflowBtn");
                var main = (StackPanel)toolbar.FindName("mainPanel");
                var hidden = main.Children.OfType<FrameworkElement>().Count(c => c.Visibility != Visibility.Visible);

                window.Close();
                Assert.True(overflow.Visibility != Visibility.Visible && hidden == 0,
                    $"{key}: {trace}");
            });
        }

        private static string State(DependencyObject root, string step)
        {
            var t = Find<ActionToolbar>(root);
            var main = (StackPanel)t.FindName("mainPanel");
            return $" [{step}: w={t.ActualWidth:N0} hidden={main.Children.OfType<FrameworkElement>().Count(c => c.Visibility != Visibility.Visible)}/{main.Children.Count}]";
        }

        private static void Pump()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        private static T Find<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T found) return found;
                if (Find<T>(child) is { } deeper) return deeper;
            }
            return null;
        }
    }
}
