using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;

namespace PrimeERP.Data.Core
{
    /// <summary>
    /// طبقة وصول عامة لقاعدة البيانات — تعمل فوق أي محرك عبر DbFactory.Current،
    /// ولا تحتوي أي SQL أو نوع خاص بمحرك معين.
    /// </summary>
    public static class DbHelper
    {
        public static DbConnection GetConnection()
        {
            var config = DbFactory.Config;

            if (!config.EnableRetryOnFailure)
                return DbFactory.Current.CreateConnection(config);

            int maxAttempts = Math.Max(1, config.MaxRetryCount);
            Exception lastError = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return DbFactory.Current.CreateConnection(config);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    if (attempt < maxAttempts)
                        Thread.Sleep(200 * attempt);
                }
            }

            throw lastError;
        }

        public static Dictionary<string, object> Params(params (string Name, object Value)[] items)
        {
            var dict = new Dictionary<string, object>();
            foreach (var (name, value) in items)
                dict[name] = value ?? DBNull.Value;
            return dict;
        }

        /// <summary>
        /// يقرأ النتيجة صفاً صفاً بدل DataTable.Load — الأخيرة تستنتج قيود المخطط من القارئ، فتضيف قيد
        /// تفرّد على أي عمود مصدره عمود UNIQUE في الجدول (EntryNo مثلاً). حينها أي استعلام يُعيد نفس المستند
        /// في أكثر من صف (سطرا قيد على نفس الحساب — وهو مسموح بإعداد AllowDuplicateAccountInEntry) يرمي
        /// ConstraintException بدل إعادة الصفوف. القراءة هنا بلا استنتاج مخطط: النتيجة بيانات لا جدول قاعدة.
        /// </summary>
        private static DataTable ReadTable(DbDataReader reader)
        {
            var table = new DataTable();
            for (int i = 0; i < reader.FieldCount; i++)
                table.Columns.Add(reader.GetName(i), typeof(object));

            while (reader.Read())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                table.Rows.Add(values);
            }

            return table;
        }

        public static DataTable Query(string sql, IDictionary<string, object> parameters = null)
        {
            using var conn = GetConnection();
            using var cmd  = NewCommand(conn);
            cmd.CommandText = sql;
            AddParams(cmd, parameters);

            using var reader = cmd.ExecuteReader();
            return ReadTable(reader);
        }

        /// <summary>
        /// نفس Query أعلاه لكن على اتصال/معاملة قائمة بالفعل — ضروري لأي قراءة تحدث من داخل معاملة خدمة أخرى
        /// مفتوحة على نفس الخيط (RunTransaction لخدمة تستدعي (conn,tx) في خدمة أخرى). فتح اتصال جديد (Query
        /// العادية) في هذه الحالة يعلّق (deadlock) على SQLite: القفل الكتابي للمعاملة الخارجية لا يُحرَّر حتى
        /// يعود الاستدعاء المتزامن على نفس الخيط، فانتظار اتصال جديد لقفل قراءة/كتابة ينتظر نفسه فعلياً.
        /// اكتُشفت هذه الحالة فعلياً أثناء بناء JournalService (F.2.3) — Post/Unpost/Create (conn,tx) تحتاج
        /// قراءة سطور/حسابات وهي داخل معاملة FiscalPeriodService.CloseYear/ReopenYear الخارجية.
        /// </summary>
        public static DataTable Query(DbConnection conn, DbTransaction tx, string sql, IDictionary<string, object> parameters = null)
        {
            using var cmd = NewCommand(conn, tx);
            cmd.CommandText = sql;
            AddParams(cmd, parameters);

            using var reader = cmd.ExecuteReader();
            return ReadTable(reader);
        }

        public static int Execute(string sql, IDictionary<string, object> parameters = null)
        {
            using var conn = GetConnection();
            using var cmd  = NewCommand(conn);
            cmd.CommandText = sql;
            AddParams(cmd, parameters);
            return cmd.ExecuteNonQuery();
        }

        public static int Execute(DbConnection conn, DbTransaction tx, string sql, IDictionary<string, object> parameters = null)
        {
            using var cmd = NewCommand(conn, tx);
            cmd.CommandText = sql;
            AddParams(cmd, parameters);
            return cmd.ExecuteNonQuery();
        }

        public static object Scalar(string sql, IDictionary<string, object> parameters = null)
        {
            using var conn = GetConnection();
            using var cmd  = NewCommand(conn);
            cmd.CommandText = sql;
            AddParams(cmd, parameters);
            return cmd.ExecuteScalar();
        }

        /// <summary>
        /// ينفّذ عبارة INSERT ويرجع المعرف الجديد — يتعامل مع فروقات المحركات داخلياً:
        /// PostgreSQL عبر RETURNING في نفس العبارة، وSQLite/SQL Server عبر استعلام تالٍ على نفس الاتصال.
        /// </summary>
        public static int InsertAndGetId(string sql, IDictionary<string, object> parameters = null,
                                         string idColumn = "Id")
        {
            using var conn = GetConnection();
            return InsertAndGetId(conn, null, sql, parameters, idColumn);
        }

        public static int InsertAndGetId(DbConnection conn, DbTransaction tx, string sql,
                                         IDictionary<string, object> parameters = null,
                                         string idColumn = "Id")
        {
            var provider = DbFactory.Current;

            if (provider.Kind == DatabaseProvider.PostgreSql)
            {
                using var cmd = NewCommand(conn, tx);
                cmd.CommandText = provider.AppendReturningId(sql, idColumn);
                AddParams(cmd, parameters);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }

            using (var cmd = NewCommand(conn, tx))
            {
                cmd.CommandText = sql;
                AddParams(cmd, parameters);
                cmd.ExecuteNonQuery();
            }

            using var idCmd = NewCommand(conn, tx);
            idCmd.CommandText = provider.LastInsertIdQuery;
            return Convert.ToInt32(idCmd.ExecuteScalar());
        }

        /// <summary>
        /// ينفّذ تحديثاً بشرط تطابق نسخة الصف (Optimistic Concurrency) — عبارة SQL يجب أن
        /// تتضمن شرط "RowVersion = @expectedVersion" في WHERE (وزيادة العمود يدوياً في SET
        /// للمحركات التي لا تحدّثه تلقائياً — انظر IDbProvider.ConcurrencyIncrementClause).
        /// يرجع false إن لم يتأثر أي صف، أي أن مستخدماً آخر عدّل الصف بين القراءة والحفظ.
        /// </summary>
        public static bool ExecuteWithConcurrencyCheck(string sql, object expectedVersion,
                                                        IDictionary<string, object> parameters = null)
        {
            var allParams = parameters != null
                ? new Dictionary<string, object>(parameters)
                : new Dictionary<string, object>();
            allParams["@expectedVersion"] = expectedVersion ?? DBNull.Value;

            return Execute(sql, allParams) > 0;
        }

        public static void RunTransaction(Action<DbConnection, DbTransaction> action)
        {
            using var conn = GetConnection();
            using var tx   = conn.BeginTransaction();
            try
            {
                action(conn, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static T RunTransaction<T>(Func<DbConnection, DbTransaction, T> action)
        {
            // لا تُفوَّض لـ RunTransaction(Action<...>) — لامدا "result = action(conn, tx)" ترجع T فعلياً،
            // فيلتقطها حلّ التحميل الزائد (Overload Resolution) كاستدعاء ذاتي متكرر لهذه الدالة نفسها
            // (تطابق Func<...,T> أدق من تحويلها لـ Action مع تجاهل القيمة) — تكرار لا نهائي حتى Stack Overflow.
            // مكتشَفة فعلياً هنا (أول استدعاء حقيقي لهذه الدالة في المشروع)، لا نظرياً.
            using var conn = GetConnection();
            using var tx   = conn.BeginTransaction();
            try
            {
                var result = action(conn, tx);
                tx.Commit();
                return result;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static DbCommand CreateCommand(DbConnection conn, DbTransaction tx, string sql,
                                              IDictionary<string, object> parameters = null)
        {
            var cmd = NewCommand(conn, tx);
            cmd.CommandText = sql;
            AddParams(cmd, parameters);
            return cmd;
        }

        private static DbCommand NewCommand(DbConnection conn, DbTransaction tx = null)
        {
            var cmd = conn.CreateCommand();
            if (tx != null) cmd.Transaction = tx;
            cmd.CommandTimeout = DbFactory.Config.CommandTimeout;
            return cmd;
        }

        private static void AddParams(DbCommand cmd, IDictionary<string, object> parameters)
        {
            if (parameters == null) return;
            foreach (var kv in parameters)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = kv.Key;
                p.Value = kv.Value ?? DBNull.Value;
                cmd.Parameters.Add(p);
            }
        }

        public static void Backup(string backupFilePath)
        {
            if (DbFactory.Current.Kind != DatabaseProvider.Sqlite)
                throw new NotSupportedException("النسخ الاحتياطي المباشر مدعوم لقاعدة SQLite فقط — للمحركات الأخرى استخدم أدوات النسخ الخاصة بالمحرك.");

            using var source = (Microsoft.Data.Sqlite.SqliteConnection)GetConnection();
            using var dest   = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={backupFilePath}");
            dest.Open();
            source.BackupDatabase(dest);
        }
    }
}
