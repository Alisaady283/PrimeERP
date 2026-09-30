using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Legacy.Parties;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>صفحة CRUD حقيقية من تعريفها</summary>
    [Collection("WpfApplication")]
    public class CrudPageRendererTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public CrudPageRendererTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Render_BuildsPage_AndLoadsRealDataOnLoaded()
        {
            _db.Services.GetRequiredService<ICustomerService>()
               .Create(new CreateCustomerDto { Name = "عميل Renderer" });

            WpfApplicationFixture.Run(() =>
            {
                PrimeERP.UI.Services.UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var definition = registry.Get("Customers");
                Assert.NotNull(definition);

                var element = CrudPageRenderer.Render(definition, _db.Services);
                Assert.IsType<Grid>(element);
                Assert.Equal(4, ((Grid)element).Children.Count);

                element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                dynamic vm = element.DataContext;
                Assert.NotNull(vm);

                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (!((IEnumerable)vm.Items).Cast<object>().Any() && DateTime.UtcNow < deadline)
                {
                    var frame = new DispatcherFrame();
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                    Dispatcher.PushFrame(frame);
                }

                Assert.True(((IEnumerable)vm.Items).Cast<object>().Any());
            });
        }

        [Fact]
        public void Render_ShowsTheDeclaredColumnsOnly_WithoutARowActionsColumn()
        {
            WpfApplicationFixture.Run(() =>
            {
                PrimeERP.UI.Services.UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var definition = registry.Get("Customers");

                var element = CrudPageRenderer.Render(definition, _db.Services);

                var window = new Window { Content = element, Width = 1200, Height = 800, ShowInTaskbar = false, WindowStyle = WindowStyle.None, ShowActivated = false };
                window.Show();
                window.UpdateLayout();

                var appGrid = FindVisualChild<PrimeERP.UI.Components.Display.AppDataGrid>(element);
                Assert.NotNull(appGrid);

                var innerGrid = FindVisualChild<DataGrid>(appGrid);
                Assert.NotNull(innerGrid);

                Assert.Equal(definition.Columns.Count, innerGrid.Columns.Count);
                window.Close();
            });
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var nested = FindVisualChild<T>(child);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
