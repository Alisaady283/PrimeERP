using PrimeERP.Data.Seeders;
using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.App.Bootstrap;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Tests.Helpers;
using PrimeERP.Platform;
using PrimeERP.Data;
using PrimeERP.Application;

namespace PrimeERP.Tests
{
    /// <summary>ملف SQLite مؤقت مستقل لكل</summary>
    public class TestDatabaseFixture : IDisposable
    {
        public string DbPath { get; }

        public IServiceProvider Services { get; }

        public TestDatabaseFixture()
        {
            // ⚠️ registers the pack:// scheme before any print test
            WpfApplicationFixture.Ensure();

            DbPath = TestPaths.Unique("PrimeERP.Tests", ".db");

            DbConfig.Use(new DbConfig
            {
                Provider = DatabaseProvider.Sqlite,
                FilePath = DbPath
            });

            Services = BuildServices();

        }

        public static IServiceProvider BuildServices(Action<IServiceCollection> configureOverrides = null)
        {
            var services = new ServiceCollection()
                .AddPlatform()
                .AddData()
                .AddApplication()
                .AddUI()
                .AddComposition();

            configureOverrides?.Invoke(services);

            var provider = services.BuildServiceProvider();

            provider.EnsureDatabaseReady(PrimeERP.UI.Services.LanguageResources.Apply);
            provider.RegisterModules();
            return provider;
        }

        public IPermissionStore Permissions => Services.GetRequiredService<IPermissionStore>();

        /// <summary>مستخدمٌ بكلمة مرور مُلبَّدة</summary>
        public int AddUser(string username, string password, string displayName, int roleId, bool isActive = true)
        {
            var (hash, salt) = PrimeERP.Platform.Security.PasswordHasher.Hash(password);
            return Permissions.InsertUser(new PrimeERP.Domain.Entities.User
            {
                Username = username, PasswordHash = hash, Salt = salt,
                DisplayName = displayName, RoleId = roleId, IsActive = isActive
            });
        }

        public void Dispose() => TestPaths.DeleteDatabase(DbPath);
    }

    [CollectionDefinition("Database")]
    public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture> { }
}
