using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Platform.Permissions
{
    public class PermissionService : IPermissionService
    {
        public static readonly PermissionService Instance = new();

        public bool Can(string key)
        {
            if (AppSession.DevMode) return true;
            return AppSession.Permissions.Contains(key);
        }

        public bool CanAny(params string[] keys)
        {
            if (AppSession.DevMode) return true;
            return keys.Any(k => AppSession.Permissions.Contains(k));
        }

        public bool CanAll(params string[] keys)
        {
            if (AppSession.DevMode) return true;
            return keys.All(k => AppSession.Permissions.Contains(k));
        }

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
    }
}
