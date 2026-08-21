using PrimeERP.Data.Providers;

namespace PrimeERP.Data.Core
{
    /// <summary>ينتج مزوّد قاعدة البيانات المناسب حسب الإعداد الحالي — نقطة التبديل الوحيدة بين المحركات.</summary>
    public static class DbFactory
    {
        private static DbConfig    _config;
        private static IDbProvider _current;

        /// <summary>يُحمَّل تلقائياً من appsettings.json عند أول استخدام — لا حاجة لاستدعاء تهيئة صريح قبل استخدام أي قطعة بيانات.</summary>
        public static DbConfig Config
        {
            get { EnsureInitialized(); return _config; }
        }

        public static IDbProvider Current
        {
            get { EnsureInitialized(); return _current; }
        }

        public static void Configure(DbConfig config)
        {
            _config  = config;
            _current = config.Provider switch
            {
                DatabaseProvider.SqlServer  => new SqlServerProvider(),
                DatabaseProvider.PostgreSql => new PostgreSqlProvider(),
                _                            => new SqliteProvider()
            };
        }

        /// <summary>يقرأ appsettings.json ويهيّئ المزوّد المناسب — استدعاء صريح اختياري (يحدث تلقائياً عند أول استخدام).</summary>
        public static void Initialize() => Configure(DbConfig.Load());

        private static void EnsureInitialized()
        {
            if (_current == null)
                Initialize();
        }
    }
}
