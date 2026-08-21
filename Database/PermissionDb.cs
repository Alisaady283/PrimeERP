using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using PrimeERP.Core.Database;
using PrimeERP.Core.Security;
using Db = PrimeERP.Core.Database.DbHelper;

namespace PrimeERP.Database
{
    /// <summary>
    /// جداول الصلاحيات والمستخدمين — مبنية عبر SchemaBuilder (تعمل على أي محرك)،
    /// لذلك تستخدم DbHelper الجديد القابل للتبديل (مستعار هنا باسم Db لتفادي تعارض
    /// الاسم مع DbHelper القديم الموجود في نفس Namespace)، لا الـ DbHelper القديم الخاص بـ SQLite.
    /// </summary>
    public static class PermissionDb
    {
        public static void CreateTables()
        {
            SchemaBuilder.Table("Permissions")
                .Id()
                .Text("Key", 150, required: true, unique: true)
                .Text("Module", 50, required: true)
                .Text("DisplayNameAr", 150)
                .Text("DisplayNameEn", 150)
                .Create();

            SchemaBuilder.Table("Roles")
                .Id()
                .Text("Name", 100, required: true, unique: true)
                .Text("NameAr", 100, required: true)
                .Bool("IsSystem", defaultValue: false)
                .Audit()
                .Create();

            SchemaBuilder.Table("RolePermissions")
                .Id()
                .Int("RoleId", nullable: false)
                .Text("PermissionKey", 150, required: true)
                .ForeignKey("RoleId", "Roles", "Id")
                .Index("RoleId")
                .Create();

            SchemaBuilder.Table("Users")
                .Id()
                .Text("Username", 100, required: true, unique: true)
                .Text("PasswordHash", 400, required: true)
                .Text("Salt", 200, required: true)
                .Text("DisplayName", 150, required: true)
                .Int("RoleId", nullable: false)
                .Bool("IsActive", defaultValue: true)
                .DateCol("LastLoginAt")
                .Audit()
                .Concurrency()
                .ForeignKey("RoleId", "Roles", "Id")
                .Create();

            SchemaBuilder.Table("UserPermissions")
                .Id()
                .Int("UserId", nullable: false)
                .Text("PermissionKey", 150, required: true)
                .Bool("IsGranted", defaultValue: true)
                .ForeignKey("UserId", "Users", "Id")
                .Index("UserId")
                .Create();
        }

        public static void SeedDefaults()
        {
            SeedPermissions();
            int adminRoleId = SeedAdminRole();
            SeedAdminUser(adminRoleId);
        }

        private static void SeedPermissions()
        {
            foreach (var key in Core.Permissions.PermissionKeys.All())
            {
                var exists = Db.Scalar(
                    "SELECT COUNT(*) FROM Permissions WHERE \"Key\" = @k",
                    Db.Params(("@k", key)));

                if (Convert.ToInt64(exists) > 0) continue;

                var module = key.Contains('.') ? key.Split('.')[0] : key;

                Db.Execute(
                    "INSERT INTO Permissions (\"Key\", Module) VALUES (@k, @m)",
                    Db.Params(("@k", key), ("@m", module)));
            }
        }

        private static int SeedAdminRole()
        {
            var existing = Db.Scalar("SELECT Id FROM Roles WHERE Name = 'SystemAdmin'");
            if (existing != null)
                return Convert.ToInt32(existing);

            var roleId = Db.InsertAndGetId(
                "INSERT INTO Roles (Name, NameAr, IsSystem) VALUES (@n, @na, @sys)",
                Db.Params(("@n", "SystemAdmin"), ("@na", "مدير النظام"), ("@sys", true)));

            foreach (var key in Core.Permissions.PermissionKeys.All())
                Db.Execute(
                    "INSERT INTO RolePermissions (RoleId, PermissionKey) VALUES (@r, @k)",
                    Db.Params(("@r", roleId), ("@k", key)));

            return roleId;
        }

        private static void SeedAdminUser(int roleId)
        {
            var exists = Db.Scalar("SELECT COUNT(*) FROM Users WHERE Username = 'admin'");
            if (Convert.ToInt64(exists) > 0) return;

            var (hash, salt) = PasswordHasher.Hash("admin");

            Db.Execute(
                @"INSERT INTO Users (Username, PasswordHash, Salt, DisplayName, RoleId, IsActive)
                  VALUES (@u, @h, @s, @d, @r, @a)",
                Db.Params(
                    ("@u", "admin"), ("@h", hash), ("@s", salt),
                    ("@d", "مدير النظام"), ("@r", roleId), ("@a", true)));
        }

        /// <summary>معرّف دور المستخدم فقط — بلا حساب صلاحيات فعّالة (ذلك في PermissionService.GetUserPermissions).</summary>
        public static int? GetRoleIdForUser(int userId)
        {
            var result = Db.Scalar("SELECT RoleId FROM Users WHERE Id = @id", Db.Params(("@id", userId)));
            return result == null ? (int?)null : Convert.ToInt32(result);
        }

        public static List<string> GetRolePermissions(int roleId) =>
            Db.Query("SELECT PermissionKey FROM RolePermissions WHERE RoleId = @r", Db.Params(("@r", roleId)))
              .AsEnumerable().Select(r => r["PermissionKey"].ToString()).ToList();

        public static List<string> GetUserGrantedPermissions(int userId) =>
            Db.Query("SELECT PermissionKey FROM UserPermissions WHERE UserId = @u AND IsGranted = 1", Db.Params(("@u", userId)))
              .AsEnumerable().Select(r => r["PermissionKey"].ToString()).ToList();

        public static List<string> GetUserRevokedPermissions(int userId) =>
            Db.Query("SELECT PermissionKey FROM UserPermissions WHERE UserId = @u AND IsGranted = 0", Db.Params(("@u", userId)))
              .AsEnumerable().Select(r => r["PermissionKey"].ToString()).ToList();

        public class UserRecord
        {
            public int    Id           { get; set; }
            public string Username     { get; set; }
            public string PasswordHash { get; set; }
            public string Salt         { get; set; }
            public string DisplayName  { get; set; }
            public int    RoleId       { get; set; }
            public string RoleName     { get; set; }
            public bool   IsActive     { get; set; }
        }

        public static UserRecord FindByUsername(string username)
        {
            var dt = Db.Query(
                @"SELECT u.Id, u.Username, u.PasswordHash, u.Salt, u.DisplayName,
                         u.RoleId, r.NameAr AS RoleName, u.IsActive
                  FROM Users u JOIN Roles r ON r.Id = u.RoleId
                  WHERE u.Username = @u",
                Db.Params(("@u", username)));

            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];

            return new UserRecord
            {
                Id           = Convert.ToInt32(row["Id"]),
                Username     = row["Username"].ToString(),
                PasswordHash = row["PasswordHash"].ToString(),
                Salt         = row["Salt"].ToString(),
                DisplayName  = row["DisplayName"].ToString(),
                RoleId       = Convert.ToInt32(row["RoleId"]),
                RoleName     = row["RoleName"].ToString(),
                IsActive     = Convert.ToInt32(row["IsActive"]) == 1
            };
        }

        public static void UpdateLastLogin(int userId)
        {
            Db.Execute(
                "UPDATE Users SET LastLoginAt = @now WHERE Id = @id",
                Db.Params(
                    ("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                    ("@id", userId)));
        }
    }
}
