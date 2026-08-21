using System.Configuration;
using System.Data;
using System.Windows;
using PrimeERP.Services;
using PrimeERP.Services.Accounting;
using PrimeERP.Services.Backup;
using PrimeERP.Services.Design;
using PrimeERP.Services.Parties;
using PrimeERP.Services.Print;
using PrimeERP.Services.Settings;

namespace PrimeERP;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
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
        ServiceLocator.Register<IIdentityService>(IdentityService.Instance);

        IdentityService.Instance.Initialize();
    }
}

