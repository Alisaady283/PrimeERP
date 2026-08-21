using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using PrimeERP.Core.Database;
using Db = PrimeERP.Core.Database.DbHelper;

namespace PrimeERP.Database
{
    public class SettingRecord
    {
        public string Key            { get; set; }
        public string Value          { get; set; }
        public string Category       { get; set; }
        public string DataType       { get; set; }
        public string DisplayNameAr  { get; set; }
        public string DisplayNameEn  { get; set; }
        public bool   IsSystem       { get; set; }
        public DateTime? ModifiedAt  { get; set; }
        public string ModifiedBy     { get; set; }
    }

    /// <summary>طبقة وصول بيانات جدول AppSettings — SQL خام ↔ SettingRecord فقط. الـ Cache والتحقق من الصلاحية في SettingsService.</summary>
    public static class SettingRepository
    {
        public static void CreateTable()
        {
            SchemaBuilder.Table("AppSettings")
                .Id()
                .Text("Key", 150, required: true, unique: true)
                .Text("Value", 1000)
                .Text("Category", 50)
                .Text("DataType", 30)
                .Text("DisplayNameAr", 200)
                .Text("DisplayNameEn", 200)
                .Bool("IsSystem", defaultValue: false)
                .DateCol("ModifiedAt")
                .Text("ModifiedBy", 100)
                .Create();
        }

        private static SettingRecord Map(DataRow row) => new()
        {
            Key           = row["Key"].ToString(),
            Value         = row["Value"] == DBNull.Value ? null : row["Value"].ToString(),
            Category      = row["Category"] == DBNull.Value ? "" : row["Category"].ToString(),
            DataType      = row["DataType"] == DBNull.Value ? "" : row["DataType"].ToString(),
            DisplayNameAr = row["DisplayNameAr"] == DBNull.Value ? "" : row["DisplayNameAr"].ToString(),
            DisplayNameEn = row["DisplayNameEn"] == DBNull.Value ? "" : row["DisplayNameEn"].ToString(),
            IsSystem      = Convert.ToBoolean(row["IsSystem"]),
            ModifiedAt    = row["ModifiedAt"] == DBNull.Value ? null : Convert.ToDateTime(row["ModifiedAt"]),
            ModifiedBy    = row["ModifiedBy"] == DBNull.Value ? null : row["ModifiedBy"].ToString()
        };

        public static List<SettingRecord> GetAll() =>
            Db.Query("SELECT * FROM AppSettings").AsEnumerable().Select(Map).ToList();

        public static SettingRecord GetByKey(string key) =>
            Db.Query("SELECT * FROM AppSettings WHERE [Key] = @k", Db.Params(("@k", key)))
              .AsEnumerable().Select(Map).FirstOrDefault();

        public static List<SettingRecord> GetByCategory(string category) =>
            Db.Query("SELECT * FROM AppSettings WHERE Category = @c", Db.Params(("@c", category)))
              .AsEnumerable().Select(Map).ToList();

        /// <summary>يُدرج الصف لو غير موجود، أو يحدّث Value/ModifiedAt/ModifiedBy لو موجود — بلا فتح معاملة (الخدمة تديرها لو لزم).</summary>
        public static void Upsert(SettingRecord setting)
        {
            var exists = Db.Scalar("SELECT COUNT(*) FROM AppSettings WHERE [Key] = @k", Db.Params(("@k", setting.Key)));
            if (Convert.ToInt64(exists) > 0)
            {
                Db.Execute(
                    "UPDATE AppSettings SET Value = @v, ModifiedAt = @now, ModifiedBy = @by WHERE [Key] = @k",
                    Db.Params(("@v", setting.Value), ("@now", DateTime.Now), ("@by", setting.ModifiedBy), ("@k", setting.Key)));
            }
            else
            {
                Db.Execute(
                    @"INSERT INTO AppSettings ([Key], Value, Category, DataType, DisplayNameAr, DisplayNameEn, IsSystem, ModifiedAt, ModifiedBy)
                      VALUES (@k, @v, @cat, @dt, @dnAr, @dnEn, @sys, @now, @by)",
                    Db.Params(
                        ("@k", setting.Key), ("@v", setting.Value), ("@cat", setting.Category ?? ""),
                        ("@dt", setting.DataType ?? ""), ("@dnAr", setting.DisplayNameAr ?? ""), ("@dnEn", setting.DisplayNameEn ?? ""),
                        ("@sys", setting.IsSystem), ("@now", DateTime.Now), ("@by", setting.ModifiedBy)));
            }
        }

        /// <summary>نفس Upsert أعلاه لكن داخل معاملة تفتحها الخدمة (SetMany) — لا تفتح معاملة هنا.</summary>
        public static void Upsert(DbConnection conn, DbTransaction tx, SettingRecord setting)
        {
            using var checkCmd = Db.CreateCommand(conn, tx, "SELECT COUNT(*) FROM AppSettings WHERE [Key] = @k", Db.Params(("@k", setting.Key)));
            var exists = Convert.ToInt64(checkCmd.ExecuteScalar()) > 0;

            if (exists)
            {
                using var cmd = Db.CreateCommand(conn, tx,
                    "UPDATE AppSettings SET Value = @v, ModifiedAt = @now, ModifiedBy = @by WHERE [Key] = @k",
                    Db.Params(("@v", setting.Value), ("@now", DateTime.Now), ("@by", setting.ModifiedBy), ("@k", setting.Key)));
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = Db.CreateCommand(conn, tx,
                    @"INSERT INTO AppSettings ([Key], Value, Category, DataType, DisplayNameAr, DisplayNameEn, IsSystem, ModifiedAt, ModifiedBy)
                      VALUES (@k, @v, @cat, @dt, @dnAr, @dnEn, @sys, @now, @by)",
                    Db.Params(
                        ("@k", setting.Key), ("@v", setting.Value), ("@cat", setting.Category ?? ""),
                        ("@dt", setting.DataType ?? ""), ("@dnAr", setting.DisplayNameAr ?? ""), ("@dnEn", setting.DisplayNameEn ?? ""),
                        ("@sys", setting.IsSystem), ("@now", DateTime.Now), ("@by", setting.ModifiedBy)));
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>يُدرج الصف فقط لو غير موجود بعد — لا يستبدل قيمة قائمة (يستخدمه SettingSeeder).</summary>
        public static void InsertIfMissing(SettingRecord setting)
        {
            var exists = Db.Scalar("SELECT COUNT(*) FROM AppSettings WHERE [Key] = @k", Db.Params(("@k", setting.Key)));
            if (Convert.ToInt64(exists) > 0) return;

            Db.Execute(
                @"INSERT INTO AppSettings ([Key], Value, Category, DataType, DisplayNameAr, DisplayNameEn, IsSystem)
                  VALUES (@k, @v, @cat, @dt, @dnAr, @dnEn, @sys)",
                Db.Params(
                    ("@k", setting.Key), ("@v", setting.Value), ("@cat", setting.Category ?? ""),
                    ("@dt", setting.DataType ?? ""), ("@dnAr", setting.DisplayNameAr ?? ""), ("@dnEn", setting.DisplayNameEn ?? ""),
                    ("@sys", setting.IsSystem)));
        }
    }
}
