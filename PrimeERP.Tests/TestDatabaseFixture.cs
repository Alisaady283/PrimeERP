using PrimeERP.Data.Seeders;
using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.App.Bootstrap;
using PrimeERP.Data.Core;
using PrimeERP.Data.Schema;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Tests
{
    /// <summary>
    /// ملف SQLite مؤقت مستقل لكل تشغيلة اختبار — لا يشارك PrimeERP.db الفعلي، ويُحذف بعد الانتهاء.
    /// يُشارك عبر ICollectionFixture بين كل الاختبارات التي تحتاج قاعدة بيانات حقيقية.
    ///
    /// منذ R3: يبني حاوية DI خاصة به عبر نفس دوال التسجيل الحقيقية (App.Bootstrap.DependencyInjection) —
    /// "حاوية اختبار خاصة" بالمعنى الحرفي، لا محاكاة يدوية منفصلة قد تنحرف عن تسجيل الإنتاج الفعلي. مثيل DI
    /// جديد كلياً لكل TestDatabaseFixture (أي لكل تشغيلة اختبار مستقلة) يعني SettingsService بذاكرة تخزين
    /// مؤقت فارغة دائماً هنا — يُلغي تماماً مشكلة "تسرّب الإعداد بين الاختبارات" التي كانت تُعالَج سابقاً
    /// بـ SettingsService.Instance.Reload() (لا معنى لها بعد zoo static Instance المحذوفة في R3 أصلاً).
    /// </summary>
    public class TestDatabaseFixture : IDisposable
    {
        public string DbPath { get; }

        /// <summary>الحاوية الافتراضية (تسجيل إنتاج كامل بلا تعديل) — تكفي أغلب الاختبارات. لاختبار يحتاج Fake/Mock لخدمة بعينها، استخدم BuildServices(overrides) بدلاً منها.</summary>
        public IServiceProvider Services { get; }

        public TestDatabaseFixture()
        {
            DbPath = Path.Combine(Path.GetTempPath(), $"PrimeERP.Tests.{Guid.NewGuid():N}.db");

            DbFactory.Configure(new DbConfig
            {
                Provider = DatabaseProvider.Sqlite,
                FilePath = DbPath
            });

            SettingRepository.CreateTable();
            SettingSeeder.Seed(); // يزرع SettingKeys.Accounts.Customers="1220"/.Suppliers="2110" — مطابقة لأكواد AccountRepository.SeedDefaults نفسها.
            BackupRepository.CreateTable();
            AccountRepository.CreateTable();
            AccountRepository.SeedDefaults();
            JournalRepository.CreateTable();
            FiscalPeriodRepository.CreateTable();
            NumberSequenceRepository.CreateTable();
            NumberSequenceSeeder.Seed(); // بعد SettingSeeder.Seed() — يقرأ SettingKeys.Documents.CustomerPrefix/SupplierPrefix/ProductPrefix منه.
            CustomerRepository.CreateTable();

            // ينشئ جدول __Migrations (بلا هجرات فعلية مسجَّلة) — BackupService.Validate يتحقق من وجوده
            // كعلامة "هذه فعلاً قاعدة بيانات PrimeERP"، تماماً كقاعدة بيانات حقيقية مُهيَّأة بشكل صحيح.
            MigrationRunner.RunPending();

            Services = BuildServices();
        }

        /// <summary>يبني حاوية DI جديدة بنفس تسجيل الإنتاج — configureOverrides يُستدعى بعده مباشرة، فأي تسجيل فيه (Fake/Mock) يفوز عند الحلّ (آخر تسجيل لنفس النوع هو الفائز في Microsoft.Extensions.DependencyInjection).</summary>
        public static IServiceProvider BuildServices(Action<IServiceCollection> configureOverrides = null)
        {
            var services = new ServiceCollection()
                .AddPlatform()
                .AddData()
                .AddApplication()
                .AddUI()
                .AddModules();

            configureOverrides?.Invoke(services);

            return services.BuildServiceProvider();
        }

        public void Dispose()
        {
            try { File.Delete(DbPath); } catch { /* قد يكون الملف مقفلاً لحظياً — لا يهم في بيئة الاختبار */ }
        }
    }

    [CollectionDefinition("Database")]
    public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture> { }
}
