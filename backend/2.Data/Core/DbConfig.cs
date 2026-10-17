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

        /// <summary>ملف الإعداد بجوار البرنامج</summary>
        public static string SettingsFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        /// <summary>الإعداد بمساره النهائي: الملف النسبي يسكن مجلد بيانات البرنامج</summary>
        public static DbConfig Load()
        {
            var config = Read(SettingsFile);
            if (config.Provider == DatabaseProvider.Sqlite && !Path.IsPathRooted(config.FilePath))
            {
                Directory.CreateDirectory(PrimeERP.Platform.AppInfo.DataFolder);
                config.FilePath = Path.Combine(PrimeERP.Platform.AppInfo.DataFolder, config.FilePath);
            }
            return config;
        }

        /// <summary>قسم Database كما هو مكتوبٌ في الملف</summary>
        public static DbConfig Read(string settingsFile)
        {
            var config = new DbConfig();

            try
            {
                var root = new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(Path.GetFullPath(settingsFile)))
                    .AddJsonFile(Path.GetFileName(settingsFile), optional: true, reloadOnChange: false)
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

            return config;
        }

        /// <summary>يكتب قسم Database ويُبقي باقي الملف، ويُنشئ مجلد ملف SQLite</summary>
        public void Save(string settingsFile)
        {
            var settings = File.Exists(settingsFile)
                ? System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(settingsFile)) ?? new System.Text.Json.Nodes.JsonObject()
                : new System.Text.Json.Nodes.JsonObject();

            settings["Database"] = new System.Text.Json.Nodes.JsonObject
            {
                ["Provider"] = Provider.ToString(),
                ["FilePath"] = FilePath,
                ["Host"] = Host,
                ["Port"] = Port,
                ["Database"] = Database,
                ["Username"] = Username,
                ["Password"] = Password,
                ["UseIntegratedSecurity"] = UseIntegratedSecurity,
                ["CommandTimeout"] = CommandTimeout,
                ["EnableRetryOnFailure"] = EnableRetryOnFailure,
                ["MaxRetryCount"] = MaxRetryCount,
            };

            if (Provider == DatabaseProvider.Sqlite && Path.GetDirectoryName(FilePath) is { Length: > 0 } folder)
                Directory.CreateDirectory(folder);
            File.WriteAllText(settingsFile, settings.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
