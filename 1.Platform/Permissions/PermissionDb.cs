using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Data.Schema;
using PrimeERP.Platform.Security;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Platform.Permissions
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
            foreach (var key in PermissionKeys.All())
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
            var roleId = existing != null
                ? Convert.ToInt32(existing)
                : Db.InsertAndGetId(
                    "INSERT INTO Roles (Name, NameAr, IsSystem) VALUES (@n, @na, @sys)",
                    Db.Params(("@n", "SystemAdmin"), ("@na", "مدير النظام"), ("@sys", true)));

            var granted = GetRolePermissions(roleId).ToHashSet();
            foreach (var key in PermissionKeys.All().Where(k => !granted.Contains(k)))
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

        // ═══ إدارة الأدوار والمستخدمين — CRUD مضاف على نفس الجداول أعلاه، بنفس أسلوب DbHelper المباشر
        // (استثناء مقبول ومسجَّل مثل BackupService — لا Repository موازٍ لجداول Platform.Permissions). ═══

        public class RoleRecord { public int Id; public string Name; public string NameAr; public bool IsSystem; }

        public static List<RoleRecord> GetAllRoles() =>
            Db.Query("SELECT Id, Name, NameAr, IsSystem FROM Roles ORDER BY Name").AsEnumerable()
              .Select(r => new RoleRecord { Id = Convert.ToInt32(r["Id"]), Name = r["Name"].ToString(), NameAr = r["NameAr"].ToString(), IsSystem = Convert.ToInt32(r["IsSystem"]) == 1 })
              .ToList();

        public static int InsertRole(string name, string nameAr) =>
            Db.InsertAndGetId("INSERT INTO Roles (Name, NameAr, IsSystem) VALUES (@n, @na, 0)", Db.Params(("@n", name), ("@na", nameAr)));

        public static void UpdateRole(int id, string name, string nameAr) =>
            Db.Execute("UPDATE Roles SET Name = @n, NameAr = @na WHERE Id = @id AND IsSystem = 0", Db.Params(("@n", name), ("@na", nameAr), ("@id", id)));

        public static bool IsSystemRole(int id) =>
            Convert.ToInt32(Db.Scalar("SELECT IsSystem FROM Roles WHERE Id = @id", Db.Params(("@id", id)))) == 1;

        public static bool RoleHasUsers(int id) =>
            Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM Users WHERE RoleId = @id", Db.Params(("@id", id)))) > 0;

        public static void DeleteRole(int id) =>
            Db.Execute("DELETE FROM Roles WHERE Id = @id AND IsSystem = 0", Db.Params(("@id", id)));

        public class UserListRecord { public int Id; public string Username; public string DisplayName; public int RoleId; public string RoleName; public bool IsActive; public DateTime? LastLoginAt; }

        public static List<UserListRecord> GetAllUsers() =>
            Db.Query(@"SELECT u.Id, u.Username, u.DisplayName, u.RoleId, r.NameAr AS RoleName, u.IsActive, u.LastLoginAt
                       FROM Users u JOIN Roles r ON r.Id = u.RoleId ORDER BY u.Username").AsEnumerable()
              .Select(r => new UserListRecord
              {
                  Id = Convert.ToInt32(r["Id"]), Username = r["Username"].ToString(), DisplayName = r["DisplayName"].ToString(),
                  RoleId = Convert.ToInt32(r["RoleId"]), RoleName = r["RoleName"].ToString(), IsActive = Convert.ToInt32(r["IsActive"]) == 1,
                  LastLoginAt = r["LastLoginAt"] == DBNull.Value ? null : Convert.ToDateTime(r["LastLoginAt"])
              }).ToList();

        public static bool UsernameExists(string username) =>
            Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM Users WHERE Username = @u", Db.Params(("@u", username)))) > 0;

        public static int InsertUser(string username, string password, string displayName, int roleId, bool isActive)
        {
            var (hash, salt) = PasswordHasher.Hash(password);
            return Db.InsertAndGetId(
                "INSERT INTO Users (Username, PasswordHash, Salt, DisplayName, RoleId, IsActive) VALUES (@u, @h, @s, @d, @r, @a)",
                Db.Params(("@u", username), ("@h", hash), ("@s", salt), ("@d", displayName), ("@r", roleId), ("@a", isActive)));
        }

        public static void UpdateUser(int id, string displayName, int roleId, bool isActive) =>
            Db.Execute("UPDATE Users SET DisplayName = @d, RoleId = @r, IsActive = @a WHERE Id = @id",
                Db.Params(("@d", displayName), ("@r", roleId), ("@a", isActive), ("@id", id)));

        public static void UpdateUserPassword(int id, string password)
        {
            var (hash, salt) = PasswordHasher.Hash(password);
            Db.Execute("UPDATE Users SET PasswordHash = @h, Salt = @s WHERE Id = @id", Db.Params(("@h", hash), ("@s", salt), ("@id", id)));
        }

        public static void DeleteUser(int id) =>
            Db.Execute("UPDATE Users SET IsActive = 0 WHERE Id = @id", Db.Params(("@id", id)));
    }
}
