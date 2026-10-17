using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Data
{
    /// <summary>تسجيل المستودعات — يستدعيه كل مضيف: سطح المكتب والخادم</summary>
    public static class DataServices
    {
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
            services.AddSingleton<ILookupRepository<Branch>>(_ => new LookupRepository<Branch>("Branches"));
            services.AddSingleton<ILookupRepository<WorkCenter>>(_ => new LookupRepository<WorkCenter>("WorkCenters"));
            services.AddSingleton<IBillOfMaterialsRepository, BillOfMaterialsRepository>();
            services.AddSingleton<IProductionOrderRepository, ProductionOrderRepository>();
            services.AddSingleton<IScrapRepository, ScrapRepository>();
            services.AddSingleton<IBuilderRepository, BuilderRepository>();
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
            services.AddSingleton<IStockTransferRepository, StockTransferRepository>();
            services.AddSingleton<IPayrollRepository, PayrollRepository>();
            services.AddSingleton<IEmployeeAllowanceRepository, EmployeeAllowanceRepository>();
            services.AddSingleton<IEmployeeAdvanceRepository, EmployeeAdvanceRepository>();
            services.AddSingleton<IEmployeeDeductionRepository, EmployeeDeductionRepository>();
            services.AddSingleton<IAttendanceRepository, AttendanceRepository>();
            services.AddSingleton<IPosShiftRepository, PosShiftRepository>();
            services.AddSingleton<IHeldSaleRepository, HeldSaleRepository>();
            services.AddSingleton<INotificationRepository, NotificationRepository>();
            services.AddSingleton<IOutboxRepository, OutboxRepository>();
            services.AddSingleton<ILookupRepository<AlertRule>>(_ => new LookupRepository<AlertRule>("AlertRules"));
            services.AddSingleton<ILookupRepository<ApiKey>>(_ => new LookupRepository<ApiKey>("ApiKeys"));
            services.AddSingleton<IApiKeyRepository, ApiKeyRepository>();
            services.AddSingleton<IPaymentRepository, PaymentRepository>();
            services.AddSingleton<IEInvoiceRepository, EInvoiceRepository>();
            return services;
        }
    }
}
