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
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Data.Repositories.Base;
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
using PrimeERP.Application.Services.Admin;

namespace PrimeERP.App.Bootstrap
{
    /// <summary>تسجيل الخدمات في الحاوية</summary>
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
            services.AddSingleton<ISettingStore, SettingRepository>();
            services.AddSingleton<IPermissionStore, PermissionRepository>();
            services.AddSingleton<IAuditStore, AuditRepository>();
            services.AddSingleton<IAccountRepository, AccountRepository>();
            services.AddSingleton<IPartyRepository<Customer>, CustomerRepository>();
            services.AddSingleton<IPartyRepository<Supplier>, SupplierRepository>();
            services.AddSingleton<IJournalRepository, JournalRepository>();
            services.AddSingleton<IFiscalPeriodRepository, FiscalPeriodRepository>();
            services.AddSingleton<INumberSequenceRepository, NumberSequenceRepository>();
            services.AddSingleton<IBackupRepository, BackupRepository>();
            services.AddSingleton<IEditionRepository, EditionRepository>();
            services.AddSingleton<ILicenseRepository, LicenseRepository>();
            services.AddSingleton<PrimeERP.Platform.Net.IHttpGateway, PrimeERP.Platform.Net.HttpGateway>();
            services.AddSingleton<ICategoryRepository, CategoryRepository>();
            services.AddSingleton<IProductRepository, ProductRepository>();
            services.AddSingleton<IAssetRepository, AssetRepository>();
            services.AddSingleton<IAssetRevaluationRepository, AssetRevaluationRepository>();
            services.AddSingleton<IAssetDepreciationRepository, AssetDepreciationRepository>();
            services.AddSingleton<IAssetDisposalRepository, AssetDisposalRepository>();
            services.AddSingleton<IEmployeeRepository, EmployeeRepository>();
            services.AddSingleton<ILookupRepository<Department>>(_ => new LookupRepository<Department>("Departments"));
            services.AddSingleton<ILookupRepository<JobTitle>>(_ => new LookupRepository<JobTitle>("JobTitles"));
            services.AddSingleton<ILookupRepository<Unit>>(_ => new LookupRepository<Unit>("Units"));
            services.AddSingleton<ILookupRepository<Warehouse>>(_ => new LookupRepository<Warehouse>("Warehouses"));
            services.AddSingleton<IBuilderRepository, BuilderRepository>();
            services.AddSingleton<PrimeERP.Application.Services.Builder.IBuilderCatalog, PrimeERP.Application.Services.Builder.BuilderCatalog>();
            services.AddSingleton<PrimeERP.Application.Services.Builder.BuilderSectionsService>();
            services.AddSingleton<PrimeERP.Application.Services.Builder.BuilderModulesService>();
            services.AddSingleton<PrimeERP.Application.Services.Builder.BuilderColumnsService>();
            services.AddSingleton<PrimeERP.Application.Services.Builder.BuilderActionsService>();
            services.AddSingleton<PrimeERP.Application.Services.Builder.BuilderFiltersService>();
            services.AddSingleton<IStockMovementRepository, StockMovementRepository>();
            services.AddSingleton<IDocumentLinkRepository, DocumentLinkRepository>();
            services.AddSingleton<ITreasuryRepository, TreasuryRepository>();
            services.AddSingleton<IVoucherRepository, VoucherRepository>();
            services.AddSingleton<IChequeRepository, ChequeRepository>();
            services.AddSingleton<IInvoiceRepository<SalesInvoice, SalesInvoiceLine>, SalesInvoiceRepository>();
            services.AddSingleton<IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine>, PurchaseInvoiceRepository>();
            services.AddSingleton<IReturnRepository<SalesReturn, SalesReturnLine>, SalesReturnRepository>();
            services.AddSingleton<IReturnRepository<PurchaseReturn, PurchaseReturnLine>, PurchaseReturnRepository>();
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
            services.AddSingleton<IEmployeeAllowanceRepository, EmployeeAllowanceRepository>();
            services.AddSingleton<IEmployeeDeductionRepository, EmployeeDeductionRepository>();
            services.AddSingleton<IAttendanceRepository, AttendanceRepository>();
            return services;
        }

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<INumberSequenceService, NumberSequenceService>();
            services.AddSingleton<IAccountService, AccountService>();
            services.AddSingleton<IJournalService, JournalService>();
            services.AddSingleton<PrimeERP.Application.Reporting.IFinancialStatementService, PrimeERP.Application.Reporting.FinancialStatementService>();
            services.AddSingleton<PrimeERP.Application.Reporting.IStockReportService, PrimeERP.Application.Reporting.StockReportService>();
            services.AddSingleton<PrimeERP.Application.Reporting.IPartyReportService, PrimeERP.Application.Reporting.PartyReportService>();
            services.AddSingleton<PrimeERP.Application.Reporting.ISalesReportService, PrimeERP.Application.Reporting.SalesReportService>();
            services.AddSingleton<PrimeERP.Application.Reporting.IAssetReportService, PrimeERP.Application.Reporting.AssetReportService>();
            services.AddSingleton<PrimeERP.Application.Reporting.IPayslipReportService, PrimeERP.Application.Reporting.PayslipReportService>();
            services.AddSingleton<PrimeERP.Application.Reporting.IBuilderReportService, PrimeERP.Application.Reporting.BuilderReportService>();
            services.AddSingleton<IFiscalPeriodService, FiscalPeriodService>();
            services.AddSingleton<ICustomerService, CustomerService>();
            services.AddSingleton<ISupplierService, SupplierService>();
            services.AddSingleton<IBackupService, BackupService>();
            services.AddSingleton<PrimeERP.Application.Services.Admin.IProgramEditionService, PrimeERP.Application.Services.Admin.ProgramEditionService>();
            services.AddSingleton<PrimeERP.Application.Services.Admin.ILicenseService, PrimeERP.Application.Services.Admin.LicenseService>();
            services.AddSingleton<PrimeERP.Application.Services.Admin.IUpdateService, PrimeERP.Application.Services.Admin.UpdateService>();
            services.AddSingleton<IPrintService, PrintService>();
            services.AddSingleton<ICategoryService, CategoryService>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddSingleton<IAssetService, AssetService>();
            services.AddSingleton<PrimeERP.Application.Services.Assets.IAssetRevaluationService, PrimeERP.Application.Services.Assets.AssetRevaluationService>();
            services.AddSingleton<IEmployeeService, EmployeeService>();
            services.AddSingleton<IDepartmentService, DepartmentService>();
            services.AddSingleton<IJobTitleService, JobTitleService>();
            services.AddSingleton<IUnitService, UnitService>();
            services.AddSingleton<IWarehouseService, WarehouseService>();
            services.AddSingleton<IStockService, StockService>();
            services.AddSingleton<PrimeERP.Application.Services.Inventory.IOpeningStockService, PrimeERP.Application.Services.Inventory.OpeningStockService>();
            services.AddSingleton<PrimeERP.Application.Services.Documents.IDocumentLinkService,
                                  PrimeERP.Application.Services.Documents.DocumentLinkService>();

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
            services.AddSingleton<PrimeERP.Application.Services.Assets.IAssetDisposalService, PrimeERP.Application.Services.Assets.AssetDisposalService>();
            services.AddSingleton<PrimeERP.Application.Services.Accounting.IOpeningBalanceService, PrimeERP.Application.Services.Accounting.OpeningBalanceService>();
            services.AddSingleton<PrimeERP.Application.Services.Cheques.IChequeService, PrimeERP.Application.Services.Cheques.ChequeService>();
            services.AddSingleton<PrimeERP.Application.Services.Cheques.IChequeReceiptDocumentService, PrimeERP.Application.Services.Cheques.ChequeReceiptDocumentService>();
            services.AddSingleton<PrimeERP.Application.Services.Cheques.IChequeIssueDocumentService, PrimeERP.Application.Services.Cheques.ChequeIssueDocumentService>();
            services.AddSingleton<IPayrollService, PayrollService>();
            services.AddSingleton<IAllowanceService, AllowanceService>();
            services.AddSingleton<IDeductionService, DeductionService>();
            services.AddSingleton<IAttendanceService, AttendanceService>();
            services.AddSingleton<IRoleService, RoleService>();
            services.AddSingleton<IUserService, UserService>();

            services.AddSingleton(sp => new Lazy<IJournalService>(() => sp.GetRequiredService<IJournalService>()));

            return services;
        }

        public static IServiceCollection AddUI(this IServiceCollection services)
        {
            services.AddSingleton<IIdentityService, IdentityService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IToastService, ToastService>();
            services.AddSingleton<INavigationService, NavigationService>();

            services.AddSingleton<ExportService>();
            services.AddSingleton<IExportService>(sp => sp.GetRequiredService<ExportService>());
            services.AddSingleton<IDocumentExporter>(sp => sp.GetRequiredService<ExportService>());
            services.AddSingleton<IPrintDialogHost, PrintDialogHost>();

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
            services.AddTransient<AssetRevaluationsViewModel>();
            services.AddTransient<AssetDepreciationsViewModel>();
            services.AddTransient<AssetDisposalsViewModel>();
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
            services.AddTransient<AllowanceViewModel>();
            services.AddTransient<DeductionViewModel>();
            services.AddTransient<AttendanceViewModel>();

            return services;
        }

        public static IServiceCollection AddComposition(this IServiceCollection services)
        {
            services.AddSingleton<IModuleRegistry, ModuleRegistry>();
            return services;
        }

        private static void ApplyLicenseFile(IServiceProvider services)
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "license.json");
            if (!System.IO.File.Exists(path)) return;

            try
            {
                var file = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(path)).RootElement;
                var settings = services.GetRequiredService<ISettingsService>();

                string Read(string name) => file.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";

                settings.Set(SettingKeys.License.Serial, Read("serial"));
                settings.Set(SettingKeys.License.Customer, Read("customer"));

                var pages = Read("manifest");
                if (!string.IsNullOrWhiteSpace(pages)) settings.Set(SettingKeys.UI.Manifest, pages);

                if (file.TryGetProperty("simplified", out var simplified))
                    settings.Set(SettingKeys.Documents.SimplifiedFlow, simplified.GetBoolean() ? "true" : "false");

                System.IO.File.Delete(path);
            }
            catch { }
        }

        public static IServiceProvider RegisterModules(this IServiceProvider services)
        {
            ApplyLicenseFile(services);

            var manifest = services.GetRequiredService<ISettingsService>().Get(SettingKeys.UI.Manifest, "");
            if (!string.IsNullOrWhiteSpace(manifest))
                services.GetRequiredService<IModuleRegistry>().Manifest =
                    manifest.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

            ModuleRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            HrRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            ReportRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            PermissionModuleRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            CycleVoucherRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            CycleDocumentRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            TreasuryRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());

            BuilderRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            BuilderModuleLoader.RegisterAll(services.GetRequiredService<IModuleRegistry>(), services);

            PermissionSeeder.Seed(services.GetRequiredService<IPermissionStore>());

            services.GetRequiredService<IPrintService>().DialogHost = services.GetRequiredService<IPrintDialogHost>();
            return services;
        }

        public static IServiceProvider EnsureDatabaseReady(this IServiceProvider services)
        {
            SchemaSync.Run();

            SettingSeeder.Seed(services.GetRequiredService<ISettingStore>());


            var accounts = services.GetRequiredService<IAccountRepository>();
            accounts.SeedDefaults();


            var numberSequences = services.GetRequiredService<INumberSequenceRepository>();
            NumberSequenceSeeder.Seed(numberSequences, services.GetRequiredService<ISettingStore>());

            var treasuryService = services.GetRequiredService<PrimeERP.Application.Services.Treasury.ITreasuryService>();
            treasuryService.RepairLinkedRoots();
            treasuryService.SeedDefaults();
            treasuryService.RepairMissingAccounts();



            services.GetRequiredService<PrimeERP.Application.Services.Assets.IAssetService>().SeedDefaults();

            return services;
        }
    }
}
