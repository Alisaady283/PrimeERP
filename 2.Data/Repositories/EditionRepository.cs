using System.Data.Common;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    /// <summary>اتصالٌ بقاعدة نسخةٍ أخرى</summary>
    public interface IEditionRepository
    {
        DbConnection Open(string databasePath);
    }

    public class EditionRepository : IEditionRepository
    {
        public DbConnection Open(string databasePath)
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                new DbConfig { Provider = DatabaseProvider.Sqlite, FilePath = databasePath }.ConnectionString());

            connection.Open();
            return connection;
        }
    }
}
