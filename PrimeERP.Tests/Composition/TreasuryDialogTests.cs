using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Legacy.Treasury;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>إضافة بنك من الشاشة الحقيقية</summary>
    [Collection("WpfApplication")]
    public class TreasuryDialogTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public TreasuryDialogTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void AddingABank_ShowsTheAccountNumberField_AndCreatesTheLeafUnderTheBankRoot()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();

                var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get("Treasuries");
                var page = CrudPageRenderer.Render(definition, _db.Services);
                page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                Exception thrown = null;
                Window window = null;
                bool accountNumberVisibleForCash = true, accountNumberVisibleForBank = false;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        window = System.Windows.Application.Current.Windows.OfType<Window>().Last();

                        var kind = FindAll<AppComboBox>(window).First(c => c.Label == "النوع");
                        var accountNumber = FindAll<AppTextBox>(window).First(c => c.Label == "رقم الحساب بالبنك");
                        var name = FindAll<AppTextBox>(window).First(c => c.Label == "اسم الخزينة / البنك");

                        accountNumberVisibleForCash = accountNumber.Visibility == Visibility.Visible;

                        Select(kind, (int)TreasuryKind.Bank);
                        accountNumberVisibleForBank = accountNumber.Visibility == Visibility.Visible;

                        name.Text = "بنك الإسكندرية";
                        accountNumber.Text = "123-456";

                        ClickButton(window, "Str.Save");
                    }
                    catch (Exception ex) { thrown = ex; window?.Close(); }
                }));

                dynamic vm = page.DataContext;
                ((System.Windows.Input.ICommand)vm.AddCommand).Execute(null);

                Assert.True(thrown == null, thrown?.ToString());
                Assert.False(accountNumberVisibleForCash, "رقم الحساب البنكي ظاهر رغم اختيار خزينة");
                Assert.True(accountNumberVisibleForBank, "رقم الحساب البنكي لم يظهر عند اختيار بنك");

                var created = _db.Services.GetRequiredService<ITreasuryService>().GetAll().Value
                    .SingleOrDefault(t => t.Name == "بنك الإسكندرية");

                Assert.NotNull(created);
                Assert.Equal(TreasuryKind.Bank, created.Kind);
                Assert.StartsWith("1203", created.AccountCode);
            });
        }

        private static void Select(AppComboBox combo, int id) =>
            combo.SelectedItem = combo.ItemsSource.Cast<object>()
                .First(i => (int)i.GetType().GetProperty("Id").GetValue(i) == id);

        private static void ClickButton(DependencyObject root, string labelKey)
        {
            var text = PrimeERP.Platform.Localization.LocalizationService.Get(labelKey);
            var button = FindAll<AppButton>(root).First(b => b.Text == text);
            FindAll<Button>(button).First().RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        }

        private static IEnumerable<T> FindAll<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T typed) yield return typed;

                foreach (var descendant in FindAll<T>(child)) yield return descendant;
            }
        }
    }
}
