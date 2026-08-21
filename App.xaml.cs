using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.App.Bootstrap;
using PrimeERP.Platform.Design;
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
            .AddModules();

        Services = services.BuildServiceProvider();
        UIServices.Initialize(Services);

        Services.GetRequiredService<IIdentityService>().Initialize();
    }
}
