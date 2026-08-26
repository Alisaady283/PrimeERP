using System;
using System.Windows.Controls;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>NavigationService لم تكن مُختبرة إطلاقاً رغم كونها حية (مسجَّلة بالـDI ومُستهلَكة من
    /// AppShell/AppSidebar). UserControl حقيقي يحتاج خيط STA — WpfApplicationFixture نفس نمط بقية اختبارات
    /// WPF في المشروع.</summary>
    [Collection("WpfApplication")]
    public class NavigationServiceTests
    {
        private class FakePermissionService : IPermissionService
        {
            public bool Allowed = true;
            public bool Can(string key) => Allowed;
            public bool CanAny(params string[] keys) => Allowed;
            public bool CanAll(params string[] keys) => Allowed;
            public void LoadForUser(int userId) { }
            public System.Collections.Generic.IEnumerable<string> GetUserPermissions(int userId) => Array.Empty<string>();
        }

        [Fact]
        public void NavigateTo_UnregisteredKey_Throws()
        {
            var service = new NavigationService(new FakePermissionService());
            Assert.Throws<InvalidOperationException>(() => service.NavigateTo("NoSuchPage"));
        }

        [Fact]
        public void NavigateTo_RegisteredKeyWithPermission_SetsCurrentPageAndRaisesEvent()
        {
            WpfApplicationFixture.Run(() =>
            {
                var service = new NavigationService(new FakePermissionService { Allowed = true });
                var page = new UserControl();
                service.RegisterPage("Customers", () => page);

                bool raised = false;
                service.CurrentPageChanged += (s, e) => raised = true;

                service.NavigateTo("Customers");

                Assert.Same(page, service.CurrentPage);
                Assert.True(raised);
            });
        }

        [Fact]
        public void NavigateTo_WithoutPermission_DoesNotSetCurrentPage()
        {
            WpfApplicationFixture.Run(() =>
            {
                var permissions = new FakePermissionService { Allowed = false };
                var service = new NavigationService(permissions);
                service.RegisterPage("Customers", () => new UserControl());

                service.NavigateTo("Customers");

                Assert.Null(service.CurrentPage);
            });
        }

        [Fact]
        public void CanNavigateTo_ChecksKeyDotViewPermission()
        {
            var permissions = new FakePermissionService { Allowed = true };
            var service = new NavigationService(permissions);

            Assert.True(service.CanNavigateTo("Accounts"));

            permissions.Allowed = false;
            Assert.False(service.CanNavigateTo("Accounts"));
        }
    }
}
