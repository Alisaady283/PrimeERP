using System;
using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace PrimeERP.Data.Core
{
    /// <summary>سياقٌ فوق الاتصال والمعاملة القائمين</summary>
    public static class DbContextFactory
    {
        private static readonly ConcurrentDictionary<(DatabaseProvider, string), DbContextOptions<PrimeDbContext>> Options = new();

        /// <summary>سياقٌ جديد</summary>
        public static PrimeDbContext Open(PrimeDbContext borrowed = null)
        {
            if (borrowed != null) return borrowed;

            var config = DbConfig.Current;
            return new PrimeDbContext(Options.GetOrAdd((config.Provider, config.ConnectionString()), key =>
            {
                var options = new DbContextOptionsBuilder<PrimeDbContext>()
                    .ReplaceService<IModelCacheKeyFactory, BuiltModelCacheKeyFactory>();
                Use(options, key.Item1, key.Item2);
                return options.Options;
            }));
        }

        /// <summary>سياقٌ فوق اتصالٍ أجنبي</summary>
        public static PrimeDbContext On(DbConnection conn)
        {
            var options = new DbContextOptionsBuilder<PrimeDbContext>()
                .ReplaceService<IModelCacheKeyFactory, BuiltModelCacheKeyFactory>();

            Use(options, DbConfig.Current.Provider, conn);
            return new PrimeDbContext(options.Options);
        }

        /// <summary>معاملةٌ ذرّية بسياقٍ واحد</summary>
        public static void RunTransaction(Action<PrimeDbContext> action) =>
            RunTransaction<object>(db => { action(db); return null; });

        public static T RunTransaction<T>(Func<PrimeDbContext, T> action)
        {
            using var db = Open();
            using var tx = db.Database.BeginTransaction();
            try
            {
                var result = action(db);
                tx.Commit();
                return result;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private static void Use(DbContextOptionsBuilder builder, DatabaseProvider provider, DbConnection conn)
        {
            switch (provider)
            {
                case DatabaseProvider.SqlServer:  builder.UseSqlServer(conn); break;
                case DatabaseProvider.PostgreSql: builder.UseNpgsql(conn); break;
                default:                          builder.UseSqlite(conn); break;
            }
        }

        private static void Use(DbContextOptionsBuilder builder, DatabaseProvider provider, string connectionString)
        {
            switch (provider)
            {
                case DatabaseProvider.SqlServer:  builder.UseSqlServer(connectionString); break;
                case DatabaseProvider.PostgreSql: builder.UseNpgsql(connectionString); break;
                default:                          builder.UseSqlite(connectionString); break;
            }
        }
    }
}
