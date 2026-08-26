using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>يثبت أن CrudPageRenderer يبني صفحة حقيقية (لا XAML) من ModuleDefinition، تُحلّ ViewModel
    /// حقيقية عبر DI، وتُحمّل بيانات حقيقية من ICustomerService — على خيط STA حقيقي (عناصر WPF فعلية).
    /// [Collection("WpfApplication")] لمنع تعارض إنشاء System.Windows.Application المتوازي — نفس نمط
    /// IdentityServiceTests.</summary>
    [Collection("WpfApplication")]
    public class CrudPageRendererTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public CrudPageRendererTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Render_BuildsPage_AndLoadsRealDataOnLoaded()
        {
            _db.Services.GetRequiredService<ICustomerService>()
               .Create(new CreateCustomerDto { Name = "عميل Renderer" });

            WpfApplicationFixture.Run(() =>
            {
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var definition = registry.Get("Customers");
                Assert.NotNull(definition);

                var element = CrudPageRenderer.Render(definition, _db.Services);
                Assert.IsType<Grid>(element);
                Assert.Equal(4, ((Grid)element).Children.Count);

                element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                dynamic vm = element.DataContext;
                Assert.NotNull(vm);

                // Loaded يستدعي LoadAsync عبر async void — WpfApplicationFixture خيط بـDispatcher.Run فعلي،
                // فمتابعة await تُجدوَل على طابور هذا الخيط نفسه (DispatcherSynchronizationContext حقيقية).
                // بما أننا الآن **داخل** الاستدعاء المتزامن نفسه (Dispatcher.Invoke)، انتظار Thread.Sleep هنا
                // كان سيُعلِّق الخيط عن معالجة طابوره — ضخّ إطارات متداخلة (PushFrame) هو الحل الصحيح الوحيد.
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (!((IEnumerable)vm.Items).Cast<object>().Any() && DateTime.UtcNow < deadline)
                {
                    var frame = new DispatcherFrame();
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                    Dispatcher.PushFrame(frame);
                }

                Assert.True(((IEnumerable)vm.Items).Cast<object>().Any());
            });
        }
    }
}
