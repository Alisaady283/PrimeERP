using System.Collections.Generic;
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
            Inherit(permissions, permissions.InsertMissingPermissions(PermissionKeys.All()));

            var roleId = permissions.FindRoleId("SystemAdmin")
                         ?? permissions.InsertRole("SystemAdmin", "مدير النظام", isSystem: true);

            var granted = permissions.GetRolePermissions(roleId);
            if (PermissionKeys.All().Except(granted).Any())
                permissions.ReplaceRolePermissions(roleId, granted.Union(PermissionKeys.All()));

            if (permissions.UsernameExists("admin")) return;

            var (hash, salt) = PasswordHasher.Hash("admin");
            permissions.InsertUser(new User
            {
                Username     = "admin",
                PasswordHash = hash,
                Salt         = salt,
                DisplayName  = "Admin",
                RoleId       = roleId,
                IsActive     = true
            });
        }

        /// <summary>المفتاح الجديد يرث أصله: الدور الحامل له، واستثناء المستخدم فيه (PermissionKeys.OriginOf)</summary>
        private static void Inherit(IPermissionStore permissions, List<string> added)
        {
            var split = added.Select(k => (Key: k, Origin: PermissionKeys.OriginOf(k))).Where(k => k.Origin != null).ToList();
            if (split.Count == 0) return;

            foreach (var role in permissions.GetAllRoles())
            {
                var granted = permissions.GetRolePermissions(role.Id);
                var inherited = split.Where(k => granted.Contains(k.Origin)).Select(k => k.Key).ToList();
                if (inherited.Count > 0) permissions.ReplaceRolePermissions(role.Id, granted.Union(inherited));
            }

            foreach (var user in permissions.GetAllUsers())
            {
                var granted = permissions.GetUserGrantedPermissions(user.Id).ToHashSet();
                var revoked = permissions.GetUserRevokedPermissions(user.Id).ToHashSet();
                foreach (var (key, origin) in split)
                    if (granted.Contains(origin)) permissions.SetUserPermission(user.Id, key, true);
                    else if (revoked.Contains(origin)) permissions.SetUserPermission(user.Id, key, false);
            }
        }
    }
}
