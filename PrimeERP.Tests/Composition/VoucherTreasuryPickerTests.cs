using PrimeERP.Application.Services.Ledger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Treasury;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Legacy.Treasury;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>قائمة الخزينة/البنك تتبع طريقة الدفع</summary>
    [Collection("WpfApplication")]
    public class VoucherTreasuryPickerTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public VoucherTreasuryPickerTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void TreasuryPicker_FollowsPaymentMethod()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();

                var accounts = _db.Services.GetRequiredService<IAccountService>();
                var treasuries = _db.Services.GetRequiredService<ITreasuryService>();

                string LeafUnder(string parentCode, string name) =>
                    accounts.Create(new CreateAccountDto
                    { ParentId = accounts.GetByCode(parentCode).Value.Id, Name = name, IsLeaf = true, SkipAutoLink = true }).Value.Code;

                var cash = treasuries.Create(new PrimeERP.Domain.Entities.Treasury
                { Name = "صندوق الاختبار", Kind = PrimeERP.Domain.Enums.TreasuryKind.Cash, AccountCode = LeafUnder("1201", "صندوق"), IsActive = true });
                var bank = treasuries.Create(new PrimeERP.Domain.Entities.Treasury
                { Name = "بنك الاختبار", Kind = PrimeERP.Domain.Enums.TreasuryKind.Bank, AccountCode = LeafUnder("1201", "بنك"), BankName = "بنك مصر", IsActive = true });
                Assert.True(cash.IsSuccess, cash.ErrorMessage);
                Assert.True(bank.IsSuccess, bank.ErrorMessage);

                var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get("Receipts");
                Assert.NotNull(definition.DocumentDialog);

                var page = CrudPageRenderer.Render(definition, _db.Services);
                page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                AppComboBox method = null, treasury = null;
                Exception thrown = null;
                Window window = null;

                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        window = System.Windows.Application.Current.Windows.OfType<Window>().Last();
                        var combos = FindAll<AppComboBox>(window).ToList();
                        method   = combos.First(c => c.Label == "طريقة الدفع");
                        treasury = combos.First(c => c.Label == "الخزينة / البنك");
                    }
                    catch (Exception ex) { thrown = ex; }
                    finally { window?.Close(); }
                }));

                dynamic vm = page.DataContext;
                ((System.Windows.Input.ICommand)vm.AddCommand).Execute(null);

                Assert.True(thrown == null, thrown?.ToString());
                Assert.NotNull(treasury);

                void AssertShows(string present, string absent)
                {
                    Assert.Contains(present, Names(treasury));
                    Assert.DoesNotContain(absent, Names(treasury));
                }

                AssertShows("صندوق الاختبار", "بنك الاختبار");   // نقداً — الافتراضي

                Select(method, (int)PrimeERP.Domain.Enums.PaymentMethod.Bank);
                AssertShows("بنك الاختبار", "صندوق الاختبار");

                Select(method, (int)PrimeERP.Domain.Enums.PaymentMethod.Cash);
                AssertShows("صندوق الاختبار", "بنك الاختبار");
            });
        }

        [Fact]
        public void EveryRegisteredModule_HasATitleInBothStringDictionaries()
        {
            var registry = _db.Services.GetRequiredService<IModuleRegistry>();
            var keys = registry.All().Select(m => m.TitleKey).Where(k => k.StartsWith("Str.")).Distinct().ToList();

            foreach (var file in new[] { "Strings.ar.xaml", "Strings.en.xaml" })
            {
                var path = System.IO.Path.Combine(RepositoryRoot(), "5.Design", "Strings", file);
                var content = System.IO.File.ReadAllText(path);
                var missing = keys.Where(k => !content.Contains($"x:Key=\"{k}\"")).ToList();

                Assert.True(missing.Count == 0, $"{file} ينقصه: " + string.Join(", ", missing));
            }
        }

        private static string RepositoryRoot()
        {
            var directory = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "PrimeERP.csproj")))
                directory = directory.Parent;

            return directory?.FullName ?? throw new InvalidOperationException("تعذّر تحديد جذر المستودع");
        }

        private static IEnumerable<T> FindAll<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                if (child is T typed) yield return typed;

                foreach (var descendant in FindAll<T>(child)) yield return descendant;
            }
        }

        private static string[] Names(AppComboBox combo) =>
            combo.ItemsSource?.Cast<object>().Select(i => i.GetType().GetProperty("Display").GetValue(i)?.ToString()).ToArray()
            ?? Array.Empty<string>();

        private static void Select(AppComboBox combo, int id)
        {
            combo.SelectedItem = combo.ItemsSource.Cast<object>()
                .First(i => (int)i.GetType().GetProperty("Id").GetValue(i) == id);
        }
    }
}
