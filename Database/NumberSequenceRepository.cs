using System;
using System.Data;
using System.Data.Common;
using PrimeERP.Core.Database;
using Db = PrimeERP.Core.Database.DbHelper;

namespace PrimeERP.Database
{
    /// <summary>طبقة وصول بيانات تسلسل الأرقام — SQL خام فقط. حساب إعادة التصفير السنوي والرقم التالي مسؤولية NumberSequenceService.</summary>
    public static class NumberSequenceRepository
    {
        public record Row(string Prefix, int NextNumber, int Padding, bool ResetYearly, int? LastYear);

        public static void CreateTable()
        {
            SchemaBuilder.Table("NumberSequences")
                .Id()
                .Text("Key", 50, required: true, unique: true)
                .Text("Prefix", 20)
                .Int("NextNumber", nullable: false, defaultValue: 1)
                .Int("Padding", nullable: false, defaultValue: 5)
                .Bool("ResetYearly", defaultValue: true)
                .Int("LastYear")
                .Create();
        }

        /// <summary>ينشئ صفاً افتراضياً للمفتاح لو غير موجود بعد (البادئة = المفتاح نفسه) — بلا تأثير لو موجود (Idempotent).</summary>
        public static void EnsureRow(string key) => EnsureRow(key, key);

        /// <summary>نفس EnsureRow أعلاه لكن ببادئة مخصَّصة مختلفة عن المفتاح — يستخدمها NumberSequenceSeeder (المفتاح "Customer" ثابت، البادئة "C-" من الإعدادات).</summary>
        public static void EnsureRow(string key, string prefix)
        {
            var exists = Db.Scalar("SELECT COUNT(*) FROM NumberSequences WHERE [Key] = @k", Db.Params(("@k", key)));
            if (Convert.ToInt64(exists) > 0) return;

            Db.Execute(
                "INSERT INTO NumberSequences ([Key], Prefix, NextNumber, Padding, ResetYearly) VALUES (@k, @p, 1, 5, @r)",
                Db.Params(("@k", key), ("@p", prefix), ("@r", true)));
        }

        /// <summary>نفس EnsureRow أعلاه لكن عبر (conn,tx) قائمة — يستخدمها NumberSequenceService.Next(conn,tx,...) عندما يُستدعى من داخل معاملة خدمة أخرى مفتوحة (JournalService.Create(conn,tx,...))؛ اتصال منفصل هنا يُعلِّق (deadlock) على SQLite.</summary>
        public static void EnsureRow(DbConnection conn, DbTransaction tx, string key)
        {
            using var checkCmd = Db.CreateCommand(conn, tx, "SELECT COUNT(*) FROM NumberSequences WHERE [Key] = @k", Db.Params(("@k", key)));
            if (Convert.ToInt64(checkCmd.ExecuteScalar()) > 0) return;

            using var insertCmd = Db.CreateCommand(conn, tx,
                "INSERT INTO NumberSequences ([Key], Prefix, NextNumber, Padding, ResetYearly) VALUES (@k, @p, 1, 5, @r)",
                Db.Params(("@k", key), ("@p", key), ("@r", true)));
            insertCmd.ExecuteNonQuery();
        }

        public static Row GetRow(string key)
        {
            var row = Db.Query("SELECT * FROM NumberSequences WHERE [Key] = @k", Db.Params(("@k", key))).Rows[0];
            return MapRow(row);
        }

        public static Row GetRow(DbConnection conn, DbTransaction tx, string key)
        {
            using var cmd = Db.CreateCommand(conn, tx, "SELECT * FROM NumberSequences WHERE [Key] = @k", Db.Params(("@k", key)));
            using var reader = cmd.ExecuteReader();
            reader.Read();
            return new Row(
                reader["Prefix"] == DBNull.Value ? key : reader["Prefix"].ToString(),
                Convert.ToInt32(reader["NextNumber"]),
                Convert.ToInt32(reader["Padding"]),
                Convert.ToBoolean(reader["ResetYearly"]),
                reader["LastYear"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["LastYear"]));
        }

        public static void UpdateNext(DbConnection conn, DbTransaction tx, string key, int nextNumber, int year)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE NumberSequences SET NextNumber = @n, LastYear = @y WHERE [Key] = @k",
                Db.Params(("@n", nextNumber), ("@y", year), ("@k", key)));
            cmd.ExecuteNonQuery();
        }

        private static Row MapRow(DataRow row) => new(
            row["Prefix"] == DBNull.Value ? row["Key"].ToString() : row["Prefix"].ToString(),
            Convert.ToInt32(row["NextNumber"]),
            Convert.ToInt32(row["Padding"]),
            Convert.ToBoolean(row["ResetYearly"]),
            row["LastYear"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["LastYear"]));
    }
}
