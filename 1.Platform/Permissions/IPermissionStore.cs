using System.Collections.Generic;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Platform.Permissions
{
    /// <summary>مخزن جداول الصلاحيات والأدوار والمستخدمين</summary>
    public interface IPermissionStore
    {
        int? GetRoleIdForUser(int userId);
        List<string> GetRolePermissions(int roleId);
        List<string> GetUserGrantedPermissions(int userId);
        List<string> GetUserRevokedPermissions(int userId);
        void SetUserPermission(int userId, string key, bool? granted);
        void ReplaceRolePermissions(int roleId, IEnumerable<string> keys);
        void InsertMissingPermissions(IEnumerable<string> keys);

        List<Role> GetAllRoles();
        int InsertRole(string name, string nameAr, bool isSystem = false);
        void UpdateRole(int id, string name, string nameAr);
        bool IsSystemRole(int id);
        bool RoleHasUsers(int id);
        void DeleteRole(int id);
        int? FindRoleId(string name);

        List<User> GetAllUsers();
        User FindByUsername(string username);
        bool UsernameExists(string username);
        int InsertUser(User user);
        void UpdateUser(int id, string displayName, int roleId, bool isActive);
        void UpdateUserPassword(int id, string passwordHash, string salt);
        void UpdateLastLogin(int userId);
        void DeleteUser(int id);
    }
}
