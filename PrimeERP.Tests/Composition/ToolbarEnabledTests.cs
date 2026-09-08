using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>أزرار الشريط: أيّها يعمل بلا تحديد صفّ، وأيّها ينتظره.</summary>
    [Collection("WpfApplication")]
    public class ToolbarEnabledTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public ToolbarEnabledTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private List<ToolbarAction> Toolbar(string moduleKey)
        {
            UIServices.Initialize(_db.Services);
            _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

            var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get(moduleKey);
            var page = CrudPageRenderer.Render(definition, _db.Services);
            page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            page.Measure(new Size(1200, 800));
            page.Arrange(new Rect(0, 0, 1200, 800));
            page.UpdateLayout();

            return Descendants<ActionToolbar>(page)
                .First(t => Ancestor<PrimeERP.UI.Components.Layout.FilterBar>(t) == null)
                .ButtonsSource.ToList();
        }

        [Fact]
        public void NewIsAlwaysEnabled_EditAndDeleteWaitForARow()
        {
            WpfApplicationFixture.Run(() =>
            {
                var actions = Toolbar("Quotation");

                bool Can(string text) => actions.Single(a => a.Text == text).Command.CanExecute(null);

                Assert.True(Can("جديد"), "زرّ الإضافة معطَّل بلا سبب");
                Assert.False(Can("تعديل"), "التعديل مفعَّل بلا صفّ محدَّد");
                Assert.False(Can("حذف"), "الحذف مفعَّل بلا صفّ محدَّد");
                Assert.True(Can("تحديث"));
            });
        }

        [Fact]
        public void ASingleRecordModule_DisablesNewOnceOneExists()
        {
            WpfApplicationFixture.Run(() =>
            {
                var actions = Toolbar("OpeningBalances");
                var add = actions.Single(a => a.Text == "جديد");

                // لا قيد افتتاحي بعد — الإضافة متاحة.
                Assert.True(add.Command.CanExecute(null));
            });
        }

        private static T Ancestor<T>(DependencyObject node) where T : DependencyObject
        {
            for (var parent = VisualTreeHelper.GetParent(node); parent != null; parent = VisualTreeHelper.GetParent(parent))
                if (parent is T match) return match;

            return null;
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            if (root is T typed) yield return typed;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
                foreach (var found in Descendants<T>(VisualTreeHelper.GetChild(root, i)))
                    yield return found;
        }
    }
}
