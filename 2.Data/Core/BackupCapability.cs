namespace PrimeERP.Data.Core
{
    /// <summary>كيف يبني هذا المحرك نسخة احتياطية — يقرأه BackupService ليقرر مساره (نسخ ملف/أمر SQL/أداة خارجية) بلا أي "if" على نوع المحرك.</summary>
    public enum BackupCapability
    {
        /// <summary>نسخ/تصدير ملف قاعدة البيانات مباشرة (SQLite).</summary>
        FileCopy,

        /// <summary>أمر SQL ينفَّذه على الاتصال (BACKUP DATABASE في SQL Server).</summary>
        SqlCommand,

        /// <summary>يحتاج أداة خارجية غير متاحة من داخل التطبيق (pg_dump لـ PostgreSQL).</summary>
        ExternalTool
    }
}
