using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع الصلاحيات والأدوار والمستخدمين</summary>
    public class PermissionRepository : RepositoryBase<User>, IPermissionStore
    {
        private const string Roles           = "Roles";
        private const string Keys            = "Permissions";
        private const string RoleKeys        = "RolePermissions";
        private const string UserKeys        = "UserPermissions";

        protected override string TableName => "Users";

        // ── الصلاحيات ──────────────────────────────────────────────

        public int? GetRoleIdForUser(int userId) => One(q => q.Where(u => u.Id == userId))?.RoleId;

        public List<string> GetRolePermissions(int roleId) =>
            Values<RolePermission>(RoleKeys, q => q.Where(p => p.RoleId == roleId), p => p.PermissionKey);

        public List<string> GetUserGrantedPermissions(int userId) =>
            Values<UserPermission>(UserKeys, q => q.Where(p => p.UserId == userId && p.IsGranted), p => p.PermissionKey);

        public List<string> GetUserRevokedPermissions(int userId) =>
            Values<UserPermission>(UserKeys, q => q.Where(p => p.UserId == userId && !p.IsGranted), p => p.PermissionKey);

        public void SetUserPermission(int userId, string key, bool? granted) =>
            Write(db =>
            {
                RemoveIn<UserPermission>(UserKeys, p => p.UserId == userId && p.PermissionKey == key, db);
                if (granted != null)
                    SetOf<UserPermission>(db, UserKeys).Add(new UserPermission { UserId = userId, PermissionKey = key, IsGranted = granted.Value });
                return 0;
            });

        public void ReplaceRolePermissions(int roleId, IEnumerable<string> keys) =>
            Write(db =>
            {
                RemoveIn<RolePermission>(RoleKeys, p => p.RoleId == roleId, db);
                foreach (var key in keys.Distinct())
                    SetOf<RolePermission>(db, RoleKeys).Add(new RolePermission { RoleId = roleId, PermissionKey = key });
                return 0;
            });

        public void InsertMissingPermissions(IEnumerable<string> keys) =>
            Write(db =>
            {
                var rows = SetOf<Permission>(db, Keys);
                var known = rows.Select(p => p.Key).ToHashSet();
                foreach (var key in keys.Where(k => !known.Contains(k)))
                    rows.Add(new Permission { Key = key, Module = key.Contains('.') ? key.Split('.')[0] : key });
                return 0;
            });

        // ── الأدوار ────────────────────────────────────────────────

        public List<Role> GetAllRoles() =>
            FetchOf<Role>(Roles, q => q.OrderBy(r => r.Name));

        public int InsertRole(string name, string nameAr, bool isSystem = false) =>
            Write(db =>
            {
                var role = new Role { Name = name, NameAr = nameAr, IsSystem = isSystem };
                SetOf<Role>(db, Roles).Add(role);
                db.SaveChanges();
                return role.Id;
            });

        public void UpdateRole(int id, string name, string nameAr) =>
            Write(db =>
            {
                var role = SetOf<Role>(db, Roles).FirstOrDefault(r => r.Id == id && !r.IsSystem);
                if (role == null) return 0;
                role.Name = name;
                role.NameAr = nameAr;
                return 1;
            });

        public bool IsSystemRole(int id) =>
            FetchOf<Role>(Roles, q => q.Where(r => r.Id == id && r.IsSystem).Take(1)).Count > 0;

        public bool RoleHasUsers(int id) => Any(q => q.Where(u => u.RoleId == id));

        public void DeleteRole(int id) =>
            RemoveIn<Role>(Roles, r => r.Id == id && !r.IsSystem);

        public int? FindRoleId(string name) =>
            FetchOf<Role>(Roles, q => q.Where(r => r.Name == name).Take(1)).FirstOrDefault()?.Id;

        // ── المستخدمون ─────────────────────────────────────────────

        public List<User> GetAllUsers() => WithRole(q => q.OrderBy(u => u.Username));

        public User FindByUsername(string username) =>
            WithRole(q => q.Where(u => u.Username == username).Take(1)).FirstOrDefault();

        public bool UsernameExists(string username) => Any(q => q.Where(u => u.Username == username));

        public int InsertUser(User user) => Add(user);

        public void UpdateUser(int id, string displayName, int roleId, bool isActive) =>
            Edit(u => u.Id == id, user =>
            {
                user.DisplayName = displayName;
                user.RoleId = roleId;
                user.IsActive = isActive;
            });

        public void UpdateUserPassword(int id, string passwordHash, string salt) =>
            Edit(u => u.Id == id, user =>
            {
                user.PasswordHash = passwordHash;
                user.Salt = salt;
            });

        public void UpdateLastLogin(int userId) =>
            Set(u => u.Id == userId, s => s.SetProperty(r => r.LastLoginAt, DateTime.Now));

        public void DeleteUser(int id) => Set(u => u.Id == id, s => s.SetProperty(r => r.IsActive, false));

        // ── مشترك ──────────────────────────────────────────────────

        /// <summary>عمودٌ واحد من جدولٍ آخر</summary>
        private static List<string> Values<T>(string table, Func<IQueryable<T>, IQueryable<T>> shape,
            Func<T, string> column) where T : class =>
            FetchOf<T>(table, shape).Select(column).ToList();

        /// <summary>المستخدم باسم دوره</summary>
        private static List<User> WithRole(Func<IQueryable<User>, IQueryable<User>> shape)
        {
            using var db = DbContextFactory.Open();
            var rows = (from user in shape(db.Users.AsNoTracking())
                        join role in db.Roles.AsNoTracking() on user.RoleId equals role.Id
                        select new { user, role.NameAr }).ToList();

            foreach (var row in rows) row.user.RoleName = row.NameAr;
            return rows.Select(r => r.user).ToList();
        }
    }
}
