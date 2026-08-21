using PrimeERP.Data.Core;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace PrimeERP.Data.Providers
{
    public class SqlServerProvider : IDbProvider
    {
        public DatabaseProvider Kind => DatabaseProvider.SqlServer;

        public string LastInsertIdQuery => "SELECT CAST(SCOPE_IDENTITY() AS INT)";
        public string CurrentTimestampFunction => "GETDATE()";
        public string BoolTrue  => "1";
        public string BoolFalse => "0";
        public string IntType   => "INT";
        public string BoolType  => "BIT";
        public string DateType  => "DATETIME2";
        public string AutoIncrementPk => "INT IDENTITY(1,1) PRIMARY KEY";

        public string TextType(int? length = null) =>
            length.HasValue ? $"NVARCHAR({length.Value})" : "NVARCHAR(MAX)";
        public string DecimalType(int precision = 18, int scale = 4) => $"DECIMAL({precision},{scale})";

        public string QuoteIdentifier(string name) => $"[{name}]";

        public string LimitClause(int skip, int take) =>
            $"OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY";

        public string CreateTableIfNotExists(string tableName, string columnsAndConstraints) =>
            $"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}')\n" +
            $"BEGIN\n    CREATE TABLE {QuoteIdentifier(tableName)} (\n    {columnsAndConstraints}\n    )\nEND";

        public string CreateIndexIfNotExists(string indexName, string tableName, string column, bool unique) =>
            $"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = '{indexName}' AND object_id = OBJECT_ID('{tableName}'))\n" +
            $"    CREATE {(unique ? "UNIQUE " : "")}INDEX {QuoteIdentifier(indexName)} " +
            $"ON {QuoteIdentifier(tableName)} ({QuoteIdentifier(column)})";

        public string AppendReturningId(string insertSql, string idColumn = "Id") => insertSql;

        public bool HasAutoRowVersion => true;

        /// <summary>SQL Server يحدّث rowversion تلقائياً — لا يقبل NOT NULL ولا قيمة افتراضية.</summary>
        public string RowVersionColumnDdl(string columnName) =>
            $"{QuoteIdentifier(columnName)} ROWVERSION";

        /// <summary>فارغ عمداً — rowversion في SQL Server ممنوع تعديله يدوياً، المحرك يزيده تلقائياً.</summary>
        public string ConcurrencyIncrementClause(string columnName) => "";

        public string BuildConnectionString(DbConfig config)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = config.Port > 0 ? $"{config.Host},{config.Port}" : config.Host,
                InitialCatalog = config.Database,
                TrustServerCertificate = true,
                ConnectTimeout = config.CommandTimeout
            };

            if (config.UseIntegratedSecurity)
                builder.IntegratedSecurity = true;
            else
            {
                builder.UserID = config.Username;
                builder.Password = config.Password;
            }

            return builder.ConnectionString;
        }

        public DbConnection CreateConnection(DbConfig config)
        {
            var conn = new SqlConnection(BuildConnectionString(config));
            conn.Open();
            return conn;
        }

        public bool SupportsNativeBackup => true;

        public BackupCapability GetBackupCapability() => BackupCapability.SqlCommand;

        public string BuildBackupCommand(DbConfig config, string targetPath) =>
            $"BACKUP DATABASE {QuoteIdentifier(config.Database)} TO DISK = @path WITH INIT";

        public string BuildRestoreCommand(DbConfig config, string sourcePath) =>
            $"RESTORE DATABASE {QuoteIdentifier(config.Database)} FROM DISK = @path WITH REPLACE, RECOVERY";

        public string GetDatabaseFilePath(DbConfig config) => null;

        public string GetDatabaseName(DbConfig config) => config.Database;

        public string BackupFileExtension => ".bak";
    }
}
