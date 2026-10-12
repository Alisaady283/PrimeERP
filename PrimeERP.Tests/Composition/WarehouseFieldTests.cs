using System;
using PrimeERP.Application.PageServices.Admin;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    [Collection("WpfApplication")]
    public class WarehouseFieldTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public WarehouseFieldTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Theory]
        [InlineData("SalesInvoices")]
        [InlineData("PurchaseInvoices")]
        [InlineData("SalesReturns")]
        [InlineData("PurchaseReturns")]
        public void SimplifiedFlow_ShowsTheWarehouseInTheForm(string moduleKey)
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();
                _db.Services.GetRequiredService<ISettingsService>()
                    .SetMany(new Dictionary<string, object> { [SettingKeys.Documents.SimplifiedFlow] = true });

                var def = _db.Services.GetRequiredService<IModuleRegistry>().Get(moduleKey).DocumentDialog;
                var toast = _db.Services.GetRequiredService<IToastService>();
                var label = LocalizationService.Get("Str.Warehouse");

                bool? visible = null;
                Window window = null;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    window = System.Windows.Application.Current.Windows.OfType<Window>().Last();
                    var warehouse = All<AppComboBox>(window).FirstOrDefault(c => c.Label == label);
                    visible = warehouse?.IsVisible;
                    window.Close();
                }));

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (_, __) => { timer.Stop(); window?.Close(); };
                timer.Start();

                DocumentRenderer.ShowAndSave(def, _db.Services, toast);

                Assert.True(visible == true, $"{moduleKey}: warehouse field {(visible == null ? "missing" : "hidden")}");
            });
        }

        private static IEnumerable<T> All<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) yield return typed;
                foreach (var nested in All<T>(child)) yield return nested;
            }
        }
    }
}
