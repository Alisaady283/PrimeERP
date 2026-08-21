using System.Data.Common;

namespace PrimeERP.Data.Core
{
    /// <summary>
    /// يوحّد الاختلافات بين محركات قواعد البيانات (SQLite/SQL Server/PostgreSQL)
    /// بحيث تبقى بقية طبقة البيانات كتابة واحدة تعمل فوق أي محرك.
    /// </summary>
    public interface IDbProvider
    {
        DatabaseProvider Kind { get; }

        string LastInsertIdQuery { get; }
        string CurrentTimestampFunction { get; }
        string BoolTrue  { get; }
        string BoolFalse { get; }
        string IntType   { get; }
        string BoolType  { get; }
        string DateType  { get; }
        string AutoIncrementPk { get; }

        string TextType(int? length = null);
        string DecimalType(int precision = 18, int scale = 4);

        string QuoteIdentifier(string name);
        string LimitClause(int skip, int take);

        string CreateTableIfNotExists(string tableName, string columnsAndConstraints);
        string CreateIndexIfNotExists(string indexName, string tableName, string column, bool unique);

        /// <summary>
        /// يهيّئ عبارة الإدراج لإرجاع المعرف الجديد مباشرة (PostgreSQL يضيف RETURNING).
        /// المحركات التي لا تدعم ذلك (SQLite/SQL Server) ترجع نفس العبارة بلا تعديل —
        /// المعرف يُجلب بعدها عبر استعلام منفصل (LastInsertIdQuery) على نفس الاتصال.
        /// </summary>
        string AppendReturningId(string insertSql, string idColumn = "Id");

        /// <summary>true للمحركات التي تُحدّث عمود التزامن تلقائياً بدون تدخل من التطبيق (SQL Server rowversion).</summary>
        bool HasAutoRowVersion { get; }

        /// <summary>تعريف عمود التزامن الكامل (نوع + قيمة افتراضية) حسب المحرك.</summary>
        string RowVersionColumnDdl(string columnName);

        /// <summary>
        /// جزء SET الذي يزيد نسخة الصف يدوياً — فارغ للمحركات ذات rowversion تلقائي (SQL Server)
        /// لأن العمود هناك ممنوع تعديله يدوياً.
        /// </summary>
        string ConcurrencyIncrementClause(string columnName);

        string BuildConnectionString(DbConfig config);
        DbConnection CreateConnection(DbConfig config);

        // ===== نسخ احتياطي/استعادة — كل اختلاف بين المحركات هنا، لا "if" على نوع المحرك في BackupService =====

        /// <summary>true لو المحرك يدعم نسخاً احتياطياً من داخل التطبيق (بلا أداة خارجية).</summary>
        bool SupportsNativeBackup { get; }

        BackupCapability GetBackupCapability();

        /// <summary>عبارة SQL تُنفَّذ لأخذ نسخة (بارامتر @path للمسار الهدف) — يرمي NotSupportedException لو المحرك لا يدعم (PostgreSQL).</summary>
        string BuildBackupCommand(DbConfig config, string targetPath);

        /// <summary>عبارة SQL تُنفَّذ للاستعادة (بارامتر @path للمسار المصدر)، أو null لو الاستعادة نسخ ملف مباشر (SQLite) — يرمي NotSupportedException لو غير مدعوم (PostgreSQL).</summary>
        string BuildRestoreCommand(DbConfig config, string sourcePath);

        /// <summary>مسار ملف القاعدة الفعلي — SQLite فقط، null لباقي المحركات.</summary>
        string GetDatabaseFilePath(DbConfig config);

        /// <summary>اسم القاعدة كما يُستخدم في أوامر SQL Server/PostgreSQL — null لـ SQLite.</summary>
        string GetDatabaseName(DbConfig config);

        /// <summary>امتداد ملف النسخة الاحتياطية الطبيعي لهذا المحرك.</summary>
        string BackupFileExtension { get; }
    }
}
