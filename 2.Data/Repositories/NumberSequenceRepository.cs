using System;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;

namespace PrimeERP.Data.Repositories
{
    /// <summary>طبقة وصول بيانات تسلسل الأرقام — SQL خام فقط. حساب إعادة التصفير السنوي والرقم التالي مسؤولية NumberSequenceService.</summary>
    public class NumberSequenceRepository : RepositoryBase<NumberSequenceRow>, INumberSequenceRepository
    {
        protected override string TableName => "NumberSequences";

        protected override NumberSequenceRow Map(DataRow row) => new(
            row["Prefix"] == DBNull.Value ? row["Key"].ToString() : row["Prefix"].ToString(),
            Convert.ToInt32(row["NextNumber"]),
            Convert.ToInt32(row["Padding"]),
            Convert.ToBoolean(row["ResetYearly"]),
            row["LastYear"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["LastYear"]));

        public void CreateTable() =>
            SchemaBuilder.Table("NumberSequences")
                .Id()
                .Text("Key", 50, required: true, unique: true)
                .Text("Prefix", 20)
                .Int("NextNumber", nullable: false, defaultValue: 1)
                .Int("Padding", nullable: false, defaultValue: 5)
                .Bool("ResetYearly", defaultValue: true)
                .Int("LastYear")
                .Create();

        /// <summary>ينشئ صفاً افتراضياً للمفتاح لو غير موجود بعد (البادئة = المفتاح نفسه) — بلا تأثير لو موجود (Idempotent).</summary>
        public void EnsureRow(string key) => EnsureRow(key, key);

        /// <summary>نفس EnsureRow أعلاه لكن ببادئة مخصَّصة مختلفة عن المفتاح — يستخدمها NumberSequenceSeeder (المفتاح "Customer" ثابت، البادئة "C-" من الإعدادات).</summary>
        public void EnsureRow(string key, string prefix) => EnsureRowCore(null, null, key, prefix);

        /// <summary>نفس EnsureRow أعلاه لكن عبر (conn,tx) قائمة — يستخدمها NumberSequenceService.Next(conn,tx,...) عندما يُستدعى من داخل معاملة خدمة أخرى مفتوحة (JournalService.Create(conn,tx,...))؛ اتصال منفصل هنا يُعلِّق (deadlock) على SQLite.</summary>
        public void EnsureRow(DbConnection conn, DbTransaction tx, string key) => EnsureRowCore(conn, tx, key, key);

        private void EnsureRowCore(DbConnection conn, DbTransaction tx, string key, string prefix)
        {
            var exists = conn != null
                ? QueryAs(r => true, "SELECT 1 FROM NumberSequences WHERE [Key] = @k LIMIT 1", conn, tx, ("@k", key)).Count > 0
                : Convert.ToInt64(Scalar("SELECT COUNT(*) FROM NumberSequences WHERE [Key] = @k", ("@k", key))) > 0;
            if (exists) return;

            Exec("INSERT INTO NumberSequences ([Key], Prefix, NextNumber, Padding, ResetYearly) VALUES (@k, @p, 1, 5, @r)",
                conn, tx, ("@k", key), ("@p", prefix), ("@r", true));
        }

        public NumberSequenceRow GetRow(string key) =>
            QueryOne("SELECT * FROM NumberSequences WHERE [Key] = @k", null, null, ("@k", key));

        public NumberSequenceRow GetRow(DbConnection conn, DbTransaction tx, string key) =>
            QueryOne("SELECT * FROM NumberSequences WHERE [Key] = @k", conn, tx, ("@k", key));

        public void UpdateNext(DbConnection conn, DbTransaction tx, string key, int nextNumber, int year) =>
            Exec("UPDATE NumberSequences SET NextNumber = @n, LastYear = @y WHERE [Key] = @k",
                conn, tx, ("@n", nextNumber), ("@y", year), ("@k", key));
    }
}
