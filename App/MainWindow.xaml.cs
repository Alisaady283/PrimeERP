using PrimeERP.Application.PageServices.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Components.Shell;

namespace PrimeERP.App
{
    /// <summary>القطعة الوحيدة هنا AppShell</summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _services;
        private readonly IModuleRegistry _registry;

        public MainWindow(IServiceProvider services)
        {
            InitializeComponent();
            FlowDirection = PrimeERP.Platform.Localization.LocalizationService.Flow;
            Loaded += async (_, __) => await PrimeERP.UI.Services.UpdateFlow.SyncAsync(services);
            _services = services;
            _registry = services.GetRequiredService<IModuleRegistry>();

            shell.CompanyName = services.GetRequiredService<ISettingsService>().Get(SettingKeys.Company.Name, "PrimeERP");
            shell.UserName = AppSession.DisplayName;
            shell.UserRole = AppSession.RoleName;
            var settings = services.GetRequiredService<ISettingsProvider>();
            if (!settings.Get(SettingKeys.Edition.Arabic, true) || !settings.Get(SettingKeys.Edition.English, true)) shell.HideLanguage();

            shell.NavItems = BuildNavGroups();

            var first = _registry.All().FirstOrDefault();
            if (first != null)
            {
                shell.SelectedKey = first.Key;
                shell.CurrentPage = PageRenderer.Render(first, _services);
            }
        }

        private static readonly Dictionary<string, string> ModuleIconKeys = new()
        {
            ["Accounts"] = "IconAccounts", ["Journals"] = "IconJournal",
            ["Customers"] = "IconCustomers", ["Suppliers"] = "IconSuppliers",
            ["Products"] = "IconProducts", ["Categories"] = "IconFolder", ["Brands"] = "IconBuilding",
            ["Units"] = "IconUnits", ["Warehouses"] = "IconWarehouse",
            ["Assets"] = "IconAssets", ["AssetCategories"] = "IconFolder",
            ["Employees"] = "IconHR", ["HrLists"] = "IconFolder", ["Payroll"] = "IconPayroll",
            ["SalesInvoices"] = "IconSales", ["SalesReturns"] = "IconSales",
            ["Quotation"] = "IconSales", ["SalesOrder"] = "IconSales",
            ["PurchaseRequest"] = "IconPurchases", ["PurchaseOrder"] = "IconPurchases",
            ["PurchaseInvoices"] = "IconPurchases", ["PurchaseReturns"] = "IconPurchases",
            ["StockIn"] = "IconWarehouse", ["StockOut"] = "IconWarehouse", ["StockTransfer"] = "IconWarehouse",
            ["GoodsReceipt"] = "IconWarehouse", ["GoodsIssue"] = "IconWarehouse",
            ["DeliveryNote"] = "IconWarehouse", ["SalesReceipt"] = "IconWarehouse",
            ["Treasuries"] = "IconAccounts", ["Receipts"] = "IconJournal", ["Payments"] = "IconJournal", ["Cheques"] = "IconJournal",
            ["ChequeReceipts"] = "IconJournal", ["ChequeIssues"] = "IconJournal", ["OpeningBalances"] = "IconJournal",
            ["Settings"] = "IconSettings", ["Users"] = "IconUsers", ["Roles"] = "IconLock",
            ["RolePermissions"] = "IconLock", ["UserPermissions"] = "IconLock",
        };

        private static string IconKeyForModule(string key) =>
            ModuleIconKeys.TryGetValue(key, out var icon) ? icon : "IconReports";

        private List<NavItem> BuildNavGroups()
        {
            var groups = NavigationSource.Groups(_services);

            var simplified = _services.GetRequiredService<ISettingsService>().Get(SettingKeys.Documents.SimplifiedFlow, true);
            var visible = _registry.VisibleFor(simplified).Select(m => m.Key).ToHashSet();

            var result = new List<NavItem>();
            foreach (var group in groups)
            {
                var children = group.Keys
                    .Where(visible.Contains)
                    .Select(k => _registry.Get(k))
                    .Where(m => m != null)
                    .Select(m => new NavItem { Key = m.Key, Text = LocalizationService.Get(m.TitleKey), PermissionKey = $"{m.PermissionPrefix}.View", IconKey = IconKeyForModule(m.Key) })
                    .ToList();
                if (children.Count == 0) continue;
                result.Add(new NavItem { Text = group.Text, IconKey = group.IconKey, Children = children });
            }
            return result;
        }

        private void Shell_NavigationRequested(object sender, string key)
        {
            var definition = _registry.Get(key);
            if (definition == null) return;

            shell.CurrentPage = PageRenderer.Render(definition, _services);
        }

        private async void Shell_UpdateRequested(object sender, EventArgs e) =>
            await PrimeERP.UI.Services.UpdateFlow.RunAsync(_services);

        private void Shell_PasswordChangeRequested(object sender, EventArgs e) =>
            PrimeERP.Composition.Renderers.DialogRenderer.ShowAndSave(new PrimeERP.Composition.Definitions.DialogDefinition
            {
                TitleKey = "Str.TopBar.ChangePassword", TitleEditKey = "Str.TopBar.ChangePassword", GridColumns = 1,
                ServiceType = typeof(PrimeERP.Application.PageServices.Security.IUserService),
                CreateDtoType = typeof(PrimeERP.Application.DTOs.Security.ChangePasswordDto),
                UpdateDtoType = typeof(PrimeERP.Application.DTOs.Security.ChangePasswordDto),
                CreateMethod = nameof(PrimeERP.Application.PageServices.Security.IUserService.ChangeOwnPassword),
                Fields = new()
                {
                    new() { Key = nameof(PrimeERP.Application.DTOs.Security.ChangePasswordDto.CurrentPassword), LabelKey = "Str.User.CurrentPassword", Kind = PrimeERP.Composition.Definitions.FieldKind.Password, IsRequired = true },
                    new() { Key = nameof(PrimeERP.Application.DTOs.Security.ChangePasswordDto.NewPassword), LabelKey = "Str.User.NewPassword", Kind = PrimeERP.Composition.Definitions.FieldKind.Password, IsRequired = true },
                    new() { Key = nameof(PrimeERP.Application.DTOs.Security.ChangePasswordDto.ConfirmPassword), LabelKey = "Str.User.ConfirmPassword", Kind = PrimeERP.Composition.Definitions.FieldKind.Password, IsRequired = true },
                }
            }, _services, _services.GetRequiredService<PrimeERP.UI.Services.IToastService>());

        private async void Shell_LanguageToggled(object sender, EventArgs e)
        {
            var dialogs = _services.GetRequiredService<PrimeERP.UI.Services.IDialogService>();
            if (!await dialogs.ConfirmAsync(LocalizationService.Get("Str.TopBar.Language"), LocalizationService.Get("Str.TopBar.LanguageRestart"))) return;

            _services.GetRequiredService<ISettingsProvider>().SetRaw(SettingKeys.UI.Language,
                LocalizationService.CurrentLanguage == AppLanguage.Ar ? "En" : "Ar");
            System.Diagnostics.Process.Start(Environment.ProcessPath!);
            System.Windows.Application.Current.Shutdown();
        }

        private void Shell_LogoutRequested(object sender, EventArgs e)
        {
            AppSession.SignOut();
            Close();
        }
    }
}
