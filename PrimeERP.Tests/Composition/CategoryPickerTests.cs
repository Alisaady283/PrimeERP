using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Common;
using PrimeERP.Composition.Definitions;
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
    // يثبت بند "+ إضافة فئة" داخل منتقي الفئة نفسه (WireCategoryPickerAddOption) — حوار فئة متداخل يُفتح
    // من داخل حوار فئة آخر، يُحفَظ، والقائمة الأصلية تُعاد تحميلها لتشمل الفئة الجديدة.
    [Collection("WpfApplication")]
    public class CategoryPickerTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public CategoryPickerTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void AddCategorySentinel_OpensNestedDialog_AndReloadsParentPicker()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var dialog = CategoryDialogFactory.Build("Products");
                var toast = _db.Services.GetRequiredService<IToastService>();

                Exception thrown = null;
                Window window = null;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        window = System.Windows.Application.Current.Windows.OfType<Window>().Last();
                        var parentCombo = FindVisualChild<AppComboBox>(window);

                        // يُلتقَط أثناء PushFrame الداخلي الذي يفتحه SelectItem أدناه (حوار الفئة المتداخل).
                        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            var nestedWindow = System.Windows.Application.Current.Windows.OfType<Window>().Last();
                            FindVisualChild<AppTextBox>(nestedWindow).Text = "فئة متداخلة اختبارية";
                            ClickButton(nestedWindow, "Str.Save");
                        }));

                        var sentinel = ((System.Collections.IEnumerable)parentCombo.ItemsSource).Cast<object>().Last();
                        typeof(AppComboBox).GetMethod("SelectItem", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(parentCombo, new object[] { sentinel });

                        // الحوار المتداخل أُغلق الآن؛ الاختيار يُترَك فارغاً والقائمة تشمل الفئة الجديدة.
                        var reloaded = ((System.Collections.IEnumerable)parentCombo.ItemsSource).Cast<object>().ToList();
                        Assert.Null(parentCombo.SelectedItem);
                        Assert.Contains(reloaded, i => (string)i.GetType().GetProperty("Display").GetValue(i) == "فئة متداخلة اختبارية");

                        FindVisualChild<AppTextBox>(window).Text = "فئة رئيسية";
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

                var categories = _db.Services.GetRequiredService<ICategoryService>().GetAll("Products").Value;
                Assert.Contains(categories, c => c.Name == "فئة متداخلة اختبارية");
                Assert.Contains(categories, c => c.Name == "فئة رئيسية");
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
