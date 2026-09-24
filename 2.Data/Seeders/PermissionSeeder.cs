using System.Linq;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Security;

namespace PrimeERP.Data.Seeders
{
    /// <summary>زرع المفاتيح ودور مدير النظام</summary>
    public static class PermissionSeeder
    {
        public static void Seed(IPermissionStore permissions)
        {
            permissions.InsertMissingPermissions(PermissionKeys.All());

            var roleId = permissions.FindRoleId("SystemAdmin")
                         ?? permissions.InsertRole("SystemAdmin", "مدير النظام", isSystem: true);

            permissions.ReplaceRolePermissions(roleId,
                permissions.GetRolePermissions(roleId).Union(PermissionKeys.All()));

            if (permissions.UsernameExists("admin")) return;

            var (hash, salt) = PasswordHasher.Hash("admin");
            permissions.InsertUser(new User
            {
                Username     = "admin",
                PasswordHash = hash,
                Salt         = salt,
                DisplayName  = "مدير النظام",
                RoleId       = roleId,
                IsActive     = true
            });
        }
    }
}
