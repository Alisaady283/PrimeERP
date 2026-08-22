using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>
    /// طبقة وصول بيانات شجرة الحسابات — SQL خام ↔ Models فقط، بلا أي منطق أعمال (مستوى/كود/ربط/رصيد/Audit).
    /// كل ذلك مسؤولية IAccountService.
    /// </summary>
    public class AccountRepository : RepositoryBase<Account>, IAccountRepository
    {
        protected override string TableName => "Accounts";

        public void CreateTable() =>
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

        public void SeedDefaults()
        {
            if (GetAll().Count > 0) return;

            // IsLeaf=false افتراضياً لكل حساب — فئة تحتاج حسابات فرعية حقيقية تُنشأ لاحقاً عبر AccountService.Create.
            // "3200" استثناء: الأرباح المحتجزة حساب دفتري واحد نهائي (مستهدَف مباشرة بقيد الإقفال السنوي)، فيجب أن يكون Leaf فعلياً.
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
                Exec("INSERT INTO Accounts (Code, Name, ParentCode, Level, IsLeaf, Type) VALUES (@code, @name, @parent, @level, @leaf, @type)",
                    null, null, ("@code", a.Code), ("@name", a.Name), ("@parent", a.Parent), ("@level", a.Level), ("@leaf", a.IsLeaf), ("@type", a.Type));
        }

        protected override Account Map(DataRow row) => new()
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

        public List<Account> GetAll(bool includeInactive = false) =>
            includeInactive
                ? Query("SELECT * FROM Accounts ORDER BY Code")
                : Query("SELECT * FROM Accounts WHERE IsActive = @a ORDER BY Code", null, null, ("@a", true));

        public Account GetByCode(string code, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Accounts WHERE Code = @c", conn, tx, ("@c", code));

        public List<Account> GetChildren(string parentCode, DbConnection conn = null, DbTransaction tx = null) =>
            Query("SELECT * FROM Accounts WHERE ParentCode = @p AND IsActive = @a ORDER BY Code", conn, tx, ("@p", parentCode), ("@a", true));

        public List<Account> GetLeaves() =>
            Query("SELECT * FROM Accounts WHERE IsLeaf = @l AND IsActive = @a ORDER BY Code", null, null, ("@l", true), ("@a", true));

        public int GetLevel(string code)
        {
            var result = Scalar("SELECT Level FROM Accounts WHERE Code = @c", ("@c", code));
            return result != null ? Convert.ToInt32(result) : 1;
        }

        public int GetTypeOf(string code)
        {
            var result = Scalar("SELECT Type FROM Accounts WHERE Code = @c", ("@c", code));
            return result != null ? Convert.ToInt32(result) : 1;
        }

        public bool HasChildren(string code) =>
            Convert.ToInt64(Scalar("SELECT COUNT(*) FROM Accounts WHERE ParentCode = @p AND IsActive = @a", ("@p", code), ("@a", true))) > 0;

        /// <summary>إدراج صف كما هو — الخدمة هي من تحسب Level وتضبط IsLeaf للأب والربط والـ Audit، لا هنا.</summary>
        public int Insert(Account a, DbConnection conn = null, DbTransaction tx = null) =>
            InsertGetId(
                "INSERT INTO Accounts (Code, Name, ParentCode, Level, IsLeaf, Type) VALUES (@code, @name, @parent, @level, @leaf, @type)",
                conn, tx, ("@code", a.Code), ("@name", a.Name), ("@parent", a.ParentCode), ("@level", a.Level), ("@leaf", a.IsLeaf), ("@type", a.Type));

        /// <summary>تحديث حقول الحساب نفسه فقط — مزامنة العميل/المورد المرتبط والـ Audit مسؤولية الخدمة.</summary>
        public void Update(Account a, DbConnection conn = null, DbTransaction tx = null) =>
            Exec("UPDATE Accounts SET Name = @name, Notes = @notes, IsLeaf = @leaf, UpdatedAt = @now WHERE Code = @code",
                conn, tx, ("@name", a.Name), ("@notes", a.Notes ?? ""), ("@leaf", a.IsLeaf), ("@now", DateTime.Now), ("@code", a.Code));

        /// <summary>تحديث الاسم فقط — تستخدمها AccountService.UpdateName لمزامنة اسم حساب من تعديل الطرف المرتبط (عميل/مورد).</summary>
        public void UpdateName(DbConnection conn, DbTransaction tx, string code, string name) =>
            Exec("UPDATE Accounts SET Name = @name, UpdatedAt = @now WHERE Code = @code",
                conn, tx, ("@name", name), ("@now", DateTime.Now), ("@code", code));

        public void SetIsLeaf(string code, bool isLeaf, DbConnection conn = null, DbTransaction tx = null) =>
            Exec("UPDATE Accounts SET IsLeaf = @f WHERE Code = @c", conn, tx, ("@f", isLeaf), ("@c", code));

        /// <summary>حذف منطقي (IsActive=false) لصف الحساب فقط — حذف العميل/المورد المرتبط والـ Audit مسؤولية الخدمة.</summary>
        public void Delete(string code, DbConnection conn = null, DbTransaction tx = null) =>
            Exec("UPDATE Accounts SET IsActive = @a WHERE Code = @code", conn, tx, ("@a", false), ("@code", code));

        public void UpdateBalance(string code, decimal balance, DbConnection conn = null, DbTransaction tx = null) =>
            Exec("UPDATE Accounts SET Balance = @b, UpdatedAt = @now WHERE Code = @code",
                conn, tx, ("@b", balance), ("@now", DateTime.Now), ("@code", code));
    }
}
