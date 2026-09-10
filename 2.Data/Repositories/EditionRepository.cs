using System.Data.Common;
using PrimeERP.Data.Core;
using PrimeERP.Data.Providers;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    /// <summary>
    /// اتصالٌ بقاعدة نسخةٍ أخرى. قاعدة البرنامج العامل تمرّ من DbFactory (اتصال واحد للعملية)، وقاعدة
    /// النسخة ليست هي — فالاتصال يُفتح هنا صراحةً، ثم تكتب فيه مستودعاتُ النظام نفسها بتحميلاتها التي
    /// تقبل اتصالاً. لا جملة SQL تُكتب هنا.
    /// </summary>
    public interface IEditionRepository
    {
        DbConnection Open(string databasePath);
    }

    public class EditionRepository : IEditionRepository
    {
        public DbConnection Open(string databasePath)
        {
            var connection = new SqliteProvider().CreateConnection(
                new DbConfig { Provider = DatabaseProvider.Sqlite, FilePath = databasePath });

            connection.Open();
            return connection;
        }
    }
}
