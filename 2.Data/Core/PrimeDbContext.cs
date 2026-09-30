using System;
using Microsoft.EntityFrameworkCore;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Data.Core
{
    /// <summary>نموذج EF مولَّد</summary>
    public partial class PrimeDbContext : DbContext
    {
        public PrimeDbContext(DbContextOptions<PrimeDbContext> options) : base(options) { }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<AppSetting> AppSettings => Set<AppSetting>();
        public DbSet<Asset> Assets => Set<Asset>();
        public DbSet<AssetDepreciation> AssetDepreciations => Set<AssetDepreciation>();
        public DbSet<AssetDisposal> AssetDisposals => Set<AssetDisposal>();
        public DbSet<AssetRevaluation> AssetRevaluations => Set<AssetRevaluation>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<AuditEntry> AuditLog => Set<AuditEntry>();
        public DbSet<BackupHistoryRecord> BackupHistory => Set<BackupHistoryRecord>();
        public DbSet<BuilderAction> BuilderActions => Set<BuilderAction>();
        public DbSet<BuilderColumn> BuilderColumns => Set<BuilderColumn>();
        public DbSet<BuilderFilter> BuilderFilters => Set<BuilderFilter>();
        public DbSet<BuilderModule> BuilderModules => Set<BuilderModule>();
        public DbSet<BuilderSection> BuilderSections => Set<BuilderSection>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Cheque> Cheques => Set<Cheque>();
        public DbSet<ChequeMovement> ChequeMovements => Set<ChequeMovement>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<DocumentLink> DocumentLinks => Set<DocumentLink>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<EmployeeAllowance> EmployeeAllowances => Set<EmployeeAllowance>();
        public DbSet<EmployeeDeduction> EmployeeDeductions => Set<EmployeeDeduction>();
        public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
        public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
        public DbSet<JobTitle> JobTitles => Set<JobTitle>();
        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
        public DbSet<JournalLine> JournalEntryLines => Set<JournalLine>();
        public DbSet<License> Licenses => Set<License>();
        public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();
        public DbSet<Payroll> Payrolls => Set<Payroll>();
        public DbSet<PayrollLine> PayrollLines => Set<PayrollLine>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
        public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines => Set<PurchaseInvoiceLine>();
        public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
        public DbSet<PurchaseReturnLine> PurchaseReturnLines => Set<PurchaseReturnLine>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
        public DbSet<SalesInvoiceLine> SalesInvoiceLines => Set<SalesInvoiceLine>();
        public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
        public DbSet<SalesReturnLine> SalesReturnLines => Set<SalesReturnLine>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();
        public DbSet<StockTransferDocument> StockTransferDocuments => Set<StockTransferDocument>();
        public DbSet<StockTransferLine> StockTransferLines => Set<StockTransferLine>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Treasury> Treasuries => Set<Treasury>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<VoucherAllocation> VoucherAllocations => Set<VoucherAllocation>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<Account>(e =>
            {
                e.ToTable("Accounts");
                e.HasKey(x => x.Id);
                e.Property(x => x.Balance).HasPrecision(18, 4);
                e.HasIndex("ParentCode").HasDatabaseName("IX_Accounts_ParentCode");
                e.HasIndex("Code").HasDatabaseName("IX_Accounts_Code").IsUnique();
            });

            model.Entity<AppSetting>(e =>
            {
                e.ToTable("AppSettings");
                e.HasKey(x => x.Id);
                e.HasIndex("Key").HasDatabaseName("IX_AppSettings_Key").IsUnique();
            });

            model.Entity<AssetDepreciation>(e =>
            {
                e.ToTable("AssetDepreciations");
                e.HasKey(x => x.Id);
                e.Property(x => x.Amount).HasPrecision(18, 4);
                e.HasIndex("AssetId").HasDatabaseName("IX_AssetDepreciations_AssetId");
            });

            model.Entity<AssetDisposal>(e =>
            {
                e.ToTable("AssetDisposals");
                e.HasKey(x => x.Id);
                e.Property(x => x.AccumulatedDepreciation).HasPrecision(18, 4);
                e.Property(x => x.AssetValue).HasPrecision(18, 4);
                e.Property(x => x.SalePrice).HasPrecision(18, 4);
                e.HasIndex("AssetId").HasDatabaseName("IX_AssetDisposals_AssetId");
            });

            model.Entity<AssetRevaluation>(e =>
            {
                e.ToTable("AssetRevaluations");
                e.HasKey(x => x.Id);
                e.Property(x => x.NewValue).HasPrecision(18, 4);
                e.Property(x => x.OldValue).HasPrecision(18, 4);
                e.HasIndex("AssetId").HasDatabaseName("IX_AssetRevaluations_AssetId");
            });

            model.Entity<Asset>(e =>
            {
                e.ToTable("Assets");
                e.HasKey(x => x.Id);
                e.Property(x => x.AccumulatedDepreciation).HasPrecision(18, 4);
                e.Ignore(x => x.CategoryName);
                e.Property(x => x.CurrentValue).HasPrecision(18, 4);
                e.Property(x => x.DepreciationAccountCode).HasColumnName("DepAccountCode");
                e.Property(x => x.PurchaseCost).HasPrecision(18, 4);
                e.Property(x => x.RevaluedValue).HasPrecision(18, 4);
                e.Property(x => x.SalvageValue).HasPrecision(18, 4);
                e.HasIndex("CategoryId").HasDatabaseName("IX_Assets_CategoryId");
                e.HasIndex("Code").HasDatabaseName("IX_Assets_Code").IsUnique();
            });

            model.Entity<Attendance>(e =>
            {
                e.ToTable("Attendances");
                e.HasKey(x => x.Id);
                e.Property(x => x.CheckIn).HasColumnName("CheckInMinutes");
                e.Property(x => x.CheckIn).HasConversion(v => v == null ? (int?)null : (int)v.Value.TotalMinutes, v => v == null ? (TimeSpan?)null : TimeSpan.FromMinutes(v.Value));
                e.Property(x => x.CheckOut).HasColumnName("CheckOutMinutes");
                e.Property(x => x.CheckOut).HasConversion(v => v == null ? (int?)null : (int)v.Value.TotalMinutes, v => v == null ? (TimeSpan?)null : TimeSpan.FromMinutes(v.Value));
                e.Ignore(x => x.EmployeeCode);
                e.Ignore(x => x.EmployeeName);
                e.Property(x => x.OvertimeHours).HasPrecision(18, 4);
                e.HasIndex("EmployeeId").HasDatabaseName("IX_Attendances_EmployeeId");
            });

            model.Entity<AuditEntry>(e =>
            {
                e.ToTable("AuditLog");
                e.HasKey(x => x.Id);
            });

            model.Entity<BackupHistoryRecord>(e =>
            {
                e.ToTable("BackupHistory");
                e.HasKey(x => x.Id);
            });

            model.Entity<BuilderAction>(e =>
            {
                e.ToTable("BuilderActions");
                e.HasKey(x => x.Id);
                e.HasIndex("ModuleId").HasDatabaseName("IX_BuilderActions_ModuleId");
            });

            model.Entity<BuilderColumn>(e =>
            {
                e.ToTable("BuilderColumns");
                e.HasKey(x => x.Id);
                e.HasIndex("ModuleId").HasDatabaseName("IX_BuilderColumns_ModuleId");
            });

            model.Entity<BuilderFilter>(e =>
            {
                e.ToTable("BuilderFilters");
                e.HasKey(x => x.Id);
                e.HasIndex("ModuleId").HasDatabaseName("IX_BuilderFilters_ModuleId");
            });

            model.Entity<BuilderModule>(e =>
            {
                e.ToTable("BuilderModules");
                e.HasKey(x => x.Id);
                e.HasIndex("SectionId").HasDatabaseName("IX_BuilderModules_SectionId");
                e.HasIndex("Key").HasDatabaseName("IX_BuilderModules_Key").IsUnique();
            });

            model.Entity<BuilderSection>(e =>
            {
                e.ToTable("BuilderSections");
                e.HasKey(x => x.Id);
                e.HasIndex("Key").HasDatabaseName("IX_BuilderSections_Key").IsUnique();
            });

            model.Entity<Category>(e =>
            {
                e.ToTable("Categories");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.ParentName);
                e.Property(x => x.DepreciationAccountCode).HasColumnName("DepAccountCode");
                e.HasIndex("ParentId").HasDatabaseName("IX_Categories_ParentId");
                e.HasIndex("ModuleKey").HasDatabaseName("IX_Categories_ModuleKey");
            });

            model.Entity<ChequeMovement>(e =>
            {
                e.ToTable("ChequeMovements");
                e.HasKey(x => x.Id);
                e.HasIndex("ChequeId").HasDatabaseName("IX_ChequeMovements_ChequeId");
            });

            model.Entity<Cheque>(e =>
            {
                e.ToTable("Cheques");
                e.HasKey(x => x.Id);
                e.Property(x => x.Amount).HasPrecision(18, 4);
                e.HasIndex("Status").HasDatabaseName("IX_Cheques_Status");
                e.HasIndex("DueDate").HasDatabaseName("IX_Cheques_DueDate");
            });

            model.Entity<Customer>(e =>
            {
                e.ToTable("Customers");
                e.HasKey(x => x.Id);
                e.Property(x => x.Balance).HasPrecision(18, 4);
                e.Ignore(x => x.CategoryName);
                e.Property(x => x.CreditLimit).HasPrecision(18, 4);
                e.HasIndex("AccountCode").HasDatabaseName("IX_Customers_AccountCode");
                e.HasIndex("Code").HasDatabaseName("IX_Customers_Code").IsUnique();
            });

            model.Entity<Department>(e =>
            {
                e.ToTable("Departments");
                e.HasKey(x => x.Id);
            });

            model.Entity<DocumentLink>(e =>
            {
                e.ToTable("DocumentLinks");
                e.HasKey(x => x.Id);
                e.Property(x => x.PulledQty).HasPrecision(18, 4);
                e.HasIndex("TargetId").HasDatabaseName("IX_DocumentLinks_TargetId");
                e.HasIndex("SourceLineId").HasDatabaseName("IX_DocumentLinks_SourceLineId");
            });

            model.Entity<EmployeeAllowance>(e =>
            {
                e.ToTable("EmployeeAllowances");
                e.HasKey(x => x.Id);
                e.Property(x => x.Amount).HasPrecision(18, 4);
                e.Ignore(x => x.EmployeeCode);
                e.Ignore(x => x.EmployeeName);
                e.HasIndex("EmployeeId").HasDatabaseName("IX_EmployeeAllowances_EmployeeId");
            });

            model.Entity<EmployeeDeduction>(e =>
            {
                e.ToTable("EmployeeDeductions");
                e.HasKey(x => x.Id);
                e.Property(x => x.Amount).HasPrecision(18, 4);
                e.Ignore(x => x.EmployeeCode);
                e.Ignore(x => x.EmployeeName);
                e.HasIndex("EmployeeId").HasDatabaseName("IX_EmployeeDeductions_EmployeeId");
            });

            model.Entity<Employee>(e =>
            {
                e.ToTable("Employees");
                e.HasKey(x => x.Id);
                e.Property(x => x.BasicSalary).HasPrecision(18, 4);
                e.Ignore(x => x.DepartmentName);
                e.Property(x => x.FixedAllowances).HasPrecision(18, 4);
                e.Property(x => x.InsuranceAmount).HasPrecision(18, 4);
                e.Ignore(x => x.JobTitleName);
                e.Property(x => x.TaxAmount).HasPrecision(18, 4);
                e.HasIndex("DepartmentId").HasDatabaseName("IX_Employees_DepartmentId");
                e.HasIndex("Code").HasDatabaseName("IX_Employees_Code").IsUnique();
            });

            model.Entity<FiscalPeriod>(e =>
            {
                e.ToTable("FiscalPeriods");
                e.HasKey(x => x.Id);
                e.HasIndex("FiscalYearId").HasDatabaseName("IX_FiscalPeriods_FiscalYearId");
            });

            model.Entity<FiscalYear>(e =>
            {
                e.ToTable("FiscalYears");
                e.HasKey(x => x.Id);
            });

            model.Entity<JobTitle>(e =>
            {
                e.ToTable("JobTitles");
                e.HasKey(x => x.Id);
            });

            model.Entity<JournalEntry>(e =>
            {
                e.ToTable("JournalEntries");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Lines);
                e.Property(x => x.TotalCredit).HasPrecision(18, 4);
                e.Property(x => x.TotalDebit).HasPrecision(18, 4);
                e.HasIndex("EntryNo").HasDatabaseName("IX_JournalEntries_EntryNo").IsUnique();
            });

            model.Entity<JournalLine>(e =>
            {
                e.ToTable("JournalEntryLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Credit).HasPrecision(18, 4);
                e.Property(x => x.Debit).HasPrecision(18, 4);
                e.HasIndex("AccountCode").HasDatabaseName("IX_JournalEntryLines_AccountCode");
                e.HasIndex("EntryId").HasDatabaseName("IX_JournalEntryLines_EntryId");
            });

            model.Entity<License>(e =>
            {
                e.ToTable("Licenses");
                e.HasKey(x => x.Id);
                e.HasIndex("Serial").HasDatabaseName("IX_Licenses_Serial").IsUnique();
            });

            model.Entity<NumberSequence>(e =>
            {
                e.ToTable("NumberSequences");
                e.HasKey(x => x.Id);
                e.HasIndex("Key").HasDatabaseName("IX_NumberSequences_Key").IsUnique();
            });

            model.Entity<PayrollLine>(e =>
            {
                e.ToTable("PayrollLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Advances).HasPrecision(18, 4);
                e.Property(x => x.Allowances).HasPrecision(18, 4);
                e.Property(x => x.BasicSalary).HasPrecision(18, 4);
                e.Property(x => x.Deductions).HasPrecision(18, 4);
                e.Ignore(x => x.EmployeeName);
                e.Property(x => x.Insurance).HasPrecision(18, 4);
                e.Property(x => x.NetSalary).HasPrecision(18, 4);
                e.Property(x => x.Overtime).HasPrecision(18, 4);
                e.Property(x => x.Tax).HasPrecision(18, 4);
                e.HasIndex("PayrollId").HasDatabaseName("IX_PayrollLines_PayrollId");
            });

            model.Entity<Payroll>(e =>
            {
                e.ToTable("Payrolls");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Lines);
                e.Property(x => x.NetTotal).HasPrecision(18, 4);
                e.Property(x => x.TotalAllowances).HasPrecision(18, 4);
                e.Property(x => x.TotalBasic).HasPrecision(18, 4);
                e.Property(x => x.TotalDeductions).HasPrecision(18, 4);
                e.HasIndex("PayrollNo").HasDatabaseName("IX_Payrolls_PayrollNo").IsUnique();
            });

            model.Entity<Permission>(e =>
            {
                e.ToTable("Permissions");
                e.HasKey(x => x.Id);
                e.HasIndex("Key").HasDatabaseName("IX_Permissions_Key").IsUnique();
            });

            model.Entity<Product>(e =>
            {
                e.ToTable("Products");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.BrandName);
                e.Ignore(x => x.CategoryName);
                e.Property(x => x.CostPrice).HasPrecision(18, 4);
                e.Ignore(x => x.CurrentStock);
                e.Property(x => x.MaxQty).HasPrecision(18, 4);
                e.Property(x => x.MinPrice).HasPrecision(18, 4);
                e.Property(x => x.MinQty).HasPrecision(18, 4);
                e.Property(x => x.ReorderPoint).HasPrecision(18, 4);
                e.Property(x => x.SalePrice).HasPrecision(18, 4);
                e.Property(x => x.TaxRate).HasPrecision(18, 4);
                e.Ignore(x => x.UnitName);
                e.HasIndex("BrandId").HasDatabaseName("IX_Products_BrandId");
                e.HasIndex("CategoryId").HasDatabaseName("IX_Products_CategoryId");
                e.HasIndex("Code").HasDatabaseName("IX_Products_Code").IsUnique();
            });

            model.Entity<PurchaseInvoiceLine>(e =>
            {
                e.ToTable("PurchaseInvoiceLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
                e.Property(x => x.DiscountPercent).HasPrecision(18, 4);
                e.Property(x => x.LineTotal).HasPrecision(18, 4);
                e.Property(x => x.NetAmount).HasPrecision(18, 4);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitPrice).HasPrecision(18, 4);
                e.Property(x => x.VatAmount).HasPrecision(18, 4);
                e.Property(x => x.VatPercent).HasPrecision(18, 4);
                e.Property(x => x.WithholdingAmount).HasPrecision(18, 4);
                e.Property(x => x.WithholdingPercent).HasPrecision(18, 4);
                e.HasIndex("InvoiceId").HasDatabaseName("IX_PurchaseInvoiceLines_InvoiceId");
            });

            model.Entity<PurchaseInvoice>(e =>
            {
                e.ToTable("PurchaseInvoices");
                e.HasKey(x => x.Id);
                e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
                e.Property(x => x.DiscountPercent).HasPrecision(18, 4);
                e.Property(x => x.ExchangeRate).HasPrecision(18, 4);
                e.Ignore(x => x.Lines);
                e.Property(x => x.NetTotal).HasPrecision(18, 4);
                e.Property(x => x.PaidAmount).HasPrecision(18, 4);
                e.Property(x => x.RemainingAmount).HasPrecision(18, 4);
                e.Property(x => x.SubTotal).HasPrecision(18, 4);
                e.Property(x => x.VatAmount).HasPrecision(18, 4);
                e.Property(x => x.WithholdingAmount).HasPrecision(18, 4);
                e.HasIndex("SupplierId").HasDatabaseName("IX_PurchaseInvoices_SupplierId");
                e.HasIndex("InvoiceNo").HasDatabaseName("IX_PurchaseInvoices_InvoiceNo").IsUnique();
            });

            model.Entity<PurchaseReturnLine>(e =>
            {
                e.ToTable("PurchaseReturnLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
                e.Property(x => x.DiscountPercent).HasPrecision(18, 4);
                e.Property(x => x.LineTotal).HasPrecision(18, 4);
                e.Property(x => x.NetAmount).HasPrecision(18, 4);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitPrice).HasPrecision(18, 4);
                e.Property(x => x.VatAmount).HasPrecision(18, 4);
                e.Property(x => x.VatPercent).HasPrecision(18, 4);
                e.Property(x => x.WithholdingAmount).HasPrecision(18, 4);
                e.Property(x => x.WithholdingPercent).HasPrecision(18, 4);
                e.HasIndex("ReturnId").HasDatabaseName("IX_PurchaseReturnLines_ReturnId");
            });

            model.Entity<PurchaseReturn>(e =>
            {
                e.ToTable("PurchaseReturns");
                e.HasKey(x => x.Id);
                e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
                e.Ignore(x => x.Lines);
                e.Property(x => x.NetTotal).HasPrecision(18, 4);
                e.Property(x => x.SubTotal).HasPrecision(18, 4);
                e.Property(x => x.VatAmount).HasPrecision(18, 4);
                e.Property(x => x.WithholdingAmount).HasPrecision(18, 4);
                e.HasIndex("SupplierId").HasDatabaseName("IX_PurchaseReturns_SupplierId");
                e.HasIndex("ReturnNo").HasDatabaseName("IX_PurchaseReturns_ReturnNo").IsUnique();
            });

            model.Entity<RolePermission>(e =>
            {
                e.ToTable("RolePermissions");
                e.HasKey(x => x.Id);
                e.HasIndex("RoleId").HasDatabaseName("IX_RolePermissions_RoleId");
            });

            model.Entity<Role>(e =>
            {
                e.ToTable("Roles");
                e.HasKey(x => x.Id);
                e.HasIndex("Name").HasDatabaseName("IX_Roles_Name").IsUnique();
            });

            model.Entity<SalesInvoiceLine>(e =>
            {
                e.ToTable("SalesInvoiceLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
                e.Property(x => x.DiscountPercent).HasPrecision(18, 4);
                e.Property(x => x.LineTotal).HasPrecision(18, 4);
                e.Property(x => x.NetAmount).HasPrecision(18, 4);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitPrice).HasPrecision(18, 4);
                e.Property(x => x.VatAmount).HasPrecision(18, 4);
                e.Property(x => x.VatPercent).HasPrecision(18, 4);
                e.Property(x => x.WithholdingAmount).HasPrecision(18, 4);
                e.Property(x => x.WithholdingPercent).HasPrecision(18, 4);
                e.HasIndex("InvoiceId").HasDatabaseName("IX_SalesInvoiceLines_InvoiceId");
            });

            model.Entity<SalesInvoice>(e =>
            {
                e.ToTable("SalesInvoices");
                e.HasKey(x => x.Id);
                e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
                e.Property(x => x.DiscountPercent).HasPrecision(18, 4);
                e.Property(x => x.ExchangeRate).HasPrecision(18, 4);
                e.Ignore(x => x.Lines);
                e.Property(x => x.NetTotal).HasPrecision(18, 4);
                e.Property(x => x.PaidAmount).HasPrecision(18, 4);
                e.Property(x => x.RemainingAmount).HasPrecision(18, 4);
                e.Property(x => x.SubTotal).HasPrecision(18, 4);
                e.Property(x => x.VatAmount).HasPrecision(18, 4);
                e.Property(x => x.WithholdingAmount).HasPrecision(18, 4);
                e.HasIndex("CustomerId").HasDatabaseName("IX_SalesInvoices_CustomerId");
                e.HasIndex("InvoiceNo").HasDatabaseName("IX_SalesInvoices_InvoiceNo").IsUnique();
            });

            model.Entity<SalesReturnLine>(e =>
            {
                e.ToTable("SalesReturnLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
                e.Property(x => x.DiscountPercent).HasPrecision(18, 4);
                e.Property(x => x.LineTotal).HasPrecision(18, 4);
                e.Property(x => x.NetAmount).HasPrecision(18, 4);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitPrice).HasPrecision(18, 4);
                e.Property(x => x.VatAmount).HasPrecision(18, 4);
                e.Property(x => x.VatPercent).HasPrecision(18, 4);
                e.Property(x => x.WithholdingAmount).HasPrecision(18, 4);
                e.Property(x => x.WithholdingPercent).HasPrecision(18, 4);
                e.HasIndex("ReturnId").HasDatabaseName("IX_SalesReturnLines_ReturnId");
            });

            model.Entity<SalesReturn>(e =>
            {
                e.ToTable("SalesReturns");
                e.HasKey(x => x.Id);
                e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
                e.Ignore(x => x.Lines);
                e.Property(x => x.NetTotal).HasPrecision(18, 4);
                e.Property(x => x.SubTotal).HasPrecision(18, 4);
                e.Property(x => x.VatAmount).HasPrecision(18, 4);
                e.Property(x => x.WithholdingAmount).HasPrecision(18, 4);
                e.HasIndex("CustomerId").HasDatabaseName("IX_SalesReturns_CustomerId");
                e.HasIndex("ReturnNo").HasDatabaseName("IX_SalesReturns_ReturnNo").IsUnique();
            });

            model.Entity<StockMovement>(e =>
            {
                e.ToTable("StockMovements");
                e.HasKey(x => x.Id);
                e.Property(x => x.BalanceAfter).HasPrecision(18, 4);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.TotalCost).HasPrecision(18, 4);
                e.Property(x => x.UnitCost).HasPrecision(18, 4);
                e.HasIndex("WarehouseId").HasDatabaseName("IX_StockMovements_WarehouseId");
                e.HasIndex("ProductId").HasDatabaseName("IX_StockMovements_ProductId");
            });

            model.Entity<StockTransferDocument>(e =>
            {
                e.ToTable("StockTransferDocuments");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Lines);
                e.HasIndex("DocNo").HasDatabaseName("IX_StockTransferDocuments_DocNo").IsUnique();
            });

            model.Entity<StockTransferLine>(e =>
            {
                e.ToTable("StockTransferLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.HasIndex("DocumentId").HasDatabaseName("IX_StockTransferLines_DocumentId");
            });

            model.Entity<Supplier>(e =>
            {
                e.ToTable("Suppliers");
                e.HasKey(x => x.Id);
                e.Property(x => x.Balance).HasPrecision(18, 4);
                e.Ignore(x => x.CategoryName);
                e.Property(x => x.CreditLimit).HasPrecision(18, 4);
                e.HasIndex("AccountCode").HasDatabaseName("IX_Suppliers_AccountCode");
                e.HasIndex("Code").HasDatabaseName("IX_Suppliers_Code").IsUnique();
            });

            model.Entity<Treasury>(e =>
            {
                e.ToTable("Treasuries");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.AccountBalance);
                e.Ignore(x => x.KindName);
                e.HasIndex("Code").HasDatabaseName("IX_Treasuries_Code").IsUnique();
            });

            model.Entity<Unit>(e =>
            {
                e.ToTable("Units");
                e.HasKey(x => x.Id);
            });

            model.Entity<UserPermission>(e =>
            {
                e.ToTable("UserPermissions");
                e.HasKey(x => x.Id);
                e.HasIndex("UserId").HasDatabaseName("IX_UserPermissions_UserId");
            });

            model.Entity<User>(e =>
            {
                e.ToTable("Users");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.RoleName);
                e.HasIndex("Username").HasDatabaseName("IX_Users_Username").IsUnique();
            });

            model.Entity<VoucherAllocation>(e =>
            {
                e.ToTable("VoucherAllocations");
                e.HasKey(x => x.Id);
                e.Property(x => x.Amount).HasPrecision(18, 4);
                e.HasIndex("VoucherId").HasDatabaseName("IX_VoucherAllocations_VoucherId");
            });

            model.Entity<Voucher>(e =>
            {
                e.ToTable("Vouchers");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Allocations);
                e.Property(x => x.Amount).HasPrecision(18, 4);
                e.HasIndex("VoucherDate").HasDatabaseName("IX_Vouchers_VoucherDate");
                e.HasIndex("VoucherNo").HasDatabaseName("IX_Vouchers_VoucherNo").IsUnique();
            });

            model.Entity<Warehouse>(e =>
            {
                e.ToTable("Warehouses");
                e.HasKey(x => x.Id);
                e.HasIndex("Code").HasDatabaseName("IX_Warehouses_Code").IsUnique();
            });

            model.SharedTypeEntity<StockAdjustment>("DeliveryNoteDocuments", e =>
            {
                e.ToTable("DeliveryNoteDocuments");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Lines);
                e.HasIndex("DocNo").HasDatabaseName("IX_DeliveryNoteDocuments_DocNo").IsUnique();
            });

            model.SharedTypeEntity<StockAdjustmentLine>("DeliveryNoteLines", e =>
            {
                e.ToTable("DeliveryNoteLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitCost).HasPrecision(18, 4);
                e.HasIndex("DocumentId").HasDatabaseName("IX_DeliveryNoteLines_DocumentId");
            });

            model.SharedTypeEntity<StockAdjustment>("GoodsIssueDocuments", e =>
            {
                e.ToTable("GoodsIssueDocuments");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Lines);
                e.HasIndex("DocNo").HasDatabaseName("IX_GoodsIssueDocuments_DocNo").IsUnique();
            });

            model.SharedTypeEntity<StockAdjustmentLine>("GoodsIssueLines", e =>
            {
                e.ToTable("GoodsIssueLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitCost).HasPrecision(18, 4);
                e.HasIndex("DocumentId").HasDatabaseName("IX_GoodsIssueLines_DocumentId");
            });

            model.SharedTypeEntity<StockAdjustment>("GoodsReceiptDocuments", e =>
            {
                e.ToTable("GoodsReceiptDocuments");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Lines);
                e.HasIndex("DocNo").HasDatabaseName("IX_GoodsReceiptDocuments_DocNo").IsUnique();
            });

            model.SharedTypeEntity<StockAdjustmentLine>("GoodsReceiptLines", e =>
            {
                e.ToTable("GoodsReceiptLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitCost).HasPrecision(18, 4);
                e.HasIndex("DocumentId").HasDatabaseName("IX_GoodsReceiptLines_DocumentId");
            });

            model.SharedTypeEntity<CycleDocument>("PurchaseOrderDocuments", e =>
            {
                e.ToTable("PurchaseOrderDocuments");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Lines);
                e.HasIndex("DocNo").HasDatabaseName("IX_PurchaseOrderDocuments_DocNo").IsUnique();
            });

            model.SharedTypeEntity<CycleDocumentLine>("PurchaseOrderLines", e =>
            {
                e.ToTable("PurchaseOrderLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitPrice).HasPrecision(18, 4);
                e.HasIndex("DocumentId").HasDatabaseName("IX_PurchaseOrderLines_DocumentId");
            });

            model.SharedTypeEntity<CycleDocument>("PurchaseRequestDocuments", e =>
            {
                e.ToTable("PurchaseRequestDocuments");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Lines);
                e.HasIndex("DocNo").HasDatabaseName("IX_PurchaseRequestDocuments_DocNo").IsUnique();
            });

            model.SharedTypeEntity<CycleDocumentLine>("PurchaseRequestLines", e =>
            {
                e.ToTable("PurchaseRequestLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitPrice).HasPrecision(18, 4);
                e.HasIndex("DocumentId").HasDatabaseName("IX_PurchaseRequestLines_DocumentId");
            });

            model.SharedTypeEntity<CycleDocument>("QuotationDocuments", e =>
            {
                e.ToTable("QuotationDocuments");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Lines);
                e.HasIndex("DocNo").HasDatabaseName("IX_QuotationDocuments_DocNo").IsUnique();
            });

            model.SharedTypeEntity<CycleDocumentLine>("QuotationLines", e =>
            {
                e.ToTable("QuotationLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitPrice).HasPrecision(18, 4);
                e.HasIndex("DocumentId").HasDatabaseName("IX_QuotationLines_DocumentId");
            });

            model.SharedTypeEntity<CycleDocument>("SalesOrderDocuments", e =>
            {
                e.ToTable("SalesOrderDocuments");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Lines);
                e.HasIndex("DocNo").HasDatabaseName("IX_SalesOrderDocuments_DocNo").IsUnique();
            });

            model.SharedTypeEntity<CycleDocumentLine>("SalesOrderLines", e =>
            {
                e.ToTable("SalesOrderLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitPrice).HasPrecision(18, 4);
                e.HasIndex("DocumentId").HasDatabaseName("IX_SalesOrderLines_DocumentId");
            });

            model.SharedTypeEntity<StockAdjustment>("SalesReceiptDocuments", e =>
            {
                e.ToTable("SalesReceiptDocuments");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Lines);
                e.HasIndex("DocNo").HasDatabaseName("IX_SalesReceiptDocuments_DocNo").IsUnique();
            });

            model.SharedTypeEntity<StockAdjustmentLine>("SalesReceiptLines", e =>
            {
                e.ToTable("SalesReceiptLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitCost).HasPrecision(18, 4);
                e.HasIndex("DocumentId").HasDatabaseName("IX_SalesReceiptLines_DocumentId");
            });

            model.SharedTypeEntity<StockAdjustment>("StockInDocuments", e =>
            {
                e.ToTable("StockInDocuments");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Lines);
                e.HasIndex("DocNo").HasDatabaseName("IX_StockInDocuments_DocNo").IsUnique();
            });

            model.SharedTypeEntity<StockAdjustmentLine>("StockInLines", e =>
            {
                e.ToTable("StockInLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitCost).HasPrecision(18, 4);
                e.HasIndex("DocumentId").HasDatabaseName("IX_StockInLines_DocumentId");
            });

            model.SharedTypeEntity<StockAdjustment>("StockOutDocuments", e =>
            {
                e.ToTable("StockOutDocuments");
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Lines);
                e.HasIndex("DocNo").HasDatabaseName("IX_StockOutDocuments_DocNo").IsUnique();
            });

            model.SharedTypeEntity<StockAdjustmentLine>("StockOutLines", e =>
            {
                e.ToTable("StockOutLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Qty).HasPrecision(18, 4);
                e.Property(x => x.UnitCost).HasPrecision(18, 4);
                e.HasIndex("DocumentId").HasDatabaseName("IX_StockOutLines_DocumentId");
            });

            foreach (var (table, columns) in BuiltTables.All)
                BuiltTables.Shape(model, table, columns);

            ModelConventions.HideDeleted(model);
        }
    }
}
