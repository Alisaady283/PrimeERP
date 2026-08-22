using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Backup;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Print;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Contracts;
using PrimeERP.Platform.Design;
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
            services.AddSingleton<ISettingsService, SettingsService>();
            return services;
        }

        public static IServiceCollection AddData(this IServiceCollection services)
        {
            services.AddSingleton<IAccountRepository, AccountRepository>();
            services.AddSingleton<ICustomerRepository, CustomerRepository>();
            services.AddSingleton<IJournalRepository, JournalRepository>();
            services.AddSingleton<IFiscalPeriodRepository, FiscalPeriodRepository>();
            services.AddSingleton<INumberSequenceRepository, NumberSequenceRepository>();
            services.AddSingleton<IBackupRepository, BackupRepository>();
            return services;
        }

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<INumberSequenceService, NumberSequenceService>();
            services.AddSingleton<IAccountService, AccountService>();
            services.AddSingleton<IJournalService, JournalService>();
            services.AddSingleton<IFiscalPeriodService, FiscalPeriodService>();
            services.AddSingleton<ICustomerService, CustomerService>();
            services.AddSingleton<IBackupService, BackupService>();
            services.AddSingleton<IPrintService, PrintService>();

            // Lazy<IJournalService> يكسر الدائرية الحقيقية JournalService↔FiscalPeriodService — راجع تعليق
            // التوثيق أعلى FiscalPeriodService.cs. لا يبني IJournalService الآن، فقط عند أول .Value فعلي.
            services.AddSingleton(sp => new Lazy<IJournalService>(() => sp.GetRequiredService<IJournalService>()));

            // TEMPORARY — يُحذف في R6: ISupplierService بلا تنفيذ بعد (راجع ISupplierService.cs)، فلا تسجيل هنا عمداً.
            // لا تبنِ عليه. AccountService.ResolveAutoLink يحلّها عبر IServiceProvider.GetService (يرجع null بأمان لو غير مسجَّلة).
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
    }
}
