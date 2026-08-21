using System.Configuration;
using System.Data;
using System.Windows;
using PrimeERP.Domain.Contracts;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;
using PrimeERP.Application;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Backup;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Print;
using PrimeERP.Platform.Settings;

namespace PrimeERP.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ServiceLocator.Register<INavigationService>(NavigationService.Instance);
        ServiceLocator.Register<INumberSequenceService>(NumberSequenceService.Instance);
        ServiceLocator.Register<ISettingsService>(SettingsService.Instance);
        ServiceLocator.Register<IBackupService>(BackupService.Instance);
        ServiceLocator.Register<IPrintService>(PrintService.Instance);
        ServiceLocator.Register<IAccountService>(AccountService.Instance);
        ServiceLocator.Register<IFiscalPeriodService>(FiscalPeriodService.Instance);
        ServiceLocator.Register<IJournalService>(JournalService.Instance);
        ServiceLocator.Register<ICustomerService>(CustomerService.Instance);
        ServiceLocator.Register<IDialogService>(DialogService.Instance);
        ServiceLocator.Register<IToastService>(ToastService.Instance);
        ServiceLocator.Register<IExportService>(ExportService.Instance);
        ServiceLocator.Register<IDocumentExporter>(ExportService.Instance);
        ServiceLocator.Register<IIdentityService>(IdentityService.Instance);

        IdentityService.Instance.Initialize();
    }
}

