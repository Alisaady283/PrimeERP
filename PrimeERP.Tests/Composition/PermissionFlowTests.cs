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
    /// <summary>دورة الصلاحيات كما يعيشها المستخدم</summary>
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

        [Theory]
        [InlineData("GoodsReceipt")]
        [InlineData("GoodsIssue")]
        [InlineData("DeliveryNote")]
        [InlineData("SalesReceipt")]
        public void AStockVoucher_OpensForARoleGrantedIt(string moduleKey)
        {
            var roleId = _db.Permissions.InsertRole($"Keeper{moduleKey}", "أمين مخزن");
            var userId = _db.AddUser($"keeper{moduleKey}", "p", "أمين", roleId);

            _db.Permissions.ReplaceRolePermissions(roleId,
                PermissionKeys.All().Where(key => key.StartsWith("Inventory.")).ToList());
            _db.Services.GetRequiredService<IPermissionService>().LoadForUser(userId);

            var serviceType = _db.Services.GetRequiredService<IModuleRegistry>()
                .Get(moduleKey).DocumentDialog.ServiceType;
            var service = (dynamic)_db.Services.GetRequiredService(serviceType);

            var result = service.GetPaged(1, 20, null);

            Assert.True(result.IsSuccess, $"{moduleKey}: {result.ErrorMessage}");
            Assert.NotEqual(PrimeERP.Domain.Results.ErrorCode.Unauthorized, result.ErrorCode);
        }

        [Theory]
        [InlineData("Inventory.GoodsReceipt")]
        [InlineData("Inventory.GoodsIssue")]
        [InlineData("Inventory.DeliveryNote")]
        [InlineData("Inventory.SalesReceipt")]
        public void AStockVoucherKey_AppearsInTheRoleScreen(string key)
        {
            var roleId = _db.Permissions.InsertRole($"Empty{key}", "فارغ");

            var ids = PermissionTreeFactory.KeyNodes(RoleScreen().BuildTree(_db.Services, roleId))
                .Select(node => node.Id).ToList();

            Assert.Contains(key, ids);
        }

        [Fact]
        public void ARoleWithPermissions_OpensWithThemChecked()
        {
            var roleId = _db.Permissions.InsertRole("Full", "كامل");
            _db.Permissions.ReplaceRolePermissions(roleId, PermissionKeys.All());

            var keys = PermissionTreeFactory.KeyNodes(RoleScreen().BuildTree(_db.Services, roleId)).ToList();

            Assert.NotEmpty(keys);
            Assert.All(keys, node => Assert.Equal(NodeCheckState.Checked, node.CheckState));
        }

        [Fact]
        public void CheckingAnActionWithoutView_StillOpensTheSection()
        {
            var roleId = _db.Permissions.InsertRole("Printer", "طابع");
            var userId = _db.AddUser("printer", "p", "طابع", roleId);

            var screen = RoleScreen();
            var nodes = screen.BuildTree(_db.Services, roleId);

            foreach (var node in PermissionTreeFactory.KeyNodes(nodes))
                if (node.Id == "Sales.Print") node.CheckState = NodeCheckState.Checked;

            Assert.True(screen.Save(_db.Services, roleId, nodes).Result.IsSuccess);

            var permissions = _db.Services.GetRequiredService<IPermissionService>();
            permissions.LoadForUser(userId);

            Assert.True(permissions.Can("Sales.Print"));
            Assert.True(permissions.Can("Sales.View"), "الطباعة مُنحت والعرض لم يلحق بها");

            Assert.False(permissions.Can("Purchases.View"));
        }

        [Fact]
        public void GrantingARole_Persists_AndReachesTheUsersSession()
        {
            var roleId = _db.Permissions.InsertRole("Assistant", "مساعد");
            var userId = _db.AddUser("rehab", "rehab", "رحاب", roleId);

            var screen = RoleScreen();
            var nodes = screen.BuildTree(_db.Services, roleId);

            foreach (var node in PermissionTreeFactory.KeyNodes(nodes))
                if (node.Id == "Journal.View" || node.Id.StartsWith("Sales."))
                    node.CheckState = NodeCheckState.Checked;

            Assert.True(screen.Save(_db.Services, roleId, nodes).Result.IsSuccess);

            var reopened = PermissionTreeFactory.KeyNodes(screen.BuildTree(_db.Services, roleId))
                .Where(n => n.CheckState == NodeCheckState.Checked)
                .Select(n => n.Id).ToHashSet();

            Assert.Contains("Journal.View", reopened);
            Assert.Contains("Sales.View", reopened);

            var permissions = _db.Services.GetRequiredService<IPermissionService>();
            permissions.LoadForUser(userId);

            Assert.True(permissions.Can("Journal.View"), "قيود اليومية محجوبة رغم منحها");
            Assert.True(permissions.Can("Sales.View"), "المبيعات محجوبة رغم منحها");
            Assert.False(permissions.Can("Settings.System"), "صلاحية غير ممنوحة صارت مسموحة");
        }
    }
}
