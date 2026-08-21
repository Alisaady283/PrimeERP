using PrimeERP.Data.Core;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace PrimeERP.Data.Providers
{
    public class SqliteProvider : IDbProvider
    {
        public DatabaseProvider Kind => DatabaseProvider.Sqlite;

        public string LastInsertIdQuery => "SELECT last_insert_rowid()";
        public string CurrentTimestampFunction => "datetime('now')";
        public string BoolTrue  => "1";
        public string BoolFalse => "0";
        public string IntType   => "INTEGER";
        public string BoolType  => "INTEGER";
        public string DateType  => "TEXT";
        public string AutoIncrementPk => "INTEGER PRIMARY KEY AUTOINCREMENT";

        public string TextType(int? length = null) => "TEXT";
        public string DecimalType(int precision = 18, int scale = 4) => "REAL";

        public string QuoteIdentifier(string name) => $"\"{name}\"";
        public string LimitClause(int skip, int take) => $"LIMIT {take} OFFSET {skip}";

        public string CreateTableIfNotExists(string tableName, string columnsAndConstraints) =>
            $"CREATE TABLE IF NOT EXISTS {QuoteIdentifier(tableName)} (\n    {columnsAndConstraints}\n)";

        public string CreateIndexIfNotExists(string indexName, string tableName, string column, bool unique) =>
            $"CREATE {(unique ? "UNIQUE " : "")}INDEX IF NOT EXISTS {QuoteIdentifier(indexName)} " +
            $"ON {QuoteIdentifier(tableName)} ({QuoteIdentifier(column)})";

        public string AppendReturningId(string insertSql, string idColumn = "Id") => insertSql;

        public bool HasAutoRowVersion => false;

        public string RowVersionColumnDdl(string columnName) =>
            $"{QuoteIdentifier(columnName)} INTEGER NOT NULL DEFAULT 1";

        public string ConcurrencyIncrementClause(string columnName) =>
            $"{QuoteIdentifier(columnName)} = {QuoteIdentifier(columnName)} + 1";

        public string BuildConnectionString(DbConfig config) =>
            $"Data Source={config.FilePath};Default Timeout={config.CommandTimeout}";

        public DbConnection CreateConnection(DbConfig config)
        {
            var conn = new SqliteConnection(BuildConnectionString(config));
            conn.Open();

            using var pragma = conn.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();

            return conn;
        }

        public bool SupportsNativeBackup => true;

        public BackupCapability GetBackupCapability() => BackupCapability.FileCopy;

        /// <summary>VACUUM INTO ينسخ قاعدة متماسكة حتى مع اتصال مفتوح — أنظف من نسخ الملف مباشرة.</summary>
        public string BuildBackupCommand(DbConfig config, string targetPath) => "VACUUM INTO @path";

        /// <summary>null عمداً — الاستعادة نسخ ملف مباشر تتولاه الخدمة، لا عبارة SQL لها معنى هنا.</summary>
        public string BuildRestoreCommand(DbConfig config, string sourcePath) => null;

        public string GetDatabaseFilePath(DbConfig config) => config.FilePath;

        public string GetDatabaseName(DbConfig config) => null;

        public string BackupFileExtension => ".db";
    }
}
