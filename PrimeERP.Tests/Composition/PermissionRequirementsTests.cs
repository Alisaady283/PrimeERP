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
    /// <summary>كل ما طُلب لشجرة الصلاحيات، مقيساً من الشاشة المرسومة لا من التعريف.</summary>
    [Collection("WpfApplication")]
    public class PermissionRequirementsTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public PermissionRequirementsTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private (AppTreeView Tree, FrameworkElement Page) Open(string moduleKey)
        {
            UIServices.Initialize(_db.Services);
            _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

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

        // ===================== شاشة الأدوار =====================

        [Fact]
        public void RoleScreen_MeetsEveryRequirement()
        {
            WpfApplicationFixture.Run(() =>
            {
                PermissionDb.InsertRole("Assistant", "مساعد");
                var (tree, page) = Open("RolePermissions");

                var sales = tree.ItemsSource.Single(r => r.Id == "Sales");
                var view = sales.Children.First();
                var others = sales.Children.Skip(1).ToList();
                var print = others.Single(n => n.Id == "Sales.Print");

                // (١) «عرض» أول بند في القسم
                Assert.Equal("Sales.View", view.Id);

                // (٢) بلا «عرض» الأزرار في وضع المنع، والنقر بلا أثر
                Assert.All(others, n => Assert.False(n.IsCheckEnabled));
                tree.Cycle(print);
                Assert.Equal(NodeCheckState.Unchecked, print.CheckState);

                // (٣) «عرض» يفتح وضع السماح ولا يؤشّر شيئاً
                tree.Cycle(view);
                Assert.All(others, n => Assert.True(n.IsCheckEnabled));
                Assert.All(others, n => Assert.Equal(NodeCheckState.Unchecked, n.CheckState));

                // (٤) التأشير والإلغاء بحرية لكل إجراء على حدة
                tree.Cycle(print);
                Assert.Equal(NodeCheckState.Checked, print.CheckState);
                tree.Cycle(print);
                Assert.Equal(NodeCheckState.Unchecked, print.CheckState);

                // (٥) تأشير ابن يؤشّر الأب
                tree.Cycle(print);
                Assert.Equal(NodeCheckState.Checked, sales.CheckState);

                // (٦) إلغاء «عرض» يفرّغ الكل ويمنع
                tree.Cycle(view);
                Assert.All(sales.Children, n => Assert.Equal(NodeCheckState.Unchecked, n.CheckState));
                Assert.All(others, n => Assert.False(n.IsCheckEnabled));
                Assert.Equal(NodeCheckState.Unchecked, sales.CheckState);

                // (٧) تأشير الأب يؤشّر كل الأبناء
                tree.Cycle(sales);
                Assert.All(sales.Children, n => Assert.Equal(NodeCheckState.Checked, n.CheckState));

                // (٨) زرّا «تحديد الكل» و«إلغاء الكل»
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

        // ===================== شاشة المستخدمين =====================

        [Fact]
        public void UserScreen_ShowsTheRolesRealStateAndOpensOnInheritedView()
        {
            WpfApplicationFixture.Run(() =>
            {
                var roleId = PermissionDb.InsertRole("Full", "كامل");
                PermissionDb.ReplaceRolePermissions(roleId, PermissionKeys.All());
                PermissionDb.InsertUser("rehab", "r", "رحاب", roleId, true);

                var (tree, page) = Open("UserPermissions");
                var sales = tree.ItemsSource.Single(r => r.Id == "Sales");
                var view = sales.Children.First();
                var others = sales.Children.Skip(1).ToList();

                // (٩) الموروث المسموح يظهر كمسموح لا كفارغ — ولا يُحوَّل لمنح صريح
                Assert.Equal(NodeCheckState.Inherited, view.CheckState);
                Assert.True(view.InheritedAllowed, "الموروث المسموح لا يُميَّز عن الممنوع");

                // (١٠) «عرض» الموروث المسموح يفتح باقي الإجراءات
                Assert.All(others, n => Assert.True(n.IsCheckEnabled));

                // (١١) «إعادة الكل للموروث» يعمل
                tree.Cycle(others.Single(n => n.Id == "Sales.Print"));
                Assert.Equal(NodeCheckState.Granted, others.Single(n => n.Id == "Sales.Print").CheckState);

                ClickToolbar(page, "إعادة الكل للموروث");
                Assert.All(tree.ItemsSource.SelectMany(r => r.Children),
                    n => Assert.Equal(NodeCheckState.Inherited, n.CheckState));
            });
        }

        /// <summary>ما يفعله المستخدم فعلاً: يبدّل الدور من القائمة ثم يضغط الأزرار.</summary>
        [Fact]
        public void SwitchingTheRoleFromThePicker_KeepsTheButtonsWorking()
        {
            WpfApplicationFixture.Run(() =>
            {
                PermissionDb.InsertRole("Admin2", "مدير");
                var assistantId = PermissionDb.InsertRole("Assistant", "مساعد");

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

        /// <summary>نقرة فأرة حقيقية على مربّع القسم — نفس معالج النقر الذي يستدعيه المستخدم.</summary>
        [Fact]
        public void ARealMouseClickOnTheCheckBox_TogglesTheNode()
        {
            WpfApplicationFixture.Run(() =>
            {
                PermissionDb.InsertRole("Assistant", "مساعد");
                var (tree, page) = Open("RolePermissions");

                var sales = tree.ItemsSource.Single(r => r.Id == "Sales");

                // الشرط الحاسم: العقدة المعروضة في الشجرة هي نفس العقدة التي تعمل عليها الأزرار والحفظ.
                var box = Descendants<System.Windows.Controls.Border>(page)
                    .SingleOrDefault(b => b.Name == "checkBox" && ReferenceEquals(b.DataContext, sales));

                Assert.True(box != null, "الشجرة تعرض عُقداً غير التي يعمل عليها الكود — إسناد نفس المرجع لا يُحدّث العرض");

                Assert.True(box.IsHitTestVisible, "المربّع لا يستقبل النقر");

                box.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(
                    System.Windows.Input.Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left)
                { RoutedEvent = System.Windows.UIElement.MouseLeftButtonUpEvent, Source = box });

                // النقرة تمرّ بالمعالج فتؤشّر القسم وكل أبنائه
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
