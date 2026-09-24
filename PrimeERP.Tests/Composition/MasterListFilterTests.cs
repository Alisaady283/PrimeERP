using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>فلترة القائمة الرئيسية</summary>
    [Collection("WpfApplication")]
    public class MasterListFilterTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public MasterListFilterTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void CategoryFilterCombo_SelectingCategory_ReloadsGrid_FilteredToThatCategory()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();

                var categories = _db.Services.GetRequiredService<ICategoryService>();
                var catA = categories.Create(new CreateCategoryDto { Name = "فئة أ", ModuleKey = "Products" }).Value;
                var catB = categories.Create(new CreateCategoryDto { Name = "فئة ب", ModuleKey = "Products" }).Value;

                var products = _db.Services.GetRequiredService<IProductService>();
                products.Create(new CreateProductDto { Name = "منتج أ", CategoryId = catA.Id, CostPrice = 1, SalePrice = 2 });
                products.Create(new CreateProductDto { Name = "منتج ب", CategoryId = catB.Id, CostPrice = 1, SalePrice = 2 });

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var definition = registry.Get("Products");

                var element = CrudPageRenderer.Render(definition, _db.Services);
                element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                dynamic vm = element.DataContext;
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (((IEnumerable)vm.Items).Cast<object>().Count() < 2 && DateTime.UtcNow < deadline)
                {
                    var frame = new DispatcherFrame();
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                    Dispatcher.PushFrame(frame);
                }
                Assert.Equal(2, ((IEnumerable)vm.Items).Cast<object>().Count());

                var filterBar = FindVisualChild<FilterBar>(element);
                var combo = FindVisualChild<AppComboBox>((DependencyObject)filterBar.FiltersContent);
                var itemA = ((IEnumerable)combo.ItemsSource).Cast<object>().First(i => (string)i.GetType().GetProperty("Display").GetValue(i) == "فئة أ");
                combo.SelectedItem = itemA;

                typeof(AppComboBox).GetMethod("SelectItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(combo, new object[] { itemA });

                deadline = DateTime.UtcNow.AddSeconds(5);
                while (((IEnumerable)vm.Items).Cast<object>().Count() != 1 && DateTime.UtcNow < deadline)
                {
                    var frame = new DispatcherFrame();
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                    Dispatcher.PushFrame(frame);
                }

                var items = ((IEnumerable)vm.Items).Cast<object>().ToList();
                Assert.Single(items);
                Assert.Equal("منتج أ", (string)items[0].GetType().GetProperty("Name").GetValue(items[0]));
            });
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var nested = FindVisualChild<T>(child);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
