using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.App.Bootstrap;
using PrimeERP.Application.PageServices.Backup;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.Platform;
using PrimeERP.Data;
using PrimeERP.Application;

namespace PrimeERP.App;

/// <summary>Interaction logic for App.xaml</summary>
public partial class App : System.Windows.Application
{
    public static System.IServiceProvider Services { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LanguageResources.Apply(AppLanguage.Ar);

        var services = new ServiceCollection()
            .AddPlatform()
            .AddData()
            .AddApplication()
            .AddUI()
            .AddComposition();

        Services = services.BuildServiceProvider();
        Services.EnsureDatabaseReady(LanguageResources.Apply);
        Services.RegisterModules();

        UIServices.Initialize(Services);

        Services.GetRequiredService<IIdentityService>().Initialize();

        // ⚠️ Show() not ShowDialog() — ARCHITECTURE § المصائد
        var login = new LoginWindow(Services.GetRequiredService<PrimeERP.Application.PageServices.Security.IAuthService>());
        var loginFrame = new DispatcherFrame();
        login.Closed += (_, __) => loginFrame.Continue = false;
        login.Show();
        Dispatcher.PushFrame(loginFrame);

        if (!login.LoginSucceeded)
        {
            Shutdown();
            return;
        }

        var backup = Services.GetRequiredService<IBackupService>();
        backup.BackupFailed += Services.GetRequiredService<IToastService>().Error;
        backup.StartAutoBackup();
        Services.GetRequiredService<PrimeERP.Application.Services.Integration.AlertRunner>().Start();

        var main = new MainWindow(Services);
        main.Closed += (_, __) => Shutdown();
        MainWindow = main;
        main.Show();
    }
}
