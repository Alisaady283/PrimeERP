using PrimeERP.Data.Core;
using System;
using System.Data.Common;
using Npgsql;

namespace PrimeERP.Data.Providers
{
    public class PostgreSqlProvider : IDbProvider
    {
        public DatabaseProvider Kind => DatabaseProvider.PostgreSql;

        public string LastInsertIdQuery => "SELECT lastval()";
        public string CurrentTimestampFunction => "NOW()";
        public string BoolTrue  => "TRUE";
        public string BoolFalse => "FALSE";
        public string IntType   => "INTEGER";
        public string BoolType  => "BOOLEAN";
        public string DateType  => "TIMESTAMP";
        public string AutoIncrementPk => "SERIAL PRIMARY KEY";

        public string TextType(int? length = null) =>
            length.HasValue ? $"VARCHAR({length.Value})" : "TEXT";
        public string DecimalType(int precision = 18, int scale = 4) => $"NUMERIC({precision},{scale})";

        public string QuoteIdentifier(string name) => $"\"{name}\"";
        public string LimitClause(int skip, int take) => $"LIMIT {take} OFFSET {skip}";

        public string CreateTableIfNotExists(string tableName, string columnsAndConstraints) =>
            $"CREATE TABLE IF NOT EXISTS {QuoteIdentifier(tableName)} (\n    {columnsAndConstraints}\n)";

        public string CreateIndexIfNotExists(string indexName, string tableName, string column, bool unique) =>
            $"CREATE {(unique ? "UNIQUE " : "")}INDEX IF NOT EXISTS {QuoteIdentifier(indexName)} " +
            $"ON {QuoteIdentifier(tableName)} ({QuoteIdentifier(column)})";

        public string ExistingColumnsQuery(string tableName) =>
            $"SELECT column_name AS \"ColumnName\" FROM information_schema.columns WHERE table_name = '{tableName}'";

        public string AddColumnSql(string tableName, string columnDdl) =>
            $"ALTER TABLE {QuoteIdentifier(tableName)} ADD COLUMN {columnDdl}";

        /// <summary>PostgreSQL يرجع المعرف الجديد مباشرة من عبارة الإدراج نفسها بلا استعلام إضافي.</summary>
        public string AppendReturningId(string insertSql, string idColumn = "Id") =>
            $"{insertSql} RETURNING {QuoteIdentifier(idColumn)}";

        public bool HasAutoRowVersion => false;

        public string RowVersionColumnDdl(string columnName) =>
            $"{QuoteIdentifier(columnName)} INTEGER NOT NULL DEFAULT 1";

        public string ConcurrencyIncrementClause(string columnName) =>
            $"{QuoteIdentifier(columnName)} = {QuoteIdentifier(columnName)} + 1";

        public string BuildConnectionString(DbConfig config)
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host     = config.Host,
                Port     = config.Port > 0 ? config.Port : 5432,
                Database = config.Database,
                Username = config.Username,
                Password = config.Password,
                Timeout  = config.CommandTimeout
            };
            return builder.ConnectionString;
        }

        public DbConnection CreateConnection(DbConfig config)
        {
            var conn = new NpgsqlConnection(BuildConnectionString(config));
            conn.Open();
            return conn;
        }

        public bool SupportsNativeBackup => false;

        public BackupCapability GetBackupCapability() => BackupCapability.ExternalTool;

        public string BuildBackupCommand(DbConfig config, string targetPath) =>
            throw new NotSupportedException("النسخ الاحتياطي لقاعدة PostgreSQL يحتاج أداة pg_dump خارجية — غير مدعوم من داخل التطبيق حالياً.");

        public string BuildRestoreCommand(DbConfig config, string sourcePath) =>
            throw new NotSupportedException("استعادة قاعدة PostgreSQL تحتاج أداة pg_restore/psql خارجية — غير مدعومة من داخل التطبيق حالياً.");

        public string GetDatabaseFilePath(DbConfig config) => null;

        public string GetDatabaseName(DbConfig config) => config.Database;

        public string BackupFileExtension => ".sql";
    }
}
