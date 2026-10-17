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
            services.AddSingleton<IPrintService, PrintService>();
            services.AddSingleton<IChequePrinter, ChequePrinter>();

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
            services.AddSingleton<PrimeERP.Composition.Pull.PullService>();
            services.AddSingleton<PrimeERP.Composition.Pull.IPullService>(sp => sp.GetRequiredService<PrimeERP.Composition.Pull.PullService>());
            return services;
        }

        public static IServiceProvider RegisterModules(this IServiceProvider services)
        {
            var manifest = services.GetRequiredService<ISettingsService>().Get(SettingKeys.UI.Manifest, "");
            if (!string.IsNullOrWhiteSpace(manifest))
                services.GetRequiredService<IModuleRegistry>().Manifest =
                    manifest.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

            ModuleRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            HrRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            ManufacturingRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            ReportRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            AnalyticsRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            PermissionModuleRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            CycleDocumentRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            TreasuryRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());

            BuilderRegistrations.RegisterAll(services.GetRequiredService<IModuleRegistry>());
            BuilderModuleLoader.RegisterAll(services.GetRequiredService<IModuleRegistry>(), services);

            PermissionSeeder.Seed(services.GetRequiredService<IPermissionStore>());

            services.GetRequiredService<IPrintService>().DialogHost = services.GetRequiredService<IPrintDialogHost>();
            return services;
        }
    }
}
