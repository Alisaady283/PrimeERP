using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Tree;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>
    /// دورة الصلاحيات كما يعيشها المستخدم: يفتح الشاشة فيرى ما هو ممنوح، يمنح، يحفظ، يدخل المستخدم
    /// فيجد أقسامه. كل حلقة مكسورة هنا تظهر كسايد بار فارغ أو شجرة بلا تحديد.
    /// </summary>
    public class PermissionFlowTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public PermissionFlowTests() => AppSession.DevMode = false;

        public void Dispose()
        {
            AppSession.DevMode = true;
            _db.Dispose();
        }

        private TreeCheckListDefinition RoleScreen() =>
            _db.Services.GetRequiredService<IModuleRegistry>().Get("RolePermissions").TreeCheckList;

        [Fact]
        public void ARoleWithPermissions_OpensWithThemChecked()
        {
            var roleId = PermissionDb.InsertRole("Full", "كامل");
            PermissionDb.ReplaceRolePermissions(roleId, PermissionKeys.All());

            var keys = PermissionTreeFactory.KeyNodes(RoleScreen().BuildTree(_db.Services, roleId)).ToList();

            Assert.NotEmpty(keys);
            Assert.All(keys, node => Assert.Equal(NodeCheckState.Checked, node.CheckState));
        }

        [Fact]
        public void CheckingAnActionWithoutView_StillOpensTheSection()
        {
            var roleId = PermissionDb.InsertRole("Printer", "طابع");
            var userId = PermissionDb.InsertUser("printer", "p", "طابع", roleId, true);

            var screen = RoleScreen();
            var nodes = screen.BuildTree(_db.Services, roleId);

            // طباعة بلا عرض: القسم محجوب فلا شيء يُطبع — العرض بوّابة لباقي الإجراءات.
            foreach (var node in PermissionTreeFactory.KeyNodes(nodes))
                if (node.Id == "Sales.Print") node.CheckState = NodeCheckState.Checked;

            Assert.True(screen.Save(_db.Services, roleId, nodes).Result.IsSuccess);

            var permissions = _db.Services.GetRequiredService<IPermissionService>();
            permissions.LoadForUser(userId);

            Assert.True(permissions.Can("Sales.Print"));
            Assert.True(permissions.Can("Sales.View"), "الطباعة مُنحت والعرض لم يلحق بها");

            // ولا تتسرّب لوحدة أخرى.
            Assert.False(permissions.Can("Purchases.View"));
        }

        [Fact]
        public void GrantingARole_Persists_AndReachesTheUsersSession()
        {
            var roleId = PermissionDb.InsertRole("Assistant", "مساعد");
            var userId = PermissionDb.InsertUser("rehab", "rehab", "رحاب", roleId, true);

            var screen = RoleScreen();
            var nodes = screen.BuildTree(_db.Services, roleId);

            // ما فعله المستخدم بالضبط: عرض قيود اليومية + قسم المبيعات كاملاً.
            foreach (var node in PermissionTreeFactory.KeyNodes(nodes))
                if (node.Id == "Journal.View" || node.Id.StartsWith("Sales."))
                    node.CheckState = NodeCheckState.Checked;

            Assert.True(screen.Save(_db.Services, roleId, nodes).Result.IsSuccess);

            // ١) تُقرأ عند إعادة فتح الشاشة
            var reopened = PermissionTreeFactory.KeyNodes(screen.BuildTree(_db.Services, roleId))
                .Where(n => n.CheckState == NodeCheckState.Checked)
                .Select(n => n.Id).ToHashSet();

            Assert.Contains("Journal.View", reopened);
            Assert.Contains("Sales.View", reopened);

            // ٢) تصل جلسة المستخدم بعد تسجيل الدخول
            var permissions = _db.Services.GetRequiredService<IPermissionService>();
            permissions.LoadForUser(userId);

            Assert.True(permissions.Can("Journal.View"), "قيود اليومية محجوبة رغم منحها");
            Assert.True(permissions.Can("Sales.View"), "المبيعات محجوبة رغم منحها");
            Assert.False(permissions.Can("Settings.System"), "صلاحية غير ممنوحة صارت مسموحة");
        }
    }
}
