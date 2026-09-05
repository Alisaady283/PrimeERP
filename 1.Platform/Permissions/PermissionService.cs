using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Platform.Permissions
{
    public class PermissionService : IPermissionService, IPermissionAdminService
    {
        public bool Can(string key) => AppSession.BypassPermissions || AppSession.Permissions.Contains(key);

        public bool CanAny(params string[] keys) => AppSession.BypassPermissions || keys.Any(AppSession.Permissions.Contains);

        public bool CanAll(params string[] keys) => AppSession.BypassPermissions || keys.All(AppSession.Permissions.Contains);

        public void LoadForUser(int userId)
        {
            var perms = GetUserPermissions(userId).ToList();
            AppSession.Permissions.Clear();
            foreach (var p in perms)
                AppSession.Permissions.Add(p);
        }

        /// <summary>الصلاحيات الفعّالة = صلاحيات الدور ∪ منح خاصة بالمستخدم − سحب خاص بالمستخدم. الحساب هنا لا في PermissionDb (Repository قراءة/كتابة خام فقط).</summary>
        public IEnumerable<string> GetUserPermissions(int userId)
        {
            var roleId = PermissionDb.GetRoleIdForUser(userId);
            if (roleId == null) return Enumerable.Empty<string>();

            var rolePermissions = PermissionDb.GetRolePermissions(roleId.Value);
            var granted = PermissionDb.GetUserGrantedPermissions(userId);
            var revoked = PermissionDb.GetUserRevokedPermissions(userId).ToHashSet();

            return rolePermissions.Union(granted)
                                   .Where(k => !revoked.Contains(k))
                                   .Distinct()
                                   .ToList();
        }

        public HashSet<string> GetEffectivePermissions(int userId) => GetUserPermissions(userId).ToHashSet();

        public PermissionState GetState(int userId, string key)
        {
            if (PermissionDb.GetUserRevokedPermissions(userId).Contains(key)) return PermissionState.Revoked;
            if (PermissionDb.GetUserGrantedPermissions(userId).Contains(key)) return PermissionState.Granted;
            return PermissionState.Inherited;
        }

        public bool IsInheritedFromRole(int userId, string key)
        {
            var roleId = PermissionDb.GetRoleIdForUser(userId);
            return roleId != null && PermissionDb.GetRolePermissions(roleId.Value).Contains(key);
        }

        public void SetUserPermission(int userId, string key, PermissionState state)
        {
            PermissionDb.SetUserPermission(userId, key, state switch
            {
                PermissionState.Granted => true,
                PermissionState.Revoked => false,
                _ => (bool?)null
            });
        }

        public void SetRolePermissions(int roleId, IEnumerable<string> keys) =>
            PermissionDb.ReplaceRolePermissions(roleId, keys);

        public void CopyRolePermissions(int fromRoleId, int toRoleId) =>
            PermissionDb.ReplaceRolePermissions(toRoleId, PermissionDb.GetRolePermissions(fromRoleId));
    }
}
