using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>حفظ عرض سعر من الشاشة الحقيقية. كان يُنهي التطبيق: Type.GetMethod على IQuotationService لا
    /// يرى Create المُوَرَّثة من ICycleDocumentService، فيُستدعى null. الاختبار يمسك الاستثناء بدل أن يسقط التطبيق.</summary>
    [Collection("WpfApplication")]
    public class QuotationDocumentTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly RecordingToastService _toasts = new();
        private readonly IServiceProvider _services;

        public QuotationDocumentTests()
        {
            AppSession.DevMode = true;
            // حاوية بنفس تسجيل الإنتاج عدا الإشعارات — رسالة الفشل تصل للاختبار بدل أن تُبتلَع في الواجهة.
            _services = TestDatabaseFixture.BuildServices(s => s.AddSingleton<IToastService>(_toasts));
        }

        public void Dispose() => _db.Dispose();

        private class RecordingToastService : IToastService
        {
            public List<string> Errors { get; } = new();
            public void Success(string message, int durationMs = 3000) { }
            public void Error(string message) => Errors.Add(message);
            public void Warning(string message, int durationMs = 4000) { }
            public void Info(string message, int durationMs = 3000) { }
        }

        [Fact]
        public void SavingAQuotationFromTheRealPage_Succeeds()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_services);
                _services.GetRequiredService<IIdentityService>().Apply("Default");

                var product = _services.GetRequiredService<IProductService>().Create(new CreateProductDto
                { Name = "صنف عرض سعر", CostPrice = 10, SalePrice = 25, IsActive = true });
                Assert.True(product.IsSuccess, product.ErrorMessage);

                var definition = _services.GetRequiredService<IModuleRegistry>().Get("Quotation");
                Assert.NotNull(definition);

                var element = CrudPageRenderer.Render(definition, _services);
                element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                dynamic vm = element.DataContext;
                Exception thrown = null;
                Window window = null;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        window = System.Windows.Application.Current.Windows.OfType<Window>().Last();

                        FindVisualChild<AppDatePicker>(window).SelectedDate = DateTime.Today;

                        var combos = FindAllVisualChildren<AppComboBox>(window).ToList();
                        var productCombo = combos.First(c => c.ItemsSource != null
                            && c.ItemsSource.Cast<object>().Any(i => (i.GetType().GetProperty("Code")?.GetValue(i) as string) == product.Value.Code));
                        SelectByCode(productCombo, product.Value.Code);

                        FindAllVisualChildren<AppNumericBox>(window).First().Value = 3m;

                        ClickButton(window, "Str.Save");
                    }
                    catch (Exception ex) { thrown = ex; window?.Close(); }
                }));

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (_, __) => { timer.Stop(); window?.Close(); };
                timer.Start();

                ((System.Windows.Input.ICommand)vm.AddCommand).Execute(null);

                Assert.True(thrown == null, thrown?.ToString());
                Assert.True(_toasts.Errors.Count == 0, string.Join(" | ", _toasts.Errors));

                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (!((System.Collections.IEnumerable)vm.Items).Cast<object>().Any() && DateTime.UtcNow < deadline)
                {
                    var frame = new DispatcherFrame();
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                    Dispatcher.PushFrame(frame);
                }

                var saved = _services.GetRequiredService<PrimeERP.Application.Services.Documents.IQuotationService>().GetPaged(1, 50);
                Assert.True(saved.Value.Items.Count > 0, "لم يُحفظ عرض السعر");
                Assert.NotEmpty(((System.Collections.IEnumerable)vm.Items).Cast<object>().ToList());
            });
        }

        private static void SelectByCode(AppComboBox combo, string code)
        {
            var item = combo.ItemsSource.Cast<object>()
                .First(i => (string)i.GetType().GetProperty("Code").GetValue(i) == code);
            combo.SelectedItem = item;
        }

        private static void ClickButton(DependencyObject root, string labelKey)
        {
            var text = PrimeERP.Platform.Localization.LocalizationService.Get(labelKey);
            var button = FindAllVisualChildren<AppButton>(root).First(b => b.Text == text);
            var inner = FindVisualChild<Button>(button);
            inner.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        }

        private static T FindVisualChild<T>(DependencyObject root) where T : DependencyObject =>
            FindAllVisualChildren<T>(root).FirstOrDefault();

        private static IEnumerable<T> FindAllVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T typed) yield return typed;

                foreach (var descendant in FindAllVisualChildren<T>(child)) yield return descendant;
            }
        }
    }
}
