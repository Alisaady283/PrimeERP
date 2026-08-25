using System;
using PrimeERP.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Backup;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Print;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Seeders;
using PrimeERP.Domain.Contracts;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Services;

namespace PrimeERP.App.Bootstrap
{
    /// <summary>
    /// تسجيل كل خدمة حقيقية في حاوية DI — بديل ServiceLocator (R3، محذوف نهائياً). كل دالة تسجّل طبقة واحدة
    /// فقط، بنفس ترتيب اعتمادها (Platform أولاً، Modules أخيراً) — يعكس مصفوفة الاعتماد في ARCHITECTURE.md.
    /// كل خدمة Singleton (مثيل واحد طوال عمر التطبيق) — نفس دلالة "Instance" القديمة، لكن الحاوية تديرها الآن
    /// لا حقل static يدوي.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddPlatform(this IServiceCollection services)
        {
            services.AddSingleton<IPermissionService, PermissionService>();
            services.AddSingleton<ISettingsProvider, SettingsProvider>();
            services.AddSingleton<IAuditLogger, AuditLogger>();
            services.AddSingleton<ILocalizationService, LocalizationAdapter>();
            return services;
        }

        public static IServiceCollection AddData(this IServiceCollection services)
        {
            services.AddSingleton<IAccountRepository, AccountRepository>();
            services.AddSingleton<ICustomerRepository, CustomerRepository>();
            services.AddSingleton<ISupplierRepository, SupplierRepository>();
            services.AddSingleton<IJournalRepository, JournalRepository>();
            services.AddSingleton<IFiscalPeriodRepository, FiscalPeriodRepository>();
            services.AddSingleton<INumberSequenceRepository, NumberSequenceRepository>();
            services.AddSingleton<IBackupRepository, BackupRepository>();
            return services;
        }

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<INumberSequenceService, NumberSequenceService>();
            services.AddSingleton<IAccountService, AccountService>();
            services.AddSingleton<IJournalService, JournalService>();
            services.AddSingleton<IFiscalPeriodService, FiscalPeriodService>();
            services.AddSingleton<ICustomerService, CustomerService>();
            services.AddSingleton<ISupplierService, SupplierService>();
            services.AddSingleton<IBackupService, BackupService>();
            services.AddSingleton<IPrintService, PrintService>();

            // Lazy<IJournalService> يكسر الدائرية الحقيقية JournalService↔FiscalPeriodService — راجع تعليق
            // التوثيق أعلى FiscalPeriodService.cs. لا يبني IJournalService الآن، فقط عند أول .Value فعلي.
            services.AddSingleton(sp => new Lazy<IJournalService>(() => sp.GetRequiredService<IJournalService>()));

            return services;
        }

        public static IServiceCollection AddUI(this IServiceCollection services)
        {
            services.AddSingleton<IIdentityService, IdentityService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IToastService, ToastService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // ExportService تنفّذ IExportService و IDocumentExporter معاً — مثيل واحد مشترك بين الاثنين،
            // لا مثيلان منفصلان (كلاهما يستهلك نفس الحالة الداخلية — لا حالة فعلياً هنا، لكن مبدأ Singleton واحد للنوع).
            services.AddSingleton<ExportService>();
            services.AddSingleton<IExportService>(sp => sp.GetRequiredService<ExportService>());
            services.AddSingleton<IDocumentExporter>(sp => sp.GetRequiredService<ExportService>());

            return services;
        }

        /// <summary>لا موديولات مبنية بعد (7.Composition/8.Modules فارغتان حتى R8/R9). موجودة هنا لإكمال
        /// شكل سلسلة التسجيل — AddPlatform().AddData().AddApplication().AddUI().AddModules() في App.xaml.cs.</summary>
        public static IServiceCollection AddModules(this IServiceCollection services) => services;

        /// <summary>
        /// ⚠️ توقف 7 — الحلقة المفقودة: التطبيق الحقيقي (App.xaml.cs.OnStartup) لم يكن يستدعي هذه السلسلة
        /// إطلاقاً — فقط TestDatabaseFixture كانت تفعل (لكل تشغيلة اختبار). يعني هذا أن أي تشغيل فعلي لـ
        /// PrimeERP.exe كان سيفشل عند أول قراءة إعداد حقيقية (بما فيها IIdentityService.Initialize نفسها —
        /// تُكتشِف الآن بمحاولة تشغيل فعلية أولى للتطبيق). نفس تسلسل TestDatabaseFixture بالضبط، حرفياً — لا
        /// انحراف عن "حاوية اختبار خاصة، لا محاكاة منفصلة قد تنحرف عن تسجيل الإنتاج" لأنها الآن تُستدعى من
        /// الإنتاج نفسه أولاً. CreateTable آمنة للاستدعاء المتكرر (IF NOT EXISTS)، والزارعون (Seeders) تتحقق من
        /// عدم التكرار داخلياً — فاستدعاؤها في كل إقلاع فعلي آمن ومطلوب (قاعدة جديدة أو ترقية Schema لاحقة).
        /// </summary>
        public static IServiceProvider EnsureDatabaseReady(this IServiceProvider services)
        {
            SettingRepository.CreateTable();
            SettingSeeder.Seed();

            services.GetRequiredService<IBackupRepository>().CreateTable();

            var accounts = services.GetRequiredService<IAccountRepository>();
            accounts.CreateTable();
            accounts.SeedDefaults();

            services.GetRequiredService<IJournalRepository>().CreateTable();
            services.GetRequiredService<IFiscalPeriodRepository>().CreateTable();

            var numberSequences = services.GetRequiredService<INumberSequenceRepository>();
            numberSequences.CreateTable();
            NumberSequenceSeeder.Seed(numberSequences);

            services.GetRequiredService<ICustomerRepository>().CreateTable();
            services.GetRequiredService<ISupplierRepository>().CreateTable();

            MigrationRunner.RunPending();

            return services;
        }
    }
}
