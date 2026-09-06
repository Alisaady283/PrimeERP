using System;
using PrimeERP.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Assets;
using PrimeERP.Application.Services.HR;
using PrimeERP.Application.Services.Security;
using PrimeERP.Application.Services.Sales;
using PrimeERP.Application.Services.Purchasing;
using PrimeERP.Application.Services.Backup;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Print;
using PrimeERP.Composition.Registry;
using PrimeERP.Data.Core;
using PrimeERP.Data.Migrations;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Seeders;
using PrimeERP.Domain.Contracts;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels;
using PrimeERP.Modules;

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
            services.AddSingleton<PermissionService>();
            services.AddSingleton<IPermissionService>(sp => sp.GetRequiredService<PermissionService>());
            services.AddSingleton<IPermissionAdminService>(sp => sp.GetRequiredService<PermissionService>());
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
            services.AddSingleton<ICategoryRepository, CategoryRepository>();
            services.AddSingleton<IProductRepository, ProductRepository>();
            services.AddSingleton<IAssetRepository, AssetRepository>();
            services.AddSingleton<IEmployeeRepository, EmployeeRepository>();
            services.AddSingleton<IDepartmentRepository, DepartmentRepository>();
            services.AddSingleton<IJobTitleRepository, JobTitleRepository>();
            services.AddSingleton<IUnitRepository, UnitRepository>();
            services.AddSingleton<IWarehouseRepository, WarehouseRepository>();
            services.AddSingleton<IStockMovementRepository, StockMovementRepository>();
            services.AddSingleton<IDocumentLinkRepository, DocumentLinkRepository>();
            services.AddSingleton<ITreasuryRepository, TreasuryRepository>();
            services.AddSingleton<IVoucherRepository, VoucherRepository>();
            services.AddSingleton<IChequeRepository, ChequeRepository>();
            services.AddSingleton<ISalesInvoiceRepository, SalesInvoiceRepository>();
            services.AddSingleton<IPurchaseInvoiceRepository, PurchaseInvoiceRepository>();
            services.AddSingleton<ISalesReturnRepository, SalesReturnRepository>();
            services.AddSingleton<IPurchaseReturnRepository, PurchaseReturnRepository>();
            services.AddSingleton<IStockInRepository, StockInRepository>();
            services.AddSingleton<IStockOutRepository, StockOutRepository>();
            services.AddSingleton<IPurchaseRequestRepository, PurchaseRequestRepository>();
            services.AddSingleton<IPurchaseOrderRepository, PurchaseOrderRepository>();
            services.AddSingleton<IQuotationRepository, QuotationRepository>();
            services.AddSingleton<ISalesOrderRepository, SalesOrderRepository>();
            services.AddSingleton<IGoodsReceiptRepository, GoodsReceiptRepository>();
            services.AddSingleton<IGoodsIssueRepository, GoodsIssueRepository>();
            services.AddSingleton<IDeliveryNoteRepository, DeliveryNoteRepository>();
            services.AddSingleton<ISalesReceiptRepository, SalesReceiptRepository>();
            services.AddSingleton<IStockTransferRepository, StockTransferRepository>();
            services.AddSingleton<IPayrollRepository, PayrollRepository>();
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
            services.AddSingleton<ICategoryService, CategoryService>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddSingleton<IAssetService, AssetService>();
            services.AddSingleton<IEmployeeService, EmployeeService>();
            services.AddSingleton<IDepartmentService, DepartmentService>();
            services.AddSingleton<IJobTitleService, JobTitleService>();
            services.AddSingleton<IUnitService, UnitService>();
            services.AddSingleton<IWarehouseService, WarehouseService>();
            services.AddSingleton<IStockService, StockService>();
            services.AddSingleton<PrimeERP.Application.Services.Documents.IDocumentLinkService,
                                  PrimeERP.Application.Services.Documents.DocumentLinkService>();

            // محرّك السحب العام — يخدم الواجهة (IPullService) والتحقق داخل الخدمات (IPullSourceReader) بنسخة واحدة.
            services.AddSingleton<PrimeERP.Composition.Pull.PullService>();
            services.AddSingleton<PrimeERP.Composition.Pull.IPullService>(sp => sp.GetRequiredService<PrimeERP.Composition.Pull.PullService>());
            services.AddSingleton<PrimeERP.Domain.Contracts.IPullSourceReader>(sp => sp.GetRequiredService<PrimeERP.Composition.Pull.PullService>());
            services.AddSingleton<ISalesInvoiceService, SalesInvoiceService>();
            services.AddSingleton<IPurchaseInvoiceService, PurchaseInvoiceService>();
            services.AddSingleton<ISalesReturnService, SalesReturnService>();
            services.AddSingleton<IPurchaseReturnService, PurchaseReturnService>();
            services.AddSingleton<IStockInService, StockInService>();
            services.AddSingleton<IStockOutService, StockOutService>();
            services.AddSingleton<PrimeERP.Application.Services.Documents.IPurchaseRequestService, PrimeERP.Application.Services.Documents.PurchaseRequestService>();
            services.AddSingleton<PrimeERP.Application.Services.Documents.IPurchaseOrderService, PrimeERP.Application.Services.Documents.PurchaseOrderService>();
            services.AddSingleton<PrimeERP.Application.Services.Documents.IQuotationService, PrimeERP.Application.Services.Documents.QuotationService>();
            services.AddSingleton<PrimeERP.Application.Services.Documents.ISalesOrderService, PrimeERP.Application.Services.Documents.SalesOrderService>();
            services.AddSingleton<IGoodsReceiptService, GoodsReceiptService>();
            services.AddSingleton<IGoodsIssueService, GoodsIssueService>();
            services.AddSingleton<IDeliveryNoteService, DeliveryNoteService>();
            services.AddSingleton<ISalesReceiptService, SalesReceiptService>();
            services.AddSingleton<IStockTransferService, StockTransferService>();
            services.AddSingleton<PrimeERP.Application.Services.Treasury.ITreasuryService, PrimeERP.Application.Services.Treasury.TreasuryService>();
            services.AddSingleton<PrimeERP.Application.Services.Vouchers.IReceiptVoucherService, PrimeERP.Application.Services.Vouchers.ReceiptVoucherService>();
            services.AddSingleton<PrimeERP.Application.Services.Vouchers.IPaymentVoucherService, PrimeERP.Application.Services.Vouchers.PaymentVoucherService>();
            services.AddSingleton<PrimeERP.Application.Services.Print.IChequePrinter, PrimeERP.Application.Services.Print.ChequePrinter>();
            services.AddSingleton<PrimeERP.Application.Services.Assets.IAssetDepreciationService, PrimeERP.Application.Services.Assets.AssetDepreciationService>();
            services.AddSingleton<PrimeERP.Application.Services.Accounting.IOpeningBalanceService, PrimeERP.Application.Services.Accounting.OpeningBalanceService>();
            services.AddSingleton<PrimeERP.Application.Services.Cheques.IChequeService, PrimeERP.Application.Services.Cheques.ChequeService>();
            services.AddSingleton<PrimeERP.Application.Services.Cheques.IChequeReceiptDocumentService, PrimeERP.Application.Services.Cheques.ChequeReceiptDocumentService>();
            services.AddSingleton<PrimeERP.Application.Services.Cheques.IChequeIssueDocumentService, PrimeERP.Application.Services.Cheques.ChequeIssueDocumentService>();
            services.AddSingleton<IPayrollService, PayrollService>();
            services.AddSingleton<IRoleService, RoleService>();
            services.AddSingleton<IUserService, UserService>();

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
            services.AddSingleton<IPrintDialogHost, PrintDialogHost>();

            // ViewModels: Transient — حالة خاصة بعرض واحد (SelectedItem/Filter/Page)، لا تُشارَك بين فتحات
            // الصفحة المتعددة عكس الخدمات (Singleton طوال عمر التطبيق).
            services.AddTransient<TreasuriesViewModel>();
            services.AddTransient<ReceiptVouchersViewModel>();
            services.AddTransient<PaymentVouchersViewModel>();
            services.AddTransient<ChequesViewModel>();
            services.AddTransient<OpeningBalancesViewModel>();
            services.AddTransient<ChequeReceiptsViewModel>();
            services.AddTransient<ChequeIssuesViewModel>();
            services.AddTransient<CustomersViewModel>();
            services.AddTransient<SuppliersViewModel>();
            services.AddTransient<AccountsViewModel>();
            services.AddTransient<JournalsViewModel>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<CategoriesLookupViewModel>();
            services.AddTransient<BrandsViewModel>();
            services.AddTransient<UnitsViewModel>();
            services.AddTransient<WarehousesViewModel>();
            services.AddTransient<AssetCategoriesViewModel>();
            services.AddTransient<DepartmentsViewModel>();
            services.AddTransient<JobTitlesViewModel>();
            services.AddTransient<AssetsViewModel>();
            services.AddTransient<EmployeesViewModel>();
            services.AddTransient<RolesViewModel>();
            services.AddTransient<UsersViewModel>();
            services.AddTransient<SalesInvoicesViewModel>();
            services.AddTransient<PurchaseInvoicesViewModel>();
            services.AddTransient<SalesReturnsViewModel>();
            services.AddTransient<PurchaseReturnsViewModel>();
            services.AddTransient<StockInViewModel>();
            services.AddTransient<StockOutViewModel>();
            services.AddTransient<PurchaseRequestViewModel>();
            services.AddTransient<PurchaseOrderViewModel>();
            services.AddTransient<QuotationViewModel>();
            services.AddTransient<SalesOrderViewModel>();
            services.AddTransient<GoodsReceiptViewModel>();
            services.AddTransient<GoodsIssueViewModel>();
            services.AddTransient<DeliveryNoteViewModel>();
            services.AddTransient<SalesReceiptViewModel>();
            services.AddTransient<StockTransferViewModel>();
            services.AddTransient<PayrollViewModel>();

            return services;
        }

        /// <summary>7.Composition — IModuleRegistry وحدها حتى الآن (Definitions/Renderers ثابتة، لا تحتاج DI).</summary>
        public static IServiceCollection AddComposition(this IServiceCollection services)
        {
            services.AddSingleton<IModuleRegistry, ModuleRegistry>();
            return services;
        }

        /// <summary>يسجّل الوحدات الفعلية (R8: Customers/Suppliers كإثبات) في IModuleRegistry المبنية في
        /// AddComposition — يُستدعى بعد BuildServiceProvider في App.xaml.cs (يحتاج IServiceProvider جاهزاً
        /// لحلّ IModuleRegistry، لا IServiceCollection وقت التسجيل).</summary>
        public static IServiceProvider RegisterModules(this IServiceProvider services)
        {
            ModuleRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            ReportRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            PermissionModuleRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            CycleVoucherRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            CycleDocumentRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            TreasuryRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());

            // PrintService لا تفتح نوافذ بنفسها — تُحقن واجهتها المرئية هنا (نفس نمط IDocumentExporter).
            services.GetRequiredService<IPrintService>().DialogHost = services.GetRequiredService<IPrintDialogHost>();
            return services;
        }

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

            MigrationRunner.Register("2026_08_RenumberAccountCodes", AccountCodeRenumberMigration.Apply);

            services.GetRequiredService<IJournalRepository>().CreateTable();
            services.GetRequiredService<IFiscalPeriodRepository>().CreateTable();

            var numberSequences = services.GetRequiredService<INumberSequenceRepository>();
            numberSequences.CreateTable();
            NumberSequenceSeeder.Seed(numberSequences);

            services.GetRequiredService<ICustomerRepository>().CreateTable();
            services.GetRequiredService<ISupplierRepository>().CreateTable();
            services.GetRequiredService<ICategoryRepository>().CreateTable();
            services.GetRequiredService<IProductRepository>().CreateTable();
            services.GetRequiredService<IAssetRepository>().CreateTable();
            services.GetRequiredService<IEmployeeRepository>().CreateTable();
            services.GetRequiredService<IDepartmentRepository>().CreateTable();
            services.GetRequiredService<IJobTitleRepository>().CreateTable();
            services.GetRequiredService<IUnitRepository>().CreateTable();
            services.GetRequiredService<IWarehouseRepository>().CreateTable();
            services.GetRequiredService<IStockMovementRepository>().CreateTable();
            services.GetRequiredService<ISalesInvoiceRepository>().CreateTable();
            services.GetRequiredService<IPurchaseInvoiceRepository>().CreateTable();
            services.GetRequiredService<ISalesReturnRepository>().CreateTable();
            services.GetRequiredService<IPurchaseReturnRepository>().CreateTable();
            services.GetRequiredService<IStockInRepository>().CreateTable();
            services.GetRequiredService<IStockOutRepository>().CreateTable();
            services.GetRequiredService<IPurchaseRequestRepository>().CreateTable();
            services.GetRequiredService<IPurchaseOrderRepository>().CreateTable();
            services.GetRequiredService<IQuotationRepository>().CreateTable();
            services.GetRequiredService<ISalesOrderRepository>().CreateTable();
            services.GetRequiredService<IGoodsReceiptRepository>().CreateTable();
            services.GetRequiredService<IGoodsIssueRepository>().CreateTable();
            services.GetRequiredService<IDeliveryNoteRepository>().CreateTable();
            services.GetRequiredService<ISalesReceiptRepository>().CreateTable();
            services.GetRequiredService<IStockTransferRepository>().CreateTable();
            services.GetRequiredService<IPayrollRepository>().CreateTable();
            services.GetRequiredService<IDocumentLinkRepository>().CreateTable();
            services.GetRequiredService<ITreasuryRepository>().CreateTable();
            var treasuryService = services.GetRequiredService<PrimeERP.Application.Services.Treasury.ITreasuryService>();
            treasuryService.RepairLinkedRoots();
            treasuryService.SeedDefaults();
            services.GetRequiredService<IVoucherRepository>().CreateTable();
            services.GetRequiredService<IChequeRepository>().CreateTable();

            // ⚠️ R9 — نفس درس توقف 7: PermissionDb (جداول Permissions/Roles/RolePermissions/Users/
            // UserPermissions + بذر دور SystemAdmin ومستخدم admin) كانت مبنية بالكامل منذ وقت طويل بلا أي
            // استدعاء فعلي من مسار حي — Login/الصلاحيات الحقيقية لم يكونا ممكنَين إطلاقاً قبل هذا السطر.
            PermissionDb.CreateTables();
            PermissionDb.SeedDefaults();

            MigrationRunner.RunPending();

            return services;
        }
    }
}
