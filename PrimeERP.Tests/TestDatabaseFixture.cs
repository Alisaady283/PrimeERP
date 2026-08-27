using PrimeERP.Data.Seeders;
using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.App.Bootstrap;
using PrimeERP.Data.Core;
using PrimeERP.Data.Schema;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Permissions;
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
            // ⚠️ توقف 11 — يضمن تسجيل مخطَّط pack:// (عبر System.Windows.Application) قبل أي اختبار طباعة،
            // بصرف النظر عن ترتيب xUnit غير الحتمي؛ TestDatabaseFixture تُبنى في كل اختبار تقريباً فهذا أول
            // نقطة مضمونة التنفيذ قبل أي منطق اختبار فعلي. راجع WpfApplicationFixture وARCHITECTURE.md.
            WpfApplicationFixture.Ensure();

            DbPath = Path.Combine(Path.GetTempPath(), $"PrimeERP.Tests.{Guid.NewGuid():N}.db");

            DbFactory.Configure(new DbConfig
            {
                Provider = DatabaseProvider.Sqlite,
                FilePath = DbPath
            });

            Services = BuildServices();

            SettingRepository.CreateTable();
            SettingSeeder.Seed();
            Services.GetRequiredService<IBackupRepository>().CreateTable();
            var accounts = Services.GetRequiredService<IAccountRepository>();
            accounts.CreateTable();
            accounts.SeedDefaults();
            Services.GetRequiredService<IJournalRepository>().CreateTable();
            Services.GetRequiredService<IFiscalPeriodRepository>().CreateTable();
            var numberSequences = Services.GetRequiredService<INumberSequenceRepository>();
            numberSequences.CreateTable();
            NumberSequenceSeeder.Seed(numberSequences);
            Services.GetRequiredService<ICustomerRepository>().CreateTable();
            Services.GetRequiredService<ISupplierRepository>().CreateTable();
            Services.GetRequiredService<ICategoryRepository>().CreateTable();

            // CreateTables فقط، لا SeedDefaults — الأخيرة تلف ~80 صلاحية (Permissions+RolePermissions) عبر
            // استعلامات فردية غير مُجمَّعة بمعاملة واحدة؛ رخيصة في الإنتاج (مرة واحدة فقط، تتحقق من عدم
            // التكرار فتتخطى كل شيء من التشغيلة الثانية) لكنها مكلفة هنا لأن كل TestDatabaseFixture يبني
            // قاعدة SQLite جديدة فارغة فتُنفَّذ كاملة من الصفر في كل مرة — تراكم هذا عبر عشرات فئات الاختبار
            // تسبَّب فعلياً في تعليق/تعطُّل مضيف الاختبار على التشغيلة الكاملة (⚠️ توقف 11، ARCHITECTURE.md).
            // لا اختبار حالي يحتاج بيانات مزروعة فعلية (الكل عبر AppSession.DevMode=true) — الجداول فقط
            // تكفي لسلامة القيود الأجنبية إن استُهلكت لاحقاً.
            PermissionDb.CreateTables();

            MigrationRunner.RunPending();
        }

        /// <summary>يبني حاوية DI جديدة بنفس تسجيل الإنتاج — configureOverrides يُستدعى بعده مباشرة، فأي تسجيل فيه (Fake/Mock) يفوز عند الحلّ (آخر تسجيل لنفس النوع هو الفائز في Microsoft.Extensions.DependencyInjection).</summary>
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
            provider.RegisterModules();
            return provider;
        }

        public void Dispose()
        {
            try { File.Delete(DbPath); } catch { /* قد يكون الملف مقفلاً لحظياً — لا يهم في بيئة الاختبار */ }
        }
    }

    [CollectionDefinition("Database")]
    public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture> { }
}
