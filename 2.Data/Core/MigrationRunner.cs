using PrimeERP.Data.Schema;
using System;
using System.Collections.Generic;
using System.Data;

namespace PrimeERP.Data.Core
{
    public class Migration
    {
        public string Name  { get; set; }
        public Action Apply { get; set; }
    }

    /// <summary>يطبّق التعديلات الهيكلية الناقصة فقط عند التشغيل، ويسجّل ما تم تطبيقه في __Migrations.</summary>
    public static class MigrationRunner
    {
        private static readonly List<Migration> _migrations = new();

        /// <summary>
        /// الترحيل يُسجَّل باسمه: تسجيلٌ ثانٍ بالاسم نفسه يستبدل الأول ولا يُضاعفه. القائمة ساكنة تعيش
        /// عمر العملية، وتهيئة القاعدة قد تُستدعى أكثر من مرّة فيها — فالإلحاق الأعمى كان يُدرِج الاسم
        /// مرّتين في نفس RunPending ويكسر قيد التفرّد في __Migrations.
        /// </summary>
        public static void Register(string name, Action apply)
        {
            _migrations.RemoveAll(m => m.Name == name);
            _migrations.Add(new Migration { Name = name, Apply = apply });
        }

        private static void EnsureMigrationsTable()
        {
            SchemaBuilder.Table("__Migrations")
                .Id()
                .Text("Name", 200, required: true, unique: true)
                .DateCol("AppliedAt")
                .Create();
        }

        private static HashSet<string> GetApplied()
        {
            var dt = DbHelper.Query("SELECT Name FROM __Migrations");
            var set = new HashSet<string>();
            foreach (DataRow row in dt.Rows)
                set.Add(row["Name"].ToString());
            return set;
        }

        public static void RunPending()
        {
            EnsureMigrationsTable();
            var applied = GetApplied();

            foreach (var migration in _migrations)
            {
                if (applied.Contains(migration.Name)) continue;

                migration.Apply();

                DbHelper.Execute(
                    "INSERT INTO __Migrations (Name, AppliedAt) VALUES (@name, @date)",
                    DbHelper.Params(("name", migration.Name),
                                    ("date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))));
            }
        }
    }
}
