using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using PrimeERP.Core.Database;
using PrimeERP.Models;
using Db = PrimeERP.Core.Database.DbHelper;

namespace PrimeERP.Database
{
    /// <summary>
    /// طبقة وصول بيانات شجرة الحسابات — SQL خام ↔ Models فقط، بلا أي منطق أعمال (مستوى/كود/ربط/رصيد/Audit).
    /// كل ذلك مسؤولية Services/Accounting/IAccountService (المرحلة F.2) — راجع MIGRATION_INVENTORY.md
    /// لِما نُقل من هنا وإلى أين. فوق Core/Database/DbHelper + SchemaBuilder (نمط PermissionDb.cs المرجعي).
    /// </summary>
    public static class AccountRepository
    {
        public static void CreateTable()
        {
            SchemaBuilder.Table("Accounts")
                .Id()
                .Text("Code", 30, required: true, unique: true)
                .Text("Name", 200, required: true)
                .Text("ParentCode", 30)
                .Int("Level", nullable: false, defaultValue: 1)
                .Bool("IsLeaf", defaultValue: true)
                .Int("Type", nullable: false)
                .Decimal("Balance")
                .Text("Notes")
                .Bool("IsActive", defaultValue: true)
                .Audit()
                .Index("ParentCode")
                .Create();
        }

        public static void SeedDefaults()
        {
            if (GetAll().Count > 0) return;

            // IsLeaf=false افتراضياً لكل حساب — فئة تحتاج حسابات فرعية حقيقية تُنشأ لاحقاً عبر AccountService.Create
            // (نفس السبب أن 1220/2110 فئتان لا حساب واحد: كل عميل/مورد ابن منفصل). "3200" استثناء: الأرباح
            // المحتجزة حساب دفتري واحد نهائي في أي شجرة حسابات واقعية (لا يُقسَّم لأبناء) — وهو مستهدَف مباشرةً
            // بقيد الإقفال السنوي (FiscalPeriodService.CloseYear عبر SettingKeys.Accounts.RetainedEarnings)،
            // فيجب أن يكون Leaf فعلياً وإلا يرفضه IJournalService.Create (لا يقبل قيوداً على حساب غير Leaf).
            var accounts = new (string Code, string Name, string Parent, int Level, int Type, bool IsLeaf)[]
            {
                ("1000", "أصول",                  null,   1, 1, false),
                ("1100", "أصول غير متداولة",      "1000", 2, 1, false),
                ("1110", "أصول ثابتة",            "1100", 3, 1, false),
                ("1200", "أصول متداولة",          "1000", 2, 1, false),
                ("1210", "المخزون",               "1200", 3, 1, false),
                ("1220", "ذمم مدينة (العملاء)",   "1200", 3, 1, false),
                ("1230", "البنوك",                "1200", 3, 1, false),
                ("1240", "الصناديق",              "1200", 3, 1, false),
                ("2000", "خصوم",                  null,   1, 2, false),
                ("2100", "خصوم متداولة",          "2000", 2, 2, false),
                ("2110", "ذمم دائنة (الموردون)",  "2100", 3, 2, false),
                ("2200", "خصوم طويلة الأجل",      "2000", 2, 2, false),
                ("3000", "حقوق الملكية",          null,   1, 3, false),
                ("3100", "رأس المال",             "3000", 2, 3, false),
                ("3200", "الأرباح المحتجزة",      "3000", 2, 3, true),
                ("4000", "إيرادات",               null,   1, 4, false),
                ("4100", "إيرادات المبيعات",      "4000", 2, 4, false),
                ("4200", "إيرادات أخرى",          "4000", 2, 4, false),
                ("5000", "مصروفات",               null,   1, 5, false),
                ("5100", "مصروفات تشغيلية",       "5000", 2, 5, false),
                ("5200", "مصروفات أخرى",          "5000", 2, 5, false),
            };

            foreach (var a in accounts)
                Db.Execute(
                    "INSERT INTO Accounts (Code, Name, ParentCode, Level, IsLeaf, Type) VALUES (@code, @name, @parent, @level, @leaf, @type)",
                    Db.Params(("@code", a.Code), ("@name", a.Name), ("@parent", a.Parent), ("@level", a.Level), ("@leaf", a.IsLeaf), ("@type", a.Type)));
        }

        private static Account Map(DataRow row) => new()
        {
            Id         = Convert.ToInt32(row["Id"]),
            Code       = row["Code"].ToString(),
            Name       = row["Name"].ToString(),
            ParentCode = row["ParentCode"] == DBNull.Value ? null : row["ParentCode"].ToString(),
            Level      = Convert.ToInt32(row["Level"]),
            IsLeaf     = Convert.ToBoolean(row["IsLeaf"]),
            Type       = Convert.ToInt32(row["Type"]),
            Balance    = Convert.ToDecimal(row["Balance"]),
            Notes      = row["Notes"] == DBNull.Value ? "" : row["Notes"].ToString(),
            IsActive   = Convert.ToBoolean(row["IsActive"]),
            CreatedAt  = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            UpdatedAt  = row["UpdatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["UpdatedAt"])
        };

        public static List<Account> GetAll(bool includeInactive = false) =>
            (includeInactive
                ? Db.Query("SELECT * FROM Accounts ORDER BY Code")
                : Db.Query("SELECT * FROM Accounts WHERE IsActive = @a ORDER BY Code", Db.Params(("@a", true))))
              .AsEnumerable().Select(Map).ToList();

        public static Account GetById(int id) =>
            Db.Query("SELECT * FROM Accounts WHERE Id = @id", Db.Params(("@id", id)))
              .AsEnumerable().Select(Map).FirstOrDefault();

        /// <summary>لقراءة حساب من داخل معاملة مستدعٍ آخر مفتوحة بالفعل (AccountService.Create(conn,tx,...) المستدعاة من CustomerService.Create) — نفس سبب GetByCode(conn,tx,...).</summary>
        public static Account GetById(DbConnection conn, DbTransaction tx, int id) =>
            Db.Query(conn, tx, "SELECT * FROM Accounts WHERE Id = @id", Db.Params(("@id", id)))
              .AsEnumerable().Select(Map).FirstOrDefault();

        public static Account GetByCode(string code) =>
            Db.Query("SELECT * FROM Accounts WHERE Code = @c", Db.Params(("@c", code)))
              .AsEnumerable().Select(Map).FirstOrDefault();

        /// <summary>لقراءة حساب من داخل معاملة مستدعٍ آخر مفتوحة بالفعل (JournalService.Create(conn,tx,...)) — اتصال منفصل هنا يُعلِّق (deadlock) على SQLite؛ راجع تعليق DbHelper.Query(conn,tx,...).</summary>
        public static Account GetByCode(DbConnection conn, DbTransaction tx, string code) =>
            Db.Query(conn, tx, "SELECT * FROM Accounts WHERE Code = @c", Db.Params(("@c", code)))
              .AsEnumerable().Select(Map).FirstOrDefault();

        public static List<Account> GetChildren(string parentCode) =>
            Db.Query("SELECT * FROM Accounts WHERE ParentCode = @p AND IsActive = @a ORDER BY Code",
                Db.Params(("@p", parentCode), ("@a", true)))
              .AsEnumerable().Select(Map).ToList();

        /// <summary>نفس GetChildren أعلاه من داخل معاملة قائمة — يستخدمها توليد الكود (GenerateChildCodeInternal) عند الاستدعاء من AccountService.Create(conn,tx,...).</summary>
        public static List<Account> GetChildren(DbConnection conn, DbTransaction tx, string parentCode) =>
            Db.Query(conn, tx, "SELECT * FROM Accounts WHERE ParentCode = @p AND IsActive = @a ORDER BY Code",
                Db.Params(("@p", parentCode), ("@a", true)))
              .AsEnumerable().Select(Map).ToList();

        public static List<Account> GetLeaves() =>
            Db.Query("SELECT * FROM Accounts WHERE IsLeaf = @l AND IsActive = @a ORDER BY Code",
                Db.Params(("@l", true), ("@a", true)))
              .AsEnumerable().Select(Map).ToList();

        public static int GetLevel(string code)
        {
            var result = Db.Scalar("SELECT Level FROM Accounts WHERE Code = @c", Db.Params(("@c", code)));
            return result != null ? Convert.ToInt32(result) : 1;
        }

        public static int GetTypeOf(string code)
        {
            var result = Db.Scalar("SELECT Type FROM Accounts WHERE Code = @c", Db.Params(("@c", code)));
            return result != null ? Convert.ToInt32(result) : 1;
        }

        public static bool HasChildren(string code)
        {
            var result = Db.Scalar("SELECT COUNT(*) FROM Accounts WHERE ParentCode = @p AND IsActive = @a",
                Db.Params(("@p", code), ("@a", true)));
            return Convert.ToInt64(result) > 0;
        }

        /// <summary>إدراج صف كما هو — الخدمة هي من تحسب Level وتضبط IsLeaf للأب والربط والـ Audit، لا هنا.</summary>
        public static int Insert(Account a) =>
            Db.InsertAndGetId(
                "INSERT INTO Accounts (Code, Name, ParentCode, Level, IsLeaf, Type) VALUES (@code, @name, @parent, @level, @leaf, @type)",
                Db.Params(("@code", a.Code), ("@name", a.Name), ("@parent", a.ParentCode), ("@level", a.Level), ("@leaf", a.IsLeaf), ("@type", a.Type)));

        public static int Insert(DbConnection conn, DbTransaction tx, Account a) =>
            Db.InsertAndGetId(conn, tx,
                "INSERT INTO Accounts (Code, Name, ParentCode, Level, IsLeaf, Type) VALUES (@code, @name, @parent, @level, @leaf, @type)",
                Db.Params(("@code", a.Code), ("@name", a.Name), ("@parent", a.ParentCode), ("@level", a.Level), ("@leaf", a.IsLeaf), ("@type", a.Type)));

        /// <summary>تحديث حقول الحساب نفسه فقط — مزامنة العميل/المورد المرتبط والـ Audit مسؤولية الخدمة.</summary>
        public static void Update(Account a) =>
            Db.Execute(
                "UPDATE Accounts SET Name = @name, Notes = @notes, IsLeaf = @leaf, UpdatedAt = @now WHERE Code = @code",
                Db.Params(("@name", a.Name), ("@notes", a.Notes ?? ""), ("@leaf", a.IsLeaf), ("@now", DateTime.Now), ("@code", a.Code)));

        public static void Update(DbConnection conn, DbTransaction tx, Account a)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE Accounts SET Name = @name, Notes = @notes, IsLeaf = @leaf, UpdatedAt = @now WHERE Code = @code",
                Db.Params(("@name", a.Name), ("@notes", a.Notes ?? ""), ("@leaf", a.IsLeaf), ("@now", DateTime.Now), ("@code", a.Code)));
            cmd.ExecuteNonQuery();
        }

        /// <summary>تحديث الاسم فقط — تستخدمها AccountService.UpdateName لمزامنة اسم حساب من تعديل الطرف المرتبط (عميل/مورد)، لا Update(Account) الكاملة (لا داعٍ لإعادة قراءة/تحقق Leaf/Notes لمجرد تغيير اسم).</summary>
        public static void UpdateName(DbConnection conn, DbTransaction tx, string code, string name)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE Accounts SET Name = @name, UpdatedAt = @now WHERE Code = @code",
                Db.Params(("@name", name), ("@now", DateTime.Now), ("@code", code)));
            cmd.ExecuteNonQuery();
        }

        public static void SetIsLeaf(string code, bool isLeaf) =>
            Db.Execute("UPDATE Accounts SET IsLeaf = @f WHERE Code = @c", Db.Params(("@f", isLeaf), ("@c", code)));

        public static void SetIsLeaf(DbConnection conn, DbTransaction tx, string code, bool isLeaf)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE Accounts SET IsLeaf = @f WHERE Code = @c", Db.Params(("@f", isLeaf), ("@c", code)));
            cmd.ExecuteNonQuery();
        }

        /// <summary>حذف منطقي (IsActive=false) لصف الحساب فقط — حذف العميل/المورد المرتبط والـ Audit مسؤولية الخدمة.</summary>
        public static void Delete(string code) =>
            Db.Execute("UPDATE Accounts SET IsActive = @a WHERE Code = @code", Db.Params(("@a", false), ("@code", code)));

        public static void Delete(DbConnection conn, DbTransaction tx, string code)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE Accounts SET IsActive = @a WHERE Code = @code", Db.Params(("@a", false), ("@code", code)));
            cmd.ExecuteNonQuery();
        }

        public static void UpdateBalance(string code, decimal balance) =>
            Db.Execute("UPDATE Accounts SET Balance = @b, UpdatedAt = @now WHERE Code = @code",
                Db.Params(("@b", balance), ("@now", DateTime.Now), ("@code", code)));

        public static void UpdateBalance(DbConnection conn, DbTransaction tx, string code, decimal balance)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE Accounts SET Balance = @b, UpdatedAt = @now WHERE Code = @code",
                Db.Params(("@b", balance), ("@now", DateTime.Now), ("@code", code)));
            cmd.ExecuteNonQuery();
        }
    }
}
