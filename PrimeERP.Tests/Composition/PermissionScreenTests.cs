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
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Tree;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>الشاشة نفسها لا التعريف</summary>
    [Collection("WpfApplication")]
    public class PermissionScreenTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public PermissionScreenTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void TheScreen_ShowsTheSelectedRolesPermissionsAsChecked()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();

                var roleId = _db.Permissions.InsertRole("Full", "كامل");
                _db.Permissions.ReplaceRolePermissions(roleId, PermissionKeys.All());

                var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get("RolePermissions");
                var page = TreeCheckListRenderer.Render(definition, _db.Services);
                page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                page.Measure(new Size(1200, 800));
                page.Arrange(new Rect(0, 0, 1200, 800));
                page.UpdateLayout();

                var tree = Descendants<AppTreeView>(page).Single();
                var roots = tree.ItemsSource?.ToList() ?? new List<TreeNodeViewModel>();

                Assert.NotEmpty(roots);
                var leaves = roots.SelectMany(r => r.Children).ToList();
                Assert.NotEmpty(leaves);
                Assert.All(leaves, node => Assert.Equal(NodeCheckState.Checked, node.CheckState));
            });
        }

        [Fact]
        public void CheckingAnAction_ChecksTheModulesViewInTheTree()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();
                _db.Permissions.InsertRole("Assistant", "مساعد");

                var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get("RolePermissions");
                var page = TreeCheckListRenderer.Render(definition, _db.Services);
                page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                page.Measure(new Size(1200, 800));
                page.Arrange(new Rect(0, 0, 1200, 800));
                page.UpdateLayout();

                var tree = Descendants<AppTreeView>(page).Single();
                var leaves = tree.ItemsSource.SelectMany(r => r.Children).ToList();

                var sales = tree.ItemsSource.Single(r => r.Id == "Sales");
                var view = sales.Children.First();
                var print = sales.Children.Single(n => n.Id == "Sales.Print");

                Assert.Equal("Sales.View", view.Id);              // العرض أول بند في القسم
                Assert.False(print.IsCheckEnabled);               // الباقي معطَّل بلا عرض

                tree.Cycle(print);                                // نقرة لا أثر لها
                Assert.Equal(NodeCheckState.Unchecked, print.CheckState);

                tree.Cycle(view);                                 // تأشير العرض يفتح الباقي
                Assert.True(print.IsCheckEnabled);
                Assert.Equal(NodeCheckState.Checked, sales.CheckState);

                tree.Cycle(print);
                Assert.Equal(NodeCheckState.Checked, print.CheckState);

                tree.Cycle(view);                                 // رفع العرض يُنزل إجراءات قسمه ويعطّلها
                Assert.Equal(NodeCheckState.Unchecked, print.CheckState);
                Assert.False(print.IsCheckEnabled);
                Assert.Equal(NodeCheckState.Unchecked, sales.CheckState);
            });
        }

        [Fact]
        public void SelectAllThenClearAll_ReachEveryNodeIncludingSections()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();
                _db.Permissions.InsertRole("Assistant", "مساعد");

                var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get("RolePermissions");
                var page = TreeCheckListRenderer.Render(definition, _db.Services);
                page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                page.Measure(new Size(1200, 800));
                page.Arrange(new Rect(0, 0, 1200, 800));
                page.UpdateLayout();

                var tree = Descendants<AppTreeView>(page).Single();
                var buttons = Descendants<PrimeERP.UI.Components.Actions.AppButton>(page).ToList();

                void Click(string text) => Descendants<System.Windows.Controls.Button>(
                    buttons.Single(b => b.Text == text)).First().RaiseEvent(
                        new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

                var roots = tree.ItemsSource.ToList();
                var leaves = roots.SelectMany(r => r.Children).ToList();

                Click("تحديد الكل");
                Assert.All(leaves, n => Assert.Equal(NodeCheckState.Checked, n.CheckState));
                Assert.All(roots, r => Assert.Equal(NodeCheckState.Checked, r.CheckState));
                Assert.All(leaves, n => Assert.True(n.IsCheckEnabled));

                Click("إلغاء الكل");
                Assert.All(leaves, n => Assert.Equal(NodeCheckState.Unchecked, n.CheckState));
                Assert.All(roots, r => Assert.Equal(NodeCheckState.Unchecked, r.CheckState));
            });
        }

        [Fact]
        public void CheckingView_OnlyEnablesTheOthers_AndClearingItClearsThemAll()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();
                _db.Permissions.InsertRole("Assistant", "مساعد");

                var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get("RolePermissions");
                var page = TreeCheckListRenderer.Render(definition, _db.Services);
                page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                page.Measure(new Size(1200, 800));
                page.Arrange(new Rect(0, 0, 1200, 800));
                page.UpdateLayout();

                var tree = Descendants<AppTreeView>(page).Single();
                var sales = tree.ItemsSource.Single(r => r.Id == "Sales");
                var view = sales.Children.First();
                var others = sales.Children.Skip(1).ToList();

                tree.Cycle(view);

                Assert.True(others.All(n => n.IsCheckEnabled), "الأزرار لم تُفتح بعد تأشير العرض");
                Assert.All(others, n => Assert.Equal(NodeCheckState.Unchecked, n.CheckState));

                var print = others.Single(n => n.Id == "Sales.Print");
                tree.Cycle(print);
                Assert.Equal(NodeCheckState.Checked, print.CheckState);
                tree.Cycle(print);
                Assert.Equal(NodeCheckState.Unchecked, print.CheckState);

                tree.Cycle(print);
                tree.Cycle(others.Single(n => n.Id == "Sales.Create"));
                tree.Cycle(view);   // إلغاء العرض يلغي الكل

                Assert.All(sales.Children, n => Assert.Equal(NodeCheckState.Unchecked, n.CheckState));
                Assert.All(others, n => Assert.False(n.IsCheckEnabled));
            });
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
