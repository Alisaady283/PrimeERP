using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>صافي السطر يُحسب حيّاً أثناء الإدخال بنفس معادلة الترحيل، والنافذة تتّسع لأعمدته.</summary>
    [Collection("WpfApplication")]
    public class LineMathCheck : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public LineMathCheck() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void TheNetColumn_FollowsQuantityPriceAndRates()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");
                var toast = _db.Services.GetRequiredService<IToastService>();
                var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get("SalesInvoices").DocumentDialog;

                Window window = null;
                Exception thrown = null;
                var readings = new List<string>();
                double width = 0;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                {
                    try
                    {
                        window = System.Windows.Application.Current.Windows.OfType<Window>().Last();
                        width = window.Width;

                        var numerics = Descendants<AppNumericBox>(window).ToList();
                        var net = Descendants<AppTextBox>(window).First(t => t.IsReadOnly);

                        // ترتيب الأعمدة: الكمية، السعر، خصم %، ق.مضافة %، خ.إضافة %
                        numerics[0].Value = 10m;
                        numerics[1].Value = 120m;
                        readings.Add(net.Text);

                        numerics[2].Value = 10m;
                        readings.Add(net.Text);

                        numerics[3].Value = 14m;
                        readings.Add(net.Text);

                        numerics[4].Value = 1m;
                        readings.Add(net.Text);
                    }
                    catch (Exception ex) { thrown = ex; }
                    finally { window?.Close(); }
                }));

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (_, __) => { timer.Stop(); window?.Close(); };
                timer.Start();

                try { DocumentRenderer.ShowAndSave(definition, _db.Services, toast); }
                catch (Exception ex) { thrown = ex; }

                Assert.Null(thrown);
                Assert.Equal(new[] { "1,200.00", "1,080.00", "1,231.20", "1,220.40" }, readings);
                Assert.Equal(1260d, width);
            });
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            if (root is T typed) yield return typed;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
                foreach (var found in Descendants<T>(VisualTreeHelper.GetChild(root, i)))
                    yield return found;
        }
    }
}
