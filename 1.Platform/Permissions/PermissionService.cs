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

            var effective = rolePermissions.Union(granted)
                                            .Where(k => !revoked.Contains(k))
                                            .Distinct();

            // العرض يلحق بأي صلاحية في وحدته — بلا هذا تبقى الطباعة ممنوحة والقسم محجوباً.
            return PermissionRules.WithImpliedView(effective).ToList();
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

            // منح فعلٍ يجرّ معه عرض وحدته، وسحب العرض يسحب كل أفعالها — وإلا بقيت صلاحيات بلا باب تدخل منه.
            if (state == PermissionState.Granted) GrantViewFor(userId, key);
            else if (state == PermissionState.Revoked && IsViewKey(key)) RevokeModuleActions(userId, key);
        }

        public void SetRolePermissions(int roleId, IEnumerable<string> keys) =>
            PermissionDb.ReplaceRolePermissions(roleId, PermissionRules.WithImpliedView(keys));

        private static bool IsViewKey(string key) => key == PermissionRules.ViewKeyOf(key);

        private static void GrantViewFor(int userId, string key)
        {
            var view = PermissionRules.ViewKeyOf(key);
            if (view == null || view == key || !PermissionKeys.All().Contains(view)) return;
            if (PermissionDb.GetUserRevokedPermissions(userId).Contains(view) ||
                !PermissionDb.GetUserGrantedPermissions(userId).Contains(view))
                PermissionDb.SetUserPermission(userId, view, true);
        }

        private static void RevokeModuleActions(int userId, string viewKey)
        {
            var module = viewKey[..viewKey.IndexOf('.')] + ".";
            foreach (var granted in PermissionDb.GetUserGrantedPermissions(userId)
                                                .Where(k => k != viewKey && k.StartsWith(module)).ToList())
                PermissionDb.SetUserPermission(userId, granted, false);
        }

        public void CopyRolePermissions(int fromRoleId, int toRoleId) =>
            PermissionDb.ReplaceRolePermissions(toRoleId, PermissionDb.GetRolePermissions(fromRoleId));
    }
}
