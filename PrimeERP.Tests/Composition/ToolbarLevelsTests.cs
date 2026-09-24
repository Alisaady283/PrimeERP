using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
    /// <summary>مستويا الإجراءات</summary>
    [Collection("WpfApplication")]
    public class ToolbarLevelsTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public ToolbarLevelsTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private FrameworkElement ToolbarOf(string moduleKey)
        {
            UIServices.Initialize(_db.Services);
            _db.Services.GetRequiredService<IIdentityService>().Initialize();

            var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get(moduleKey);
            var page = CrudPageRenderer.Render(definition, _db.Services);
            page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            page.Measure(new Size(1200, 800));
            page.Arrange(new Rect(0, 0, 1200, 800));
            page.UpdateLayout();

            return page;
        }

        private static List<string> LabelsOf(FrameworkElement page, bool documentLevel)
        {
            var toolbars = FindAll<ActionToolbar>(page).ToList();
            var host = documentLevel
                ? toolbars.FirstOrDefault(t => FindAncestor<PrimeERP.UI.Components.Layout.FilterBar>(t) != null)
                : toolbars.FirstOrDefault(t => FindAncestor<PrimeERP.UI.Components.Layout.FilterBar>(t) == null);

            return host?.ButtonsSource?.Select(a => a.Text).ToList() ?? new List<string>();
        }

        private static T FindAncestor<T>(DependencyObject node) where T : DependencyObject
        {
            for (var parent = System.Windows.Media.VisualTreeHelper.GetParent(node); parent != null;
                 parent = System.Windows.Media.VisualTreeHelper.GetParent(parent))
                if (parent is T match) return match;

            return null;
        }

        private static IEnumerable<T> FindAll<T>(DependencyObject root) where T : DependencyObject
        {
            if (root is T typed) yield return typed;

            for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
                foreach (var found in FindAll<T>(System.Windows.Media.VisualTreeHelper.GetChild(root, i)))
                    yield return found;
        }

        [Fact]
        public void ADocumentModuleShowsBothLevelsSeparated()
        {
            WpfApplicationFixture.Run(() =>
            {
                var page = ToolbarOf("SalesInvoices");

                var report = LabelsOf(page, documentLevel: false);
                Assert.Contains("طباعة التقرير", report);
                Assert.Contains("تصدير التقرير", report);
                Assert.DoesNotContain("طباعة المستند", report);

                var document = LabelsOf(page, documentLevel: true);
                Assert.Equal(new[] { "طباعة المستند", "تصدير المستند" }, document);
            });
        }

        [Fact]
        public void APlainListHasOnlyTheReportLevel()
        {
            WpfApplicationFixture.Run(() =>
            {
                var page = ToolbarOf("Customers");

                Assert.Contains("طباعة التقرير", LabelsOf(page, documentLevel: false));
                Assert.Empty(LabelsOf(page, documentLevel: true));
            });
        }

        [Fact]
        public void DocumentLevelActionsAreDisabledUntilARowIsSelected()
        {
            WpfApplicationFixture.Run(() =>
            {
                var toolbars = FindAll<ActionToolbar>(ToolbarOf("SalesInvoices"))
                    .First(t => FindAncestor<PrimeERP.UI.Components.Layout.FilterBar>(t) != null);

                var print = toolbars.ButtonsSource.First(a => a.Text == "طباعة المستند");
                Assert.False(print.Command.CanExecute(null));
            });
        }

    }
}
