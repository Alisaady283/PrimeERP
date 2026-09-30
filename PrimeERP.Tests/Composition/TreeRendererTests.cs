using PrimeERP.Application.Services.Ledger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Display;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>تصيير الشجرة</summary>
    [Collection("WpfApplication")]
    public class TreeRendererTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public TreeRendererTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Render_BuildsTreePage_AndLoadsRealHierarchyOnLoaded()
        {
            WpfApplicationFixture.Run(() =>
            {
                PrimeERP.UI.Services.UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();

                var accounts = _db.Services.GetRequiredService<IAccountService>();
                var parent = accounts.Create(new CreateAccountDto { ParentId = 1, Name = "أب اختباري", IsLeaf = false });
                Assert.True(parent.IsSuccess, parent.ErrorMessage);
                var child = accounts.Create(new CreateAccountDto { ParentId = parent.Value.Id, Name = "ابن اختباري", IsLeaf = true });
                Assert.True(child.IsSuccess, child.ErrorMessage);

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var definition = registry.Get("Accounts");
                Assert.NotNull(definition);

                var element = PageRenderer.Render(definition, _db.Services);
                Assert.NotNull(element);

                element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                dynamic vm = element.DataContext;
                Assert.NotNull(vm);

                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (!((IEnumerable)vm.RootNodes).Cast<object>().Any() && DateTime.UtcNow < deadline)
                {
                    var frame = new DispatcherFrame();
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                    Dispatcher.PushFrame(frame);
                }

                Assert.True(((IEnumerable)vm.RootNodes).Cast<object>().Any());

                // ⚠️ this part caught the real defect
                var treeView = FindVisualChild<AppTreeView>(element);
                Assert.NotNull(treeView);
                Assert.NotNull(treeView.ItemsSource);
                Assert.True(treeView.ItemsSource.Cast<object>().Any());
            });
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var nested = FindVisualChild<T>(child);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
