using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Parties;
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
    /// <summary>حوار العميل من الشاشة الحقيقية</summary>
    [Collection("WpfApplication")]
    public class CustomerDialogTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public CustomerDialogTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void ShowAndSave_AddMode_CreatesCustomer_WithHiddenAccountLink()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var dialog = registry.Get("Customers").Dialog;
                Assert.NotNull(dialog);
                var toast = _db.Services.GetRequiredService<IToastService>();

                Exception thrown = null;
                Window window = null;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        window = System.Windows.Application.Current.Windows.OfType<Window>().Last();
                        FindVisualChild<AppTextBox>(window).Text = "عميل من الحوار";
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

                var customers = _db.Services.GetRequiredService<ICustomerService>();
                var created = customers.GetPaged(1, 20).Value.Items.Single(c => c.Name == "عميل من الحوار");

                Assert.False(string.IsNullOrWhiteSpace(created.AccountCode));

                var accounts = _db.Services.GetRequiredService<IAccountService>();
                var linkedAccount = accounts.GetByCode(created.AccountCode);
                Assert.True(linkedAccount.IsSuccess, linkedAccount.ErrorMessage);
                Assert.Equal("عميل من الحوار", linkedAccount.Value.Name);
                Assert.True(linkedAccount.Value.IsLeaf);
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
