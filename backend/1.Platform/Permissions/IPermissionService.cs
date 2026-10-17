using System.Collections.Generic;

namespace PrimeERP.Platform.Permissions
{
    /// <summary>فحص الصلاحية وتحميلها</summary>
    public interface IPermissionService
    {
        bool Can(string key);
        bool CanAny(params string[] keys);
        bool CanAll(params string[] keys);
        void LoadForUser(int userId);
        IEnumerable<string> GetUserPermissions(int userId);
    }

    /// <summary>إدارة المنح للدور والمستخدم</summary>
    public interface IPermissionAdminService
    {
        HashSet<string> GetEffectivePermissions(int userId);
        PermissionState GetState(int userId, string key);
        bool IsInheritedFromRole(int userId, string key);
        void SetUserPermission(int userId, string key, PermissionState state);
        void SetRolePermissions(int roleId, IEnumerable<string> keys);
        void CopyRolePermissions(int fromRoleId, int toRoleId);
    }
}
