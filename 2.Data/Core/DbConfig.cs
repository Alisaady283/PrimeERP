using System;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace PrimeERP.Data.Core
{
    /// <summary>المحرّكات المدعومة</summary>
    public enum DatabaseProvider
    {
        Sqlite,
        SqlServer,
        PostgreSql
    }

    /// <summary>قدرة المحرّك على النسخ</summary>
    public enum BackupCapability
    {
        FileCopy,
        SqlCommand,
        ExternalTool
    }

    /// <summary>قاعدة البيانات DbConfig</summary>
    public class DbConfig
    {
        public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;
        public string FilePath { get; set; } = "PrimeERP.db";
        public string Host     { get; set; } = "";
        public int    Port     { get; set; }
        public string Database { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool   UseIntegratedSecurity { get; set; } = true;

        public int  CommandTimeout       { get; set; } = 30;
        public bool EnableRetryOnFailure { get; set; } = false;
        public int  MaxRetryCount        { get; set; } = 3;

        private static DbConfig _current;

        /// <summary>الإعداد الحيّ</summary>
        public static DbConfig Current => _current ??= Load();

        public static void Use(DbConfig config) => _current = config;

        /// <summary>سلسلة الاتصال بمحرّكها</summary>
        public string ConnectionString() => Provider switch
        {
            DatabaseProvider.SqlServer => new SqlConnectionStringBuilder
            {
                DataSource = Port > 0 ? $"{Host},{Port}" : Host,
                InitialCatalog = Database,
                TrustServerCertificate = true,
                ConnectTimeout = CommandTimeout,
                IntegratedSecurity = UseIntegratedSecurity,
                UserID = UseIntegratedSecurity ? "" : Username,
                Password = UseIntegratedSecurity ? "" : Password
            }.ConnectionString,

            DatabaseProvider.PostgreSql => new NpgsqlConnectionStringBuilder
            {
                Host = Host,
                Port = Port > 0 ? Port : 5432,
                Database = Database,
                Username = Username,
                Password = Password,
                Timeout = CommandTimeout
            }.ConnectionString,

            _ => $"Data Source={FilePath};Default Timeout={CommandTimeout}"
        };

        public static DbConfig Load()
        {
            var config = new DbConfig();

            try
            {
                var root = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .Build();

                var section = root.GetSection("Database");

                if (Enum.TryParse<DatabaseProvider>(section["Provider"], true, out var parsed))
                    config.Provider = parsed;

                config.FilePath = section["FilePath"] ?? config.FilePath;
                config.Host     = section["Host"] ?? "";
                config.Database = section["Database"] ?? "";
                config.Username = section["Username"] ?? "";
                config.Password = section["Password"] ?? "";

                if (int.TryParse(section["Port"], out var port))
                    config.Port = port;

                if (bool.TryParse(section["UseIntegratedSecurity"], out var integrated))
                    config.UseIntegratedSecurity = integrated;

                if (int.TryParse(section["CommandTimeout"], out var timeout))
                    config.CommandTimeout = timeout;

                if (bool.TryParse(section["EnableRetryOnFailure"], out var retry))
                    config.EnableRetryOnFailure = retry;

                if (int.TryParse(section["MaxRetryCount"], out var maxRetry))
                    config.MaxRetryCount = maxRetry;
            }
            catch
            {
            }

            if (config.Provider == DatabaseProvider.Sqlite && !Path.IsPathRooted(config.FilePath))
            {
                Directory.CreateDirectory(PrimeERP.Platform.AppInfo.DataFolder);
                config.FilePath = Path.Combine(PrimeERP.Platform.AppInfo.DataFolder, config.FilePath);
            }

            return config;
        }
    }
}
