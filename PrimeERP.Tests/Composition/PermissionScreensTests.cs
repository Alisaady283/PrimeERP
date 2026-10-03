using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Modules;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Tree;
using Xunit;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Tests.Composition
{
    /// <summary>شاشتا الصلاحيات تكوين فوق TreeCheckListRenderer</summary>
    [Collection("Database")]
    public class PermissionScreensTests
    {
        private readonly TestDatabaseFixture _db;
        private readonly IModuleRegistry _registry = new ModuleRegistry();

        public PermissionScreensTests(TestDatabaseFixture db)
        {
            _db = db;
            PermissionModuleRegistrations.RegisterAll(_registry);
        }

        private int CreateRole(string name, params string[] permissions)
        {
            var roleId = _db.Permissions.InsertRole(name, name);
            _db.Permissions.ReplaceRolePermissions(roleId, permissions);
            return roleId;
        }

        private int CreateUser(string username, int roleId) => _db.AddUser(username, "x", username, roleId);

        [Fact]
        public void PermissionTree_IsBuiltFromPermissionKeys_NotAHandWrittenList()
        {
            var roots = PermissionTreeFactory.Build();
            var keys = PermissionTreeFactory.KeyNodes(roots).Select(n => n.Id).ToHashSet();

            Assert.Equal(PermissionKeys.All().Distinct().Count(), keys.Count);
            Assert.Contains(PermissionKeys.Sales.Post, keys);
            Assert.Contains(PermissionKeys.Products.ColumnCostPrice, keys);
            Assert.All(roots, r => Assert.False(r.IsCheckable));
        }

        [Fact]
        public void RolePermissions_LoadsCurrentGrants_AndSaveReplacesThem()
        {
            var roleId = CreateRole($"Role.{Guid.NewGuid():N}", "Customers.View", "Customers.Edit");
            var def = _registry.Get("RolePermissions").TreeCheckList;

            var nodes = def.BuildTree(_db.Services, roleId);
            var keyNodes = PermissionTreeFactory.KeyNodes(nodes).ToList();

            Assert.Equal(NodeCheckState.Checked, keyNodes.First(n => n.Id == "Customers.View").CheckState);
            Assert.Equal(NodeCheckState.Unchecked, keyNodes.First(n => n.Id == "Customers.Delete").CheckState);

            keyNodes.First(n => n.Id == "Customers.View").CheckState = NodeCheckState.Unchecked;
            keyNodes.First(n => n.Id == "Customers.Delete").CheckState = NodeCheckState.Checked;

            Assert.True(def.Save(_db.Services, roleId, nodes).Result.IsSuccess);

            var saved = _db.Permissions.GetRolePermissions(roleId);
            Assert.Contains("Customers.Delete", saved);
            Assert.Contains("Customers.Edit", saved);

            Assert.Contains("Customers.View", saved);
        }

        [Fact]
        public void UserPermissions_MapsThreeStates_AndSavePersistsOverrides()
        {
            var roleId = CreateRole($"Role.{Guid.NewGuid():N}", "Suppliers.View");
            var userId = CreateUser($"user.{Guid.NewGuid():N}", roleId);
            var def = _registry.Get("UserPermissions").TreeCheckList;

            var nodes = def.BuildTree(_db.Services, userId);
            var keyNodes = PermissionTreeFactory.KeyNodes(nodes).ToList();

            var inherited = keyNodes.First(n => n.Id == "Suppliers.View");
            Assert.Equal(NodeCheckState.Inherited, inherited.CheckState);
            Assert.Equal(LocalizationService.Get("Str.Permissions.FromRole"), inherited.InheritedHint);

            keyNodes.First(n => n.Id == "Suppliers.View").CheckState = NodeCheckState.Revoked;
            keyNodes.First(n => n.Id == "Products.Create").CheckState = NodeCheckState.Granted;

            Assert.True(def.Save(_db.Services, userId, nodes).Result.IsSuccess);

            var admin = _db.Services.GetRequiredService<IPermissionAdminService>();
            Assert.Equal(PermissionState.Revoked, admin.GetState(userId, "Suppliers.View"));
            Assert.Equal(PermissionState.Granted, admin.GetState(userId, "Products.Create"));

            var effective = admin.GetEffectivePermissions(userId);
            Assert.DoesNotContain("Suppliers.View", effective);
            Assert.Contains("Products.Create", effective);
            Assert.Contains("Products.View", effective);
        }
    }
}
