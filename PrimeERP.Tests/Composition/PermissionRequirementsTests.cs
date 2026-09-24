using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
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
    /// <summary>كل ما طُلب لشجرة الصلاحيات</summary>
    [Collection("WpfApplication")]
    public class PermissionRequirementsTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public PermissionRequirementsTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private (AppTreeView Tree, FrameworkElement Page) Open(string moduleKey)
        {
            UIServices.Initialize(_db.Services);
            _db.Services.GetRequiredService<IIdentityService>().Initialize();

            var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get(moduleKey);
            var page = TreeCheckListRenderer.Render(definition, _db.Services);
            page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            page.Measure(new Size(1200, 900));
            page.Arrange(new Rect(0, 0, 1200, 900));
            page.UpdateLayout();

            return (Descendants<AppTreeView>(page).Single(), page);
        }

        private static void ClickToolbar(FrameworkElement page, string text)
        {
            var button = Descendants<PrimeERP.UI.Components.Actions.AppButton>(page).Single(b => b.Text == text);
            Descendants<ButtonBase>(button).First().RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }


        [Fact]
        public void RoleScreen_MeetsEveryRequirement()
        {
            WpfApplicationFixture.Run(() =>
            {
                _db.Permissions.InsertRole("Assistant", "مساعد");
                var (tree, page) = Open("RolePermissions");

                var sales = tree.ItemsSource.Single(r => r.Id == "Sales");
                var view = sales.Children.First();
                var others = sales.Children.Skip(1).ToList();
                var print = others.Single(n => n.Id == "Sales.Print");

                Assert.Equal("Sales.View", view.Id);

                Assert.All(others, n => Assert.False(n.IsCheckEnabled));
                tree.Cycle(print);
                Assert.Equal(NodeCheckState.Unchecked, print.CheckState);

                tree.Cycle(view);
                Assert.All(others, n => Assert.True(n.IsCheckEnabled));
                Assert.All(others, n => Assert.Equal(NodeCheckState.Unchecked, n.CheckState));

                tree.Cycle(print);
                Assert.Equal(NodeCheckState.Checked, print.CheckState);
                tree.Cycle(print);
                Assert.Equal(NodeCheckState.Unchecked, print.CheckState);

                tree.Cycle(print);
                Assert.Equal(NodeCheckState.Checked, sales.CheckState);

                tree.Cycle(view);
                Assert.All(sales.Children, n => Assert.Equal(NodeCheckState.Unchecked, n.CheckState));
                Assert.All(others, n => Assert.False(n.IsCheckEnabled));
                Assert.Equal(NodeCheckState.Unchecked, sales.CheckState);

                tree.Cycle(sales);
                Assert.All(sales.Children, n => Assert.Equal(NodeCheckState.Checked, n.CheckState));

                var roots = tree.ItemsSource.ToList();
                var leaves = roots.SelectMany(r => r.Children).ToList();

                ClickToolbar(page, "إلغاء الكل");
                Assert.All(leaves, n => Assert.Equal(NodeCheckState.Unchecked, n.CheckState));
                Assert.All(roots, r => Assert.Equal(NodeCheckState.Unchecked, r.CheckState));

                ClickToolbar(page, "تحديد الكل");
                Assert.All(leaves, n => Assert.Equal(NodeCheckState.Checked, n.CheckState));
                Assert.All(roots, r => Assert.Equal(NodeCheckState.Checked, r.CheckState));
                Assert.All(leaves, n => Assert.True(n.IsCheckEnabled));
            });
        }


        [Fact]
        public void UserScreen_ShowsTheRolesRealStateAndOpensOnInheritedView()
        {
            WpfApplicationFixture.Run(() =>
            {
                var roleId = _db.Permissions.InsertRole("Full", "كامل");
                _db.Permissions.ReplaceRolePermissions(roleId, PermissionKeys.All());
                _db.AddUser("rehab", "r", "رحاب", roleId);

                var (tree, page) = Open("UserPermissions");
                var sales = tree.ItemsSource.Single(r => r.Id == "Sales");
                var view = sales.Children.First();
                var others = sales.Children.Skip(1).ToList();

                Assert.Equal(NodeCheckState.Inherited, view.CheckState);
                Assert.True(view.InheritedAllowed, "الموروث المسموح لا يُميَّز عن الممنوع");

                Assert.All(others, n => Assert.True(n.IsCheckEnabled));

                tree.Cycle(others.Single(n => n.Id == "Sales.Print"));
                Assert.Equal(NodeCheckState.Granted, others.Single(n => n.Id == "Sales.Print").CheckState);

                ClickToolbar(page, "إعادة الكل للموروث");
                Assert.All(tree.ItemsSource.SelectMany(r => r.Children),
                    n => Assert.Equal(NodeCheckState.Inherited, n.CheckState));
            });
        }

        [Fact]
        public void SwitchingTheRoleFromThePicker_KeepsTheButtonsWorking()
        {
            WpfApplicationFixture.Run(() =>
            {
                _db.Permissions.InsertRole("Admin2", "مدير");
                var assistantId = _db.Permissions.InsertRole("Assistant", "مساعد");

                var (tree, page) = Open("RolePermissions");
                var picker = Descendants<PrimeERP.UI.Components.Inputs.AppComboBox>(page).First();

                var options = ((IEnumerable<object>)picker.ItemsSource).ToList();
                var assistant = options.Single(o => (int)o.GetType().GetProperty("Id").GetValue(o) == assistantId);

                picker.SelectedItem = assistant;   // تبديل الدور كما يفعل المستخدم
                page.UpdateLayout();

                Assert.Equal(assistantId, picker.SelectedValue);

                var leaves = tree.ItemsSource.SelectMany(r => r.Children).ToList();
                Assert.NotEmpty(leaves);

                ClickToolbar(page, "تحديد الكل");
                Assert.All(leaves, n => Assert.Equal(NodeCheckState.Checked, n.CheckState));

                ClickToolbar(page, "إلغاء الكل");
                Assert.All(leaves, n => Assert.Equal(NodeCheckState.Unchecked, n.CheckState));
            });
        }

        [Fact]
        public void ARealMouseClickOnTheCheckBox_TogglesTheNode()
        {
            WpfApplicationFixture.Run(() =>
            {
                _db.Permissions.InsertRole("Assistant", "مساعد");
                var (tree, page) = Open("RolePermissions");

                var sales = tree.ItemsSource.Single(r => r.Id == "Sales");

                var box = Descendants<System.Windows.Controls.Border>(page)
                    .SingleOrDefault(b => b.Name == "checkBox" && ReferenceEquals(b.DataContext, sales));

                Assert.True(box != null, "الشجرة تعرض عُقداً غير التي يعمل عليها الكود — إسناد نفس المرجع لا يُحدّث العرض");

                Assert.True(box.IsHitTestVisible, "المربّع لا يستقبل النقر");

                box.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(
                    System.Windows.Input.Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left)
                { RoutedEvent = System.Windows.UIElement.MouseLeftButtonUpEvent, Source = box });

                Assert.Equal(NodeCheckState.Checked, sales.CheckState);
                Assert.All(sales.Children, n => Assert.Equal(NodeCheckState.Checked, n.CheckState));
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
