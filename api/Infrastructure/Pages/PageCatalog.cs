using System.Collections.Frozen;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Assets;
using PrimeERP.Application.PageServices.Builder;
using PrimeERP.Application.PageServices.Cheques;
using PrimeERP.Application.PageServices.Documents;
using PrimeERP.Application.PageServices.HR;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.PageServices.Manufacturing;
using PrimeERP.Application.PageServices.Parties;
using PrimeERP.Application.PageServices.Purchasing;
using PrimeERP.Application.PageServices.Sales;
using PrimeERP.Application.PageServices.Security;
using PrimeERP.Application.PageServices.Treasury;
using PrimeERP.Application.PageServices.Vouchers;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Api.Infrastructure.Pages
{
    /// <summary>دليل الصفحات: كل صفحةٍ بخدمتها وعقدها — حقول القراءة والكتابة من حوارها في النظام، والمحجوب بصلاحية عموده</summary>
    public static class PageCatalog
    {
        private static readonly string[] Audit = { "IsActive", "CreatedAt", "UpdatedAt" };

        private static string[] Fields(params string[] names) => names;

        private static PageResource Entity<TService>(string key, string[] read, string[] write, params (string Field, string Permission)[] guarded) => new()
        {
            Key = key, Service = typeof(TService), Read = read.Concat(write).Concat(Audit).Distinct().ToArray(), Write = write,
            Guarded = guarded.ToDictionary(g => g.Field, g => g.Permission, StringComparer.OrdinalIgnoreCase),
        };

        private static PageResource Dto<TService>(string key, params (string Field, string Permission)[] guarded) => new()
        {
            Key = key, Service = typeof(TService),
            Guarded = guarded.ToDictionary(g => g.Field, g => g.Permission, StringComparer.OrdinalIgnoreCase),
        };

        private static readonly FrozenDictionary<string, PageResource> Pages = new[]
        {
            Entity<ICustomerService>("Customers",
                Fields("Code", "CategoryName", "Balance"),
                Fields("Name", "Phone", "Email", "CreditLimit", "CategoryId", "Notes", "IsActive"),
                ("CreditLimit", PermissionKeys.Customers.ColumnCreditLimit)),
            Entity<ISupplierService>("Suppliers",
                Fields("Code", "CategoryName", "Balance"),
                Fields("Name", "Phone", "Email", "CreditLimit", "CategoryId", "Notes", "IsActive")),
            Entity<IProductService>("Products",
                Fields("Code", "CategoryName", "BrandName"),
                Fields("CategoryId", "Name", "Barcode", "HasExpiry", "CostPrice", "SalePrice", "BrandId", "Description", "Notes", "IsActive", "ImageData"),
                ("CostPrice", PermissionKeys.Products.ColumnCostPrice)),
            Entity<IAssetService>("Assets",
                Fields("Code", "CategoryName", "CurrentValue", "AccumulatedDepreciation"),
                Fields("CategoryId", "Name", "PurchaseDate", "PurchaseCost", "AcquisitionMethod", "FundingId", "UsefulLifeYears", "SalvageValue", "Location", "Notes")),
            Entity<IAssetDepreciationService>("AssetDepreciations",
                Fields("AssetCode", "AssetName"),
                Fields("AssetId", "PeriodDate", "Amount", "Notes")),
            Entity<IAssetRevaluationService>("AssetRevaluations",
                Fields("AssetCode", "AssetName", "OldValue", "Difference", "KindName"),
                Fields("AssetId", "RevaluationDate", "NewValue", "SalvageValue", "UsefulLifeYears", "Notes")),
            Entity<IAssetDisposalService>("AssetDisposals",
                Fields("AssetCode", "AssetName", "TreasuryName", "BookValue", "GainOrLoss", "KindName"),
                Fields("AssetId", "DisposalDate", "Settlement", "TreasuryId", "SalePrice", "Notes")),
            Entity<IEmployeeService>("Employees",
                Fields("Code", "DepartmentName", "JobTitleName"),
                Fields("DepartmentId", "Name", "JobTitleId", "Phone", "Email", "HireDate", "BasicSalary", "WorkStart", "WorkEnd", "Status", "Notes"),
                ("BasicSalary", PermissionKeys.HR.ColumnSalary)),
            Entity<ITreasuryService>("Treasuries",
                Fields("Code", "AccountCode", "AccountBalance", "KindName"),
                Fields("Kind", "Name", "AccountNumber", "IsActive", "Notes")),
            Entity<IRoleService>("Roles", Fields(), Fields("NameAr", "Name")) with { OptionLabel = "NameAr", OptionHint = "Name" },

            Dto<IUserService>("Users") with { OptionLabel = "DisplayName", OptionHint = "Username" },
            Dto<IAccountService>("Accounts", ("Balance", PermissionKeys.Accounts.ColumnBalance)),
            Dto<IJournalService>("Journals"),
            Dto<IOpeningBalanceService>("OpeningBalances"),
            Dto<IOpeningStockService>("OpeningStock"),
            Dto<ISalesInvoiceService>("SalesInvoices"),
            Dto<IPurchaseInvoiceService>("PurchaseInvoices"),
            Dto<ISalesReturnService>("SalesReturns"),
            Dto<IPurchaseReturnService>("PurchaseReturns"),
            Dto<IPurchaseRequestService>("PurchaseRequest"),
            Dto<IPurchaseOrderService>("PurchaseOrder"),
            Dto<IQuotationService>("Quotation"),
            Dto<ISalesOrderService>("SalesOrder"),
            Dto<IStockInService>("StockIn"),
            Dto<IStockOutService>("StockOut"),
            Dto<IStockTransferService>("StockTransfer"),
            Dto<IScrapService>("Scrap"),
            Dto<IBillOfMaterialsService>("BillsOfMaterials"),
            Dto<IProductionOrderService>("ProductionOrders"),
            Dto<IUnbuildService>("Unbuild"),
            Dto<IReceiptVoucherService>("Receipts"),
            Dto<IPaymentVoucherService>("Payments"),
            Dto<IChequeReceiptDocumentService>("ChequeReceipts"),
            Dto<IChequeIssueDocumentService>("ChequeIssues"),
            Dto<IChequeService>("Cheques"),
            Dto<IAllowanceService>("EmployeeAllowances"),
            Dto<IDeductionService>("EmployeeDeductions"),
            Dto<IAdvanceService>("EmployeeAdvances"),
            Dto<IAttendanceService>("Attendances"),
            Dto<IPayrollService>("Payroll"),

            Dto<Lookup<Unit>>("Units"),
            Dto<Lookup<Branch>>("Branches"),
            Dto<Lookup<AlertRule>>("AlertRules"),
            Dto<Lookup<ApiKey>>("ApiKeys"),
            Dto<Lookup<Warehouse>>("Warehouses"),
            Dto<Lookup<WorkCenter>>("WorkCenters"),
            Dto<Lookup<Project>>("Projects"),
            Dto<Lookup<ProjectTask>>("ProjectTasks"),
            Dto<Lookup<ProjectTimesheet>>("ProjectTimesheets"),
            Dto<Lookup<ProjectBill>>("ProjectBills"),
            Dto<Lookup<Department>>("Departments"),
            Dto<Lookup<AllowanceType>>("AllowanceTypes"),
            Dto<Lookup<DeductionType>>("DeductionTypes"),
            Dto<Lookup<LeaveType>>("LeaveTypes"),
            Dto<Lookup<JobTitle>>("JobTitles"),
            Dto<PosShiftReport>("PosShifts"),

            Dto<BuilderSectionsService>("BuilderSections") with { OptionLabel = "Title", OptionHint = "Key" },
            Dto<BuilderModulesService>("BuilderModules") with { OptionLabel = "Title", OptionHint = "Key" },
            Dto<BuilderColumnsService>("BuilderColumns"),
            Dto<BuilderActionsService>("BuilderActions"),
            Dto<BuilderFiltersService>("BuilderFilters"),
        }.ToFrozenDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);

        public static bool TryGet(string key, out PageResource page) => Pages.TryGetValue(key ?? "", out page);
    }
}
