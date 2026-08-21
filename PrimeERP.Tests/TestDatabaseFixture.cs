using System;
using System.IO;
using PrimeERP.Core.Database;
using PrimeERP.Database;
using PrimeERP.Services.Settings;

namespace PrimeERP.Tests
{
    /// <summary>
    /// ملف SQLite مؤقت مستقل لكل تشغيلة اختبار — لا يشارك PrimeERP.db الفعلي، ويُحذف بعد الانتهاء.
    /// يُشارك عبر ICollectionFixture بين كل الاختبارات التي تحتاج قاعدة بيانات حقيقية.
    /// </summary>
    public class TestDatabaseFixture : IDisposable
    {
        public string DbPath { get; }

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

            // SettingsService.Instance مفرد ثابت مشترك بين كل الاختبارات (لا يُعاد إنشاؤه لكل TestDatabaseFixture)،
            // ويحتفظ بذاكرة تخزين مؤقت (_cache) في الذاكرة لا تعرف تلقائياً أن قاعدة البيانات تغيّرت هنا —
            // بلا هذا الإبطال، اختبار سابق يستدعي SettingsService.Set(...) يُسرّب قيمته لاختبار لاحق يستخدم
            // قاعدة بيانات جديدة كلياً لم تُطلب منها هذه القيمة إطلاقاً (اكتُشف فعلياً: Create_DuplicateAccount_
            // Fails_ByDefault يفشل بسبب AllowDuplicateAccountInEntry المتروكة true من اختبار سابق).
            SettingsService.Instance.Reload();
        }

        public void Dispose()
        {
            try { File.Delete(DbPath); } catch { /* قد يكون الملف مقفلاً لحظياً — لا يهم في بيئة الاختبار */ }
        }
    }

    [CollectionDefinition("Database")]
    public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture> { }
}
