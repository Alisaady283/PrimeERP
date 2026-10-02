using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.App.Bootstrap;
using PrimeERP.Application.Legacy.Backup;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;

namespace PrimeERP.App;

/// <summary>Interaction logic for App.xaml</summary>
public partial class App : System.Windows.Application
{
    public static System.IServiceProvider Services { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection()
            .AddPlatform()
            .AddData()
            .AddApplication()
            .AddUI()
            .AddComposition();

        Services = services.BuildServiceProvider();

        UIServices.Initialize(Services);

        Services.GetRequiredService<IIdentityService>().Initialize();

        using (var progress = Services.GetRequiredService<IDialogService>()
                   .ShowProgress(LocalizationService.Get("Str.AppName"), LocalizationService.Get("Str.Loading")))
            Pump(Task.Run(() => Services.EnsureDatabaseReady(new Progress<double>(progress.Report))));

        Services.RegisterModules();

        // ⚠️ Show() not ShowDialog() — ARCHITECTURE § المصائد
        var login = new LoginWindow(Services.GetRequiredService<IPermissionService>(),
                                     Services.GetRequiredService<IPermissionStore>());
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

        var main = new MainWindow(Services);
        main.Closed += (_, __) => Shutdown();
        MainWindow = main;
        main.Show();
    }

    /// <summary>انتظارٌ لا يجمّد الواجهة</summary>
    private void Pump(Task work)
    {
        var frame = new DispatcherFrame();
        work.ContinueWith(_ => Dispatcher.InvokeAsync(() => frame.Continue = false), TaskScheduler.Default);
        Dispatcher.PushFrame(frame);
        work.GetAwaiter().GetResult();
    }
}
