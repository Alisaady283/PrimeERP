using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
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
    public class DialogRendererTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public DialogRendererTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void ShowAndSave_AddMode_CreatesAccount_WithoutCrashing()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var dialog = registry.Get("Accounts").Dialog;
                var toast = _db.Services.GetRequiredService<IToastService>();

                bool saved = false;
                Exception thrown = null;
                Window window = null;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        window = System.Windows.Application.Current.Windows.OfType<Window>().Last();
                        SetComboSelection(window, "12");
                        SetTextBoxValue(window, "حساب اختباري");
                        ClickButton(window, LocalizationService.Get("Str.Save"));
                    }
                    catch (Exception ex) { thrown = ex; window?.Close(); }
                }));

                // شبكة أمان — لو تعطّل الحوار لأي سبب (لا يُغلق أبداً) يُعلَّق PushFrame للأبد؛ إغلاق قسري
                // بعد مهلة يمنع تعليق الاختبار كاملاً بدل ظهور سبب حقيقي.
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (_, __) => { timer.Stop(); window?.Close(); };
                timer.Start();

                try { saved = DialogRenderer.ShowAndSave(dialog, _db.Services, toast); }
                catch (Exception ex) { thrown = ex; }

                Assert.Null(thrown);
                Assert.True(saved);

                var accounts = _db.Services.GetRequiredService<IAccountService>();
                var all = accounts.GetPaged(1, 5000).Value.Items;
                Assert.Contains(all, a => a.Name == "حساب اختباري");
            });
        }

        [Fact]
        public void ShowAndSave_EditMode_UpdatesAccount_WithoutCrashing()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var accountsService = _db.Services.GetRequiredService<IAccountService>();
                var created = accountsService.Create(new CreateAccountDto { ParentId = 1, Name = "قبل التعديل", IsLeaf = true });
                Assert.True(created.IsSuccess, created.ErrorMessage);

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var dialog = registry.Get("Accounts").Dialog;
                var toast = _db.Services.GetRequiredService<IToastService>();

                bool saved = false;
                Exception thrown = null;
                Window window = null;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        window = System.Windows.Application.Current.Windows.OfType<Window>().Last();
                        SetTextBoxValue(window, "بعد التعديل");
                        ClickButton(window, LocalizationService.Get("Str.Save"));
                    }
                    catch (Exception ex) { thrown = ex; window?.Close(); }
                }));

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (_, __) => { timer.Stop(); window?.Close(); };
                timer.Start();

                try { saved = DialogRenderer.ShowAndSave(dialog, _db.Services, toast, created.Value); }
                catch (Exception ex) { thrown = ex; }

                Assert.Null(thrown);
                Assert.True(saved);

                var updated = accountsService.GetById(created.Value.Id);
                Assert.Equal("بعد التعديل", updated.Value.Name);
            });
        }

        private static void SetComboSelection(DependencyObject root, string codePrefix)
        {
            var combo = FindVisualChild<AppComboBox>(root);
            var item = ((System.Collections.IEnumerable)combo.ItemsSource).Cast<object>()
                .First(i => i.GetType().GetProperty("Display").GetValue(i).ToString().StartsWith(codePrefix));
            combo.SelectedValue = item.GetType().GetProperty("Id").GetValue(item);
            combo.SelectedItem = item;
        }

        private static void SetTextBoxValue(DependencyObject root, string name)
        {
            var textBox = FindVisualChild<AppTextBox>(root);
            textBox.Text = name;
        }

        private static void ClickButton(DependencyObject root, string text)
        {
            var button = FindAllVisualChildren<AppButton>(root).First(b => b.Text == text);
            var innerButton = FindVisualChild<Button>(button);
            innerButton.RaiseEvent(new RoutedEventArgs(ButtonBase_ClickEvent()));
        }

        private static System.Windows.RoutedEvent ButtonBase_ClickEvent() =>
            System.Windows.Controls.Primitives.ButtonBase.ClickEvent;

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
