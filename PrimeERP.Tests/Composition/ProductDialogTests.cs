using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    [Collection("WpfApplication")]
    public class ProductDialogTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public ProductDialogTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void ShowAndSave_AddMode_CreatesProduct_UnderCategory()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var categories = _db.Services.GetRequiredService<ICategoryService>();
                var category = categories.Create(new CreateCategoryDto { Name = "أدوات كهربائية", ModuleKey = "Products" });
                Assert.True(category.IsSuccess, category.ErrorMessage);

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var dialog = registry.Get("Products").Dialog;
                Assert.NotNull(dialog);
                var toast = _db.Services.GetRequiredService<IToastService>();

                Exception thrown = null;
                Window window = null;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        window = System.Windows.Application.Current.Windows.OfType<Window>().Last();
                        FindVisualChild<AppTextBox>(window).Text = "صنف من الحوار";

                        var numerics = FindAllVisualChildren<AppNumericBox>(window).ToList();
                        numerics[0].Value = 50m; // CostPrice
                        numerics[1].Value = 80m; // SalePrice

                        var combo = FindVisualChild<AppComboBox>(window);
                        var categoryItem = ((System.Collections.IEnumerable)combo.ItemsSource).Cast<object>()
                            .First(i => (string)i.GetType().GetProperty("Display").GetValue(i) == "أدوات كهربائية");
                        combo.SelectedItem = categoryItem;

                        ClickButton(window, "Str.Save");
                    }
                    catch (Exception ex) { thrown = ex; window?.Close(); }
                }));

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (_, __) => { timer.Stop(); window?.Close(); };
                timer.Start();

                bool saved = false;
                try { saved = DialogRenderer.ShowAndSave(dialog, _db.Services, toast); }
                catch (Exception ex) { thrown = ex; }

                Assert.Null(thrown);
                Assert.True(saved);

                var products = _db.Services.GetRequiredService<IProductService>();
                var all = products.GetPaged(1, 20).Value.Items;
                var created = all.Single(p => p.Name == "صنف من الحوار");
                Assert.Equal(category.Value.Id, created.CategoryId);
                Assert.Equal(80m, created.SalePrice);

                // فلترة بسيطة بالفئة — تعمل عبر ProductFilter.CategoryId فعلياً، لا واجهة UI بعد.
                var filtered = products.GetPaged(1, 20, new ProductFilter { CategoryId = category.Value.Id }).Value.Items;
                Assert.Contains(filtered, p => p.Id == created.Id);

                var otherCategory = categories.Create(new CreateCategoryDto { Name = "فئة أخرى", ModuleKey = "Products" });
                var filteredOther = products.GetPaged(1, 20, new ProductFilter { CategoryId = otherCategory.Value.Id }).Value.Items;
                Assert.DoesNotContain(filteredOther, p => p.Id == created.Id);
            });
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var nested = FindVisualChild<T>(child);
                if (nested != null) return nested;
            }
            return null;
        }

        private static void ClickButton(DependencyObject root, string localizationKey)
        {
            var text = LocalizationService.Get(localizationKey);
            var button = FindAllVisualChildren<AppButton>(root).First(b => b.Text == text);
            var innerButton = FindVisualChild<Button>(button);
            innerButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        private static System.Collections.Generic.IEnumerable<T> FindAllVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) yield return typed;
                foreach (var nested in FindAllVisualChildren<T>(child)) yield return nested;
            }
        }
    }
}
