using System;
using PrimeERP.Core.Database.Providers;

namespace PrimeERP.Core.Database
{
    public static class DbConnectionTester
    {
        public static (bool Success, string Message) Test(DbConfig config)
        {
            try
            {
                IDbProvider provider = config.Provider switch
                {
                    DatabaseProvider.SqlServer  => new SqlServerProvider(),
                    DatabaseProvider.PostgreSql => new PostgreSqlProvider(),
                    _                            => new SqliteProvider()
                };

                using var conn = provider.CreateConnection(config);
                return (true, "تم الاتصال بقاعدة البيانات بنجاح");
            }
            catch (Exception ex)
            {
                return (false, $"فشل الاتصال بقاعدة البيانات:\n{ex.Message}");
            }
        }
    }
}
