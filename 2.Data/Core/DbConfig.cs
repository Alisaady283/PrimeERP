using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace PrimeERP.Data.Core
{
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

        /// <summary>يقرأ إعدادات قاعدة البيانات من appsettings.json — بقيم افتراضية آمنة (SQLite محلي) عند غيابه.</summary>
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
                // appsettings.json غير موجود أو غير صالح — القيم الافتراضية (SQLite محلي) تكفي
            }

            // مسار ثابت خارج bin/ عمداً — تنظيف بناء (rm -rf bin/obj) كان يمسح قاعدة البيانات معه فعلياً
            // طالما عاشت داخل مجلد الإخراج نفسه؛ AppData يبقى حتى مع أعنف تنظيف بناء.
            if (config.Provider == DatabaseProvider.Sqlite && !Path.IsPathRooted(config.FilePath))
            {
                var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimeERP");
                Directory.CreateDirectory(dataDir);
                config.FilePath = Path.Combine(dataDir, config.FilePath);
            }

            return config;
        }
    }
}
