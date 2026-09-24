using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Platform.Permissions
{
    /// <summary>فحص الصلاحيات وإدارة منحها</summary>
    public class PermissionService : IPermissionService, IPermissionAdminService
    {
        private readonly IPermissionStore _store;

        public PermissionService(IPermissionStore store) => _store = store;

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

        public IEnumerable<string> GetUserPermissions(int userId)
        {
            var roleId = _store.GetRoleIdForUser(userId);
            if (roleId == null) return Enumerable.Empty<string>();

            var rolePermissions = _store.GetRolePermissions(roleId.Value);
            var granted = _store.GetUserGrantedPermissions(userId);
            var revoked = _store.GetUserRevokedPermissions(userId).ToHashSet();

            var effective = rolePermissions.Union(granted)
                                            .Where(k => !revoked.Contains(k))
                                            .Distinct();

            return PermissionRules.WithImpliedView(effective).ToList();
        }

        public HashSet<string> GetEffectivePermissions(int userId) => GetUserPermissions(userId).ToHashSet();

        public PermissionState GetState(int userId, string key)
        {
            if (_store.GetUserRevokedPermissions(userId).Contains(key)) return PermissionState.Revoked;
            if (_store.GetUserGrantedPermissions(userId).Contains(key)) return PermissionState.Granted;
            return PermissionState.Inherited;
        }

        public bool IsInheritedFromRole(int userId, string key)
        {
            var roleId = _store.GetRoleIdForUser(userId);
            return roleId != null && _store.GetRolePermissions(roleId.Value).Contains(key);
        }

        public void SetUserPermission(int userId, string key, PermissionState state)
        {
            _store.SetUserPermission(userId, key, state switch
            {
                PermissionState.Granted => true,
                PermissionState.Revoked => false,
                _ => (bool?)null
            });

            if (state == PermissionState.Granted) GrantViewFor(userId, key);
            else if (state == PermissionState.Revoked && IsViewKey(key)) RevokeModuleActions(userId, key);
        }

        public void SetRolePermissions(int roleId, IEnumerable<string> keys) =>
            _store.ReplaceRolePermissions(roleId, PermissionRules.WithImpliedView(keys));

        private static bool IsViewKey(string key) => key == PermissionRules.ViewKeyOf(key);

        private void GrantViewFor(int userId, string key)
        {
            var view = PermissionRules.ViewKeyOf(key);
            if (view == null || view == key || !PermissionKeys.All().Contains(view)) return;
            if (_store.GetUserRevokedPermissions(userId).Contains(view) ||
                !_store.GetUserGrantedPermissions(userId).Contains(view))
                _store.SetUserPermission(userId, view, true);
        }

        private void RevokeModuleActions(int userId, string viewKey)
        {
            var module = viewKey[..viewKey.IndexOf('.')] + ".";
            foreach (var granted in _store.GetUserGrantedPermissions(userId)
                                                .Where(k => k != viewKey && k.StartsWith(module)).ToList())
                _store.SetUserPermission(userId, granted, false);
        }

        public void CopyRolePermissions(int fromRoleId, int toRoleId) =>
            _store.ReplaceRolePermissions(toRoleId, _store.GetRolePermissions(fromRoleId));
    }
}
