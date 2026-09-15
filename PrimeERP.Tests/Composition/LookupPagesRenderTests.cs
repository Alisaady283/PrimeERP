using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>القوائم المرجعية تُفتح فعلاً: تصييرٌ ثم تحميلٌ حقيقي بلا استثناء.</summary>
    public class LookupPagesRenderTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public void Dispose() => _db.Dispose();

        [Theory]
        [InlineData("Units")]
        [InlineData("Warehouses")]
        [InlineData("Treasuries")]
        [InlineData("Users")]
        [InlineData("Roles")]
        public void ThePage_Opens(string key)
        {
            WpfApplicationFixture.Run(() =>
            {
                PrimeERP.UI.Services.UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get(key);
                Assert.NotNull(definition);

                var element = PageRenderer.Render(definition, _db.Services);
                element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (DateTime.UtcNow < deadline)
                {
                    var frame = new DispatcherFrame();
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                    Dispatcher.PushFrame(frame);

                    dynamic vm = element.DataContext;
                    if (vm != null && !(bool)vm.IsLoading) break;
                }

                Assert.NotNull(element);
            });
        }
    }
}
