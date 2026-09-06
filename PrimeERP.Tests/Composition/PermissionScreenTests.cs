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
    /// <summary>الشاشة نفسها لا التعريف: هل يصل المعرّف المختار للبناء فيظهر التأشير فعلاً؟</summary>
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
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var roleId = PermissionDb.InsertRole("Full", "كامل");
                PermissionDb.ReplaceRolePermissions(roleId, PermissionKeys.All());

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

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            if (root is T typed) yield return typed;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
                foreach (var found in Descendants<T>(VisualTreeHelper.GetChild(root, i)))
                    yield return found;
        }
    }
}
