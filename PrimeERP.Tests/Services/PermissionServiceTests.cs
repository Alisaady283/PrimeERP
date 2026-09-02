using System;
using PrimeERP.Platform.Permissions;
using Xunit;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Tests.Services
{
    /// <summary>يختبر PermissionService.Can/CanAny/CanAll/LoadForUser/GetUserPermissions مباشرة — كانت
    /// مبنية بالكامل بلا اختبار مباشر (فقط PermissionDb الخام مُستهلكة عبر TestDatabaseFixture). DevMode
    /// يُعطَّل صراحة هنا (نفس نمط JournalServiceTests/CustomerServiceTests) لأنه يتخطّى كل هذا المنطق.</summary>
    [Collection("Database")]
    public class PermissionServiceTests
    {
        private readonly TestDatabaseFixture _db;
        private readonly PermissionService _service = new();

        public PermissionServiceTests(TestDatabaseFixture db) => _db = db;

        private static int CreateRole(string name, params string[] permissions)
        {
            var roleId = Db.InsertAndGetId(
                "INSERT INTO Roles (Name, NameAr, IsSystem) VALUES (@n, @na, @sys)",
                Db.Params(("@n", name), ("@na", name), ("@sys", false)));

            foreach (var key in permissions)
                Db.Execute("INSERT INTO RolePermissions (RoleId, PermissionKey) VALUES (@r, @k)",
                    Db.Params(("@r", roleId), ("@k", key)));

            return roleId;
        }

        private static int CreateUser(string username, int roleId)
        {
            var (hash, salt) = PrimeERP.Platform.Security.PasswordHasher.Hash("x");
            return Db.InsertAndGetId(
                @"INSERT INTO Users (Username, PasswordHash, Salt, DisplayName, RoleId, IsActive)
                  VALUES (@u, @h, @s, @d, @r, @a)",
                Db.Params(("@u", username), ("@h", hash), ("@s", salt), ("@d", username), ("@r", roleId), ("@a", true)));
        }

        [Fact]
        public void GetUserPermissions_ReturnsRolePermissionsOnly_WhenNoUserOverrides()
        {
            var roleId = CreateRole($"Role.{Guid.NewGuid():N}", "Customers.View", "Customers.Create");
            var userId = CreateUser($"user.{Guid.NewGuid():N}", roleId);

            var perms = _service.GetUserPermissions(userId);

            Assert.Contains("Customers.View", perms);
            Assert.Contains("Customers.Create", perms);
        }

        [Fact]
        public void GetUserPermissions_AddsUserGrantedPermission_NotInRole()
        {
            var roleId = CreateRole($"Role.{Guid.NewGuid():N}", "Customers.View");
            var userId = CreateUser($"user.{Guid.NewGuid():N}", roleId);

            Db.Execute("INSERT INTO UserPermissions (UserId, PermissionKey, IsGranted) VALUES (@u, @k, 1)",
                Db.Params(("@u", userId), ("@k", "Suppliers.View")));

            var perms = _service.GetUserPermissions(userId);

            Assert.Contains("Customers.View", perms);
            Assert.Contains("Suppliers.View", perms);
        }

        [Fact]
        public void GetUserPermissions_RevokedOverridesRolePermission()
        {
            var roleId = CreateRole($"Role.{Guid.NewGuid():N}", "Customers.View", "Customers.Delete");
            var userId = CreateUser($"user.{Guid.NewGuid():N}", roleId);

            Db.Execute("INSERT INTO UserPermissions (UserId, PermissionKey, IsGranted) VALUES (@u, @k, 0)",
                Db.Params(("@u", userId), ("@k", "Customers.Delete")));

            var perms = _service.GetUserPermissions(userId);

            Assert.Contains("Customers.View", perms);
            Assert.DoesNotContain("Customers.Delete", perms);
        }

        [Fact]
        public void LoadForUser_PopulatesAppSessionPermissions()
        {
            var roleId = CreateRole($"Role.{Guid.NewGuid():N}", "Accounts.View");
            var userId = CreateUser($"user.{Guid.NewGuid():N}", roleId);

            AppSession.DevMode = false;
            try
            {
                _service.LoadForUser(userId);

                Assert.Contains("Accounts.View", AppSession.Permissions);
                Assert.True(_service.Can("Accounts.View"));
                Assert.False(_service.Can("Accounts.Delete"));
            }
            finally
            {
                AppSession.DevMode = true;
                AppSession.Permissions.Clear();
            }
        }

        [Fact]
        public void CanAny_TrueIfAtLeastOneKeyGranted()
        {
            AppSession.DevMode = false;
            try
            {
                AppSession.Permissions.Clear();
                AppSession.Permissions.Add("Customers.View");

                Assert.True(_service.CanAny("Customers.Delete", "Customers.View"));
                Assert.False(_service.CanAny("Customers.Delete", "Customers.Create"));
            }
            finally
            {
                AppSession.DevMode = true;
                AppSession.Permissions.Clear();
            }
        }

        [Fact]
        public void CanAll_FalseIfAnyKeyMissing()
        {
            AppSession.DevMode = false;
            try
            {
                AppSession.Permissions.Clear();
                AppSession.Permissions.Add("Customers.View");
                AppSession.Permissions.Add("Customers.Create");

                Assert.True(_service.CanAll("Customers.View", "Customers.Create"));
                Assert.False(_service.CanAll("Customers.View", "Customers.Delete"));
            }
            finally
            {
                AppSession.DevMode = true;
                AppSession.Permissions.Clear();
            }
        }

        [Fact]
        public void Can_AlwaysTrue_WhenDevModeEnabled()
        {
            AppSession.DevMode = true;
            AppSession.Permissions.Clear();

            Assert.True(_service.Can("Anything.NotGranted"));
        }

        [Fact]
        public void SetUserPermission_RevokedBeatsRoleGrant_AndInheritedRemovesOverride()
        {
            var roleId = CreateRole($"Role.{Guid.NewGuid():N}", "Customers.View", "Customers.Edit");
            var userId = CreateUser($"user.{Guid.NewGuid():N}", roleId);

            _service.SetUserPermission(userId, "Customers.Edit", PermissionState.Revoked);

            Assert.Equal(PermissionState.Revoked, _service.GetState(userId, "Customers.Edit"));
            Assert.DoesNotContain("Customers.Edit", _service.GetEffectivePermissions(userId));
            Assert.Contains("Customers.View", _service.GetEffectivePermissions(userId));
            Assert.True(_service.IsInheritedFromRole(userId, "Customers.Edit"));

            _service.SetUserPermission(userId, "Customers.Edit", PermissionState.Inherited);

            Assert.Equal(PermissionState.Inherited, _service.GetState(userId, "Customers.Edit"));
            Assert.Contains("Customers.Edit", _service.GetEffectivePermissions(userId));
        }

        [Fact]
        public void SetUserPermission_GrantedAddsKeyOutsideRole()
        {
            var roleId = CreateRole($"Role.{Guid.NewGuid():N}", "Customers.View");
            var userId = CreateUser($"user.{Guid.NewGuid():N}", roleId);

            _service.SetUserPermission(userId, "Suppliers.Delete", PermissionState.Granted);

            Assert.Equal(PermissionState.Granted, _service.GetState(userId, "Suppliers.Delete"));
            Assert.False(_service.IsInheritedFromRole(userId, "Suppliers.Delete"));
            Assert.Contains("Suppliers.Delete", _service.GetEffectivePermissions(userId));
        }

        [Fact]
        public void SetRolePermissions_ReplacesWholesale_AndCopyClonesAnotherRole()
        {
            var source = CreateRole($"Role.{Guid.NewGuid():N}", "Products.View", "Products.Create");
            var target = CreateRole($"Role.{Guid.NewGuid():N}", "Journal.View");
            var userId = CreateUser($"user.{Guid.NewGuid():N}", target);

            _service.SetRolePermissions(target, new[] { "Assets.View", "Assets.Edit" });
            var afterReplace = _service.GetEffectivePermissions(userId);
            Assert.DoesNotContain("Journal.View", afterReplace);
            Assert.Contains("Assets.View", afterReplace);
            Assert.Contains("Assets.Edit", afterReplace);

            _service.CopyRolePermissions(source, target);
            var afterCopy = _service.GetEffectivePermissions(userId);
            Assert.DoesNotContain("Assets.View", afterCopy);
            Assert.Contains("Products.View", afterCopy);
            Assert.Contains("Products.Create", afterCopy);
        }
    }
}
