using System;
using System.Linq;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.PageServices.Security;
using PrimeERP.Application.PageServices.Builder;
using PrimeERP.Application.PageServices.Backup;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Core;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Common;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.PageServices.Assets;
using PrimeERP.Application.PageServices.HR;
using PrimeERP.Application.PageServices.Sales;
using PrimeERP.Application.PageServices.Purchasing;
using PrimeERP.Application.PageServices.Parties;
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

namespace PrimeERP.Application
{
    /// <summary>تسجيل خدمات الأعمال وتهيئة القاعدة — يستدعيهما كل مضيف: سطح المكتب والخادم</summary>
    public static class ApplicationServices
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<PrimeERP.Application.PageServices.Builder.IBuilderCatalog, PrimeERP.Application.PageServices.Builder.BuilderCatalog>();
            services.AddSingleton<PrimeERP.Application.PageServices.Builder.BuilderSectionsService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Builder.BuilderModulesService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Builder.BuilderColumnsService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Builder.BuilderActionsService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Builder.BuilderFiltersService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IDatabaseSettingsService, DatabaseSettingsService>();
            services.AddSingleton<INumberSequenceService, NumberSequenceService>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Guards>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.PeriodGate>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.AccountBalances>();
            services.AddSingleton<PrimeERP.Application.Services.Ledger.Settlement>();
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
            services.AddSingleton<PrimeERP.Application.Reporting.IHrReportService, PrimeERP.Application.Reporting.HrReportService>();
            services.AddSingleton<PrimeERP.Application.Reporting.IFlowReportService, PrimeERP.Application.Reporting.FlowReportService>();
            services.AddSingleton<PrimeERP.Application.Analytics.IAnalyticsService, PrimeERP.Application.Analytics.AnalyticsService>();
            services.AddSingleton<PrimeERP.Application.Reporting.IAgingReportService, PrimeERP.Application.Reporting.AgingReportService>();
            services.AddSingleton<PrimeERP.Application.Reporting.IPosReportService, PrimeERP.Application.Reporting.PosReportService>();
            services.AddSingleton<PrimeERP.Application.Reporting.IManufacturingReportService, PrimeERP.Application.Reporting.ManufacturingReportService>();
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
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<Branch>>(sp, LookupPages.Branches));
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<WorkCenter>>(sp, LookupPages.WorkCenters));

            // المشاريع: صفحات قوائم، والساعات والمستخلصات تحمل قيودها (IRowPosting)
            services.AddSingleton<IRowPosting<ProjectTimesheet>, PrimeERP.Application.Services.Projects.TimesheetPosting>();
            services.AddSingleton<IRowPosting<ProjectBill>, PrimeERP.Application.Services.Projects.BillPosting>();
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<Project>>(sp, LookupPages.Projects));
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<ProjectTask>>(sp, LookupPages.ProjectTasks));
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<ProjectTimesheet>>(sp, LookupPages.ProjectTimesheets));
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<ProjectBill>>(sp, LookupPages.ProjectBills));
            services.AddSingleton<PrimeERP.Application.Reporting.IProjectReportService, PrimeERP.Application.Reporting.ProjectReportService>();

            // التنبيهات والتكامل: الحدث من سجلّ التدقيق، والقواعد صفحة، والإرسال من صندوق الصادر
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<AlertRule>>(sp, LookupPages.AlertRules));
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<Lookup<ApiKey>>(sp, LookupPages.ApiKeys));
            services.AddSingleton<PrimeERP.Application.Services.Integration.IApiKeyService, PrimeERP.Application.Services.Integration.ApiKeyService>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.InProcessPush>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.INotificationPush>(sp => sp.GetRequiredService<PrimeERP.Application.Services.Integration.InProcessPush>());
            services.AddSingleton<PrimeERP.Application.Services.Integration.Alerts>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.IAlertScan, PrimeERP.Application.Services.Integration.StockLowScan>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.IAlertScan, PrimeERP.Application.Services.Integration.NearExpiryScan>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.IAlertChannel, PrimeERP.Application.Services.Integration.EmailChannel>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.IAlertChannel, PrimeERP.Application.Services.Integration.WhatsAppChannel>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.IAlertChannel, PrimeERP.Application.Services.Integration.WebhookChannel>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.AlertRunner>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.IPaymentGateway, PrimeERP.Application.Services.Integration.StripeGateway>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.IPaymentGateway, PrimeERP.Application.Services.Integration.PaymobGateway>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.IPaymentGateway, PrimeERP.Application.Services.Integration.FawryGateway>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.IPaymentLinkService, PrimeERP.Application.Services.Integration.PaymentLinkService>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.IEInvoiceProvider, PrimeERP.Application.Services.Integration.EtaProvider>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.IEInvoiceProvider, PrimeERP.Application.Services.Integration.ZatcaProvider>();
            services.AddSingleton<PrimeERP.Application.Services.Integration.IEInvoiceService, PrimeERP.Application.Services.Integration.EInvoiceService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Admin.INotificationService, PrimeERP.Application.PageServices.Admin.NotificationService>();
            services.AddSingleton<PrimeERP.Platform.Audit.AuditLogger>();
            services.AddSingleton<PrimeERP.Platform.Audit.IAuditLogger>(sp => new PrimeERP.Application.Services.Integration.AlertingAuditLogger(
                sp.GetRequiredService<PrimeERP.Platform.Audit.AuditLogger>(),
                new Lazy<PrimeERP.Application.Services.Integration.Alerts>(sp.GetRequiredService<PrimeERP.Application.Services.Integration.Alerts>)));
            services.AddSingleton<IStockMove, StockMove>();
            services.AddSingleton<PrimeERP.Application.PageServices.Inventory.IOpeningStockService, PrimeERP.Application.PageServices.Inventory.OpeningStockService>();
            services.AddSingleton<PrimeERP.Application.Services.Documents.IDocumentPull,
                                  PrimeERP.Application.Services.Documents.DocumentPull>();

            services.AddSingleton<ISalesInvoiceService, SalesInvoiceService>();
            services.AddSingleton<IPosService, PosService>();
            services.AddSingleton<PosShiftReport>();
            services.AddSingleton<IPurchaseInvoiceService, PurchaseInvoiceService>();
            services.AddSingleton<ISalesReturnService, SalesReturnService>();
            services.AddSingleton<IPurchaseReturnService, PurchaseReturnService>();
            services.AddSingleton<IStockInService, StockInService>();
            services.AddSingleton<IStockOutService, StockOutService>();
            services.AddSingleton<IScrapService, ScrapService>();
            services.AddSingleton<PrimeERP.Application.Services.Documents.Production>();
            services.AddSingleton<PrimeERP.Application.PageServices.Manufacturing.IBillOfMaterialsService, PrimeERP.Application.PageServices.Manufacturing.BillOfMaterialsService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Manufacturing.IProductionOrderService, PrimeERP.Application.PageServices.Manufacturing.ProductionOrderService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Manufacturing.IUnbuildService, PrimeERP.Application.PageServices.Manufacturing.UnbuildService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Documents.IPurchaseRequestService, PrimeERP.Application.PageServices.Documents.PurchaseRequestService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Documents.IPurchaseOrderService, PrimeERP.Application.PageServices.Documents.PurchaseOrderService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Documents.IQuotationService, PrimeERP.Application.PageServices.Documents.QuotationService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Documents.ISalesOrderService, PrimeERP.Application.PageServices.Documents.SalesOrderService>();
            services.AddSingleton<IStockTransferService, StockTransferService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Treasury.ITreasuryService, PrimeERP.Application.PageServices.Treasury.TreasuryService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Vouchers.IReceiptVoucherService, PrimeERP.Application.PageServices.Vouchers.ReceiptVoucherService>();
            services.AddSingleton<PrimeERP.Application.PageServices.Vouchers.IPaymentVoucherService, PrimeERP.Application.PageServices.Vouchers.PaymentVoucherService>();
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
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<SourceDocuments>();
            services.AddSingleton<IPullCandidates, PullCandidates>();
            services.AddSingleton<PrimeERP.Application.PageServices.Sales.PosReturnScope>();
            services.AddSingleton<IPullScope>(sp => sp.GetRequiredService<PrimeERP.Application.PageServices.Sales.PosReturnScope>());
            services.AddSingleton<IPullSourceReader>(sp => sp.GetRequiredService<SourceDocuments>());
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
                if (!string.IsNullOrWhiteSpace(Read("version"))) settings.SetRaw(SettingKeys.License.Version, Read("version"));

                var pages = Read("manifest");
                if (!string.IsNullOrWhiteSpace(pages)) settings.SetRaw(SettingKeys.UI.Manifest, pages);

                if (file.TryGetProperty("simplified", out var simplified))
                    settings.SetRaw(SettingKeys.Documents.SimplifiedFlow, simplified.GetBoolean() ? "true" : "false");

                if (file.TryGetProperty("english", out var english))
                    settings.SetRaw(SettingKeys.UI.Language, english.GetBoolean() ? "En" : "Ar");

                System.IO.File.Delete(path);
            }
            catch { }
        }

        public static AppLanguage LanguageOf(ISettingsProvider settings)
        {
            var english = settings.Get(SettingKeys.Edition.English, true);
            var arabic = settings.Get(SettingKeys.Edition.Arabic, true) || !english;
            return english && (!arabic || settings.Get(SettingKeys.UI.Language, "Ar") == "En") ? AppLanguage.En : AppLanguage.Ar;
        }

        public static IServiceProvider EnsureDatabaseReady(this IServiceProvider services, Action<AppLanguage> useLanguage)
        {
            SchemaSync.Run();

            SettingSeeder.Seed(services.GetRequiredService<ISettingStore>());
            services.GetRequiredService<PrimeERP.Application.PageServices.Builder.IBuilderCatalog>().RegisterPagePermissions();
            PermissionSeeder.Seed(services.GetRequiredService<IPermissionStore>());
            ApplyLicenseFile(services);
            useLanguage(LanguageOf(services.GetRequiredService<ISettingsProvider>()));

            AccountSeeder.Seed(services.GetRequiredService<IAccountRepository>(), LocalizationService.CurrentLanguage == AppLanguage.En);

            var numberSequences = services.GetRequiredService<INumberSequenceRepository>();
            NumberSequenceSeeder.Seed(numberSequences, services.GetRequiredService<ISettingStore>());

            services.GetRequiredService<SettingAccounts>().RepairRoots(PrimeERP.Platform.Settings.SettingKeys.Accounts.LinkedRoots);
            var settingAccounts = services.GetRequiredService<SettingAccounts>();
            settingAccounts.Ensure(SettingKeys.Accounts.ProductionOverhead, "51", LocalizationService.Get("Str.Account.ProductionOverhead"));
            settingAccounts.Ensure(SettingKeys.Accounts.ScrapExpense, "56", LocalizationService.Get("Str.Account.ScrapExpense"));
            settingAccounts.Ensure(SettingKeys.Accounts.CashOverShort, "56", LocalizationService.Get("Str.Account.CashOverShort"));
            settingAccounts.Ensure(SettingKeys.Accounts.ProjectRevenue, "41", LocalizationService.Get("Str.Account.ProjectRevenue"));
            settingAccounts.Ensure(SettingKeys.Accounts.ProjectCost, "5", LocalizationService.Get("Str.Account.ProjectCost"));
            settingAccounts.Ensure(SettingKeys.Accounts.RetentionReceivable, "1206", LocalizationService.Get("Str.Account.RetentionReceivable"));
            services.GetRequiredService<PrimeERP.Application.Services.Ledger.Settlement>().RecalculateUnsettled(null);
            var treasuryService = services.GetRequiredService<PrimeERP.Application.PageServices.Treasury.ITreasuryService>();
            treasuryService.SeedDefaults();
            services.GetRequiredService<PrimeERP.Application.PageServices.Parties.ICustomerService>().SeedDefaults();
            services.GetRequiredService<Lookup<Warehouse>>().SeedDefault();
            treasuryService.RepairMissingAccounts();
            foreach (var linked in services.GetServices<PrimeERP.Application.Services.Ledger.Accounts.IAccountLinkedService>())
                linked.RepairMissingEntities();
            services.GetRequiredService<PrimeERP.Application.PageServices.Cheques.IChequeService>().RepairHoldingEntries();
            services.GetRequiredService<PrimeERP.Application.PageServices.Assets.IAssetService>().SeedDefaults();

            return services;
        }
    }
}
