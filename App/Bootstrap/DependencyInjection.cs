using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.PageServices.Security;
using PrimeERP.Application.PageServices.Builder;
using PrimeERP.Application.PageServices.Backup;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Core;
using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Common;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.PageServices.Assets;
using PrimeERP.Application.PageServices.HR;
using PrimeERP.Application.PageServices.Sales;
using PrimeERP.Application.PageServices.Purchasing;
using PrimeERP.Application.PageServices.Parties;
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
            services.AddSingleton<ISettingRepository, SettingRepository>();
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
            services.AddSingleton<ILookupRepository<AllowanceType>>(_ => new LookupRepository<AllowanceType>("AllowanceTypes"));
            services.AddSingleton<ILookupRepository<DeductionType>>(_ => new LookupRepository<DeductionType>("DeductionTypes"));
            services.AddSingleton<ILookupRepository<LeaveType>>(_ => new LookupRepository<LeaveType>("LeaveTypes"));
            services.AddSingleton<ILookupRepository<JobTitle>>(_ => new LookupRepository<JobTitle>("JobTitles"));
            services.AddSingleton<ILookupRepository<Unit>>(_ => new LookupRepository<Unit>("Units"));
            services.AddSingleton<ILookupRepository<Warehouse>>(_ => new LookupRepository<Warehouse>("Warehouses"));
            services.AddSingleton<IBuilderRepository, BuilderRepository>();
            services.AddSingleton<PrimeERP.Application.PageServices.Builder.IBuilderCatalog, PrimeERP.Application.PageServices.Builder.BuilderCatalog>();
            services.AddSingleton<PrimeERP.Application.PageServices.Builder.BuilderSectionsService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Builder.BuilderModulesService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Builder.BuilderColumnsService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Builder.BuilderActionsService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Builder.BuilderFiltersService>();
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
            services.AddSingleton<IEmployeeAdvanceRepository, EmployeeAdvanceRepository>();
            services.AddSingleton<IEmployeeDeductionRepository, EmployeeDeductionRepository>();
            services.AddSingleton<IAttendanceRepository, AttendanceRepository>();
            return services;
        }

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<INumberSequenceService, NumberSequenceService>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Guards>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.PeriodGate>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.AccountBalances>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Entries>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Statement>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.TrialBalance>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.ClosingEntry>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.NewFiscalYear>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.DepreciationCharges>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.PartyByKind>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Accounts.AddTreeAccount>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Accounts.AddEntityAccount>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Accounts.RepairAccounts>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Accounts.AddLinkedAccount>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Accounts.RenameAccount>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Accounts.CloseAccount>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Accounts.EditTreeAccount>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Accounts.EditLinkedAccount>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Accounts.CloseLinkedAccount>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Accounts.AccountCases>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Accounts.LinkedAccounts>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Accounts.AccountOf>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Accounts.SettingAccounts>();
            services.AddSingleton<IAccountLinkedService>(sp => (IAccountLinkedService)sp.GetRequiredService<ICustomerService>());
            services.AddSingleton<IAccountLinkedService>(sp => (IAccountLinkedService)sp.GetRequiredService<ISupplierService>());
            services.AddSingleton<IAccountLinkedService>(sp => (IAccountLinkedService)sp.GetRequiredService<PrimeERP.Application.PageServices.Treasury.ITreasuryService>());
            services.AddSingleton<IAccountLinkedService>(sp => (IAccountLinkedService)sp.GetRequiredService<IEmployeeService>());
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
            services.AddSingleton<PrimeERP.Application.PageServices.Admin.IProgramEditionService, PrimeERP.Application.PageServices.Admin.ProgramEditionService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Admin.ILicenseService, PrimeERP.Application.PageServices.Admin.LicenseService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Admin.IUpdateService, PrimeERP.Application.PageServices.Admin.UpdateService>();
            services.AddSingleton<IPrintService, PrintService>();
            services.AddSingleton<ICategoryService, CategoryService>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddSingleton<IAssetService, AssetService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Assets.IAssetRevaluationService, PrimeERP.Application.PageServices.Assets.AssetRevaluationService>();
            services.AddSingleton<IEmployeeService, EmployeeService>();
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<Department>>(sp, LookupPages.Departments));
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<AllowanceType>>(sp, LookupPages.AllowanceTypes));
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<DeductionType>>(sp, LookupPages.DeductionTypes));
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<LeaveType>>(sp, LookupPages.LeaveTypes));
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<JobTitle>>(sp, LookupPages.JobTitles));
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<Unit>>(sp, LookupPages.Units));
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<Warehouse>>(sp, LookupPages.Warehouses));
            services.AddSingleton<IStockMove, StockMove>();
            services.AddSingleton<PrimeERP.Application.PageServices.Inventory.IOpeningStockService, PrimeERP.Application.PageServices.Inventory.OpeningStockService>();
            services.AddSingleton<PrimeERP.Application.Services.Documents.IDocumentPull,
                                  PrimeERP.Application.Services.Documents.DocumentPull>();

            services.AddSingleton<PrimeERP.Composition.Pull.PullService>();
            services.AddSingleton<PrimeERP.Composition.Pull.IPullService>(sp => sp.GetRequiredService<PrimeERP.Composition.Pull.PullService>());
            services.AddSingleton<PrimeERP.Domain.Contracts.IPullSourceReader>(sp => sp.GetRequiredService<PrimeERP.Composition.Pull.PullService>());
            services.AddSingleton<ISalesInvoiceService, SalesInvoiceService>();
            services.AddSingleton<IPurchaseInvoiceService, PurchaseInvoiceService>();
            services.AddSingleton<ISalesReturnService, SalesReturnService>();
            services.AddSingleton<IPurchaseReturnService, PurchaseReturnService>();
            services.AddSingleton<IStockInService, StockInService>();
            services.AddSingleton<IStockOutService, StockOutService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Documents.IPurchaseRequestService, PrimeERP.Application.PageServices.Documents.PurchaseRequestService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Documents.IPurchaseOrderService, PrimeERP.Application.PageServices.Documents.PurchaseOrderService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Documents.IQuotationService, PrimeERP.Application.PageServices.Documents.QuotationService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Documents.ISalesOrderService, PrimeERP.Application.PageServices.Documents.SalesOrderService>();
            services.AddSingleton<IGoodsReceiptService, GoodsReceiptService>();
            services.AddSingleton<IGoodsIssueService, GoodsIssueService>();
            services.AddSingleton<IDeliveryNoteService, DeliveryNoteService>();
            services.AddSingleton<ISalesReceiptService, SalesReceiptService>();
            services.AddSingleton<IStockTransferService, StockTransferService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Treasury.ITreasuryService, PrimeERP.Application.PageServices.Treasury.TreasuryService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Vouchers.IReceiptVoucherService, PrimeERP.Application.PageServices.Vouchers.ReceiptVoucherService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Vouchers.IPaymentVoucherService, PrimeERP.Application.PageServices.Vouchers.PaymentVoucherService>();
            services.AddSingleton<PrimeERP.UI.Services.IChequePrinter, PrimeERP.UI.Services.ChequePrinter>();
            services.AddSingleton<PrimeERP.Application.PageServices.Assets.IAssetDepreciationService, PrimeERP.Application.PageServices.Assets.AssetDepreciationService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Assets.IAssetDisposalService, PrimeERP.Application.PageServices.Assets.AssetDisposalService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Accounting.IOpeningBalanceService, PrimeERP.Application.PageServices.Accounting.OpeningBalanceService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Cheques.IChequeService, PrimeERP.Application.PageServices.Cheques.ChequeService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Cheques.IChequeReceiptDocumentService, PrimeERP.Application.PageServices.Cheques.ChequeReceiptDocumentService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Cheques.IChequeIssueDocumentService, PrimeERP.Application.PageServices.Cheques.ChequeIssueDocumentService>();
            services.AddSingleton<IPayrollService, PayrollService>();
            services.AddSingleton<IAllowanceService, AllowanceService>();
            services.AddSingleton<IAdvanceService, AdvanceService>();
            services.AddSingleton<IDeductionService, DeductionService>();
            services.AddSingleton<IAttendanceService, AttendanceService>();
            services.AddSingleton<IRoleService, RoleService>();
            services.AddSingleton<IUserService, UserService>();


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
            services.AddTransient<AssetCategoriesViewModel>();
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
            services.AddTransient<AttendanceViewModel>();
            services.AddTransient<AllowanceViewModel>();
            services.AddTransient<AdvanceViewModel>();
            services.AddTransient<DeductionViewModel>();

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
            if (!string.IsNullOrWhiteSpace(services.GetRequiredService<ISettingsProvider>().Get(SettingKeys.Developer.AdminToken, ""))) return;

            try
            {
                var file = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(path)).RootElement;
                var settings = services.GetRequiredService<ISettingsProvider>();

                string Read(string name) => file.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";

                settings.SetRaw(SettingKeys.License.Serial, Read("serial"));
                settings.SetRaw(SettingKeys.License.Customer, Read("customer"));

                var pages = Read("manifest");
                if (!string.IsNullOrWhiteSpace(pages)) settings.SetRaw(SettingKeys.UI.Manifest, pages);

                if (file.TryGetProperty("simplified", out var simplified))
                    settings.SetRaw(SettingKeys.Documents.SimplifiedFlow, simplified.GetBoolean() ? "true" : "false");

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


            AccountSeeder.Seed(services.GetRequiredService<IAccountRepository>());


            var numberSequences = services.GetRequiredService<INumberSequenceRepository>();
            NumberSequenceSeeder.Seed(numberSequences, services.GetRequiredService<ISettingStore>());

            services.GetRequiredService<SettingAccounts>().RepairRoots(PrimeERP.Platform.Settings.SettingKeys.Accounts.LinkedRoots);
            var treasuryService = services.GetRequiredService<PrimeERP.Application.PageServices.Treasury.ITreasuryService>();
            treasuryService.SeedDefaults();
            treasuryService.RepairMissingAccounts();
            foreach (var linked in services.GetServices<PrimeERP.Application.Services.Ledger.Accounts.IAccountLinkedService>())
                linked.RepairMissingEntities();
            services.GetRequiredService<PrimeERP.Application.PageServices.Cheques.IChequeService>().RepairHoldingEntries();



            services.GetRequiredService<PrimeERP.Application.PageServices.Assets.IAssetService>().SeedDefaults();

            return services;
        }
    }
}
