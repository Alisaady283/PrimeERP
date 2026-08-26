using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.App.Bootstrap;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;

namespace PrimeERP.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    /// <summary>حاوية DI الحقيقية — بديل ServiceLocator (R3). عامة للقراءة فقط؛ التهيئة الوحيدة هنا في OnStartup.</summary>
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
        Services.EnsureDatabaseReady();
        Services.RegisterModules();
        UIServices.Initialize(Services);

        Services.GetRequiredService<IIdentityService>().Initialize();

        // ⚠️ توقف 10 — Window.ShowDialog() تُعلَّق للأبد هنا (بيئة تشغيل هذا الجهاز تحديداً — راجع
        // ARCHITECTURE.md)؛ Show() تعمل فوراً. الحل: Show() + حلقة Dispatcher يدوية (DispatcherFrame)
        // تُحاكي حجب ShowDialog دون استخدام آليته الداخلية المُعطَّلة. ShutdownMode=OnExplicitShutdown في
        // App.xaml لهذا السبب بالضبط — لا اعتماد على أي نافذة تُصبح MainWindow تلقائياً.
        var login = new LoginWindow(Services.GetRequiredService<IPermissionService>());
        var loginFrame = new DispatcherFrame();
        login.Closed += (_, __) => loginFrame.Continue = false;
        login.Show();
        Dispatcher.PushFrame(loginFrame);

        if (!login.LoginSucceeded)
        {
            Shutdown();
            return;
        }

        var main = new MainWindow(Services);
        main.Closed += (_, __) => Shutdown();
        main.Show();
    }
}
