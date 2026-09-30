using PrimeERP.Application.Services.Ledger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Legacy.Cheques;
using PrimeERP.Application.Legacy.Parties;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Xunit;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.DTOs.Treasury;
using PrimeERP.Application.Legacy.Treasury;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Tests.Composition
{
    /// <summary>مستند الشيكات دفعةُ إدخال</summary>
    [Collection("WpfApplication")]
    public class ChequeDocumentPartyTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly RecordingToastService _toasts = new();
        private readonly IServiceProvider _services;

        public ChequeDocumentPartyTests()
        {
            AppSession.DevMode = true;
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
        public void Header_CarriesNoParty_AndEachLineKeepsItsOwn()
        {
            var accounts = _services.GetRequiredService<IAccountService>();
            var accountRepo = _services.GetRequiredService<IAccountRepository>();
            var bankAccount = accounts.Create(new CreateAccountDto
            { ParentId = accountRepo.GetByCode("1204").Id, Name = "حساب بنك الشيكات", SkipAutoLink = true }).Value.Code;
            var bank = _services.GetRequiredService<ITreasuryService>().Create(new Treasury
            { Name = "بنك الاختبار", AccountCode = bankAccount, Kind = PrimeERP.Domain.Enums.TreasuryKind.Bank });
            Assert.True(bank.IsSuccess, bank.ErrorMessage);

            var customers = _services.GetRequiredService<ICustomerService>();
            var first = customers.Create(new Customer { Name = "عميل الشيك الأول" }).Value;
            var second = customers.Create(new Customer { Name = "عميل الشيك الثاني" }).Value;

            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_services);
                _services.GetRequiredService<IIdentityService>().Initialize();

                var def = _services.GetRequiredService<IModuleRegistry>().Get("ChequeReceipts").DocumentDialog;

                Assert.DoesNotContain(def.HeaderFields, f => f.Key == nameof(CreateChequeLineDto.PartyId));
                Assert.Contains(def.LineFields, f => f.Key == nameof(CreateChequeLineDto.PartyId) && f.Kind == FieldKind.Picker);

                Exception thrown = null;
                Window window = null;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        window = System.Windows.Application.Current.Windows.OfType<Window>().Last();

                        FindVisualChild<AppDatePicker>(window).SelectedDate = DateTime.Today;

                        var combos = FindAll<AppComboBox>(window).ToList();
                        var texts = FindAll<AppTextBox>(window).ToList();
                        var numbers = FindAll<AppNumericBox>(window).ToList();

                        var bankCombo = combos[0];
                        bankCombo.SelectedItem = ((System.Collections.IEnumerable)bankCombo.ItemsSource).Cast<object>().First();

                        var party = combos[1];
                        Assert.True(party.ItemsSource != null, "قائمة الطرف في السطر فارغة");

                        texts[0].Text = "CH-1";
                        numbers[0].Value = 500m;
                        party.SelectedItem = ((System.Collections.IEnumerable)party.ItemsSource).Cast<object>()
                            .First(i => (int)i.GetType().GetProperty("Id").GetValue(i) == first.Id);

                        ClickButton(window, "Str.Save");
                    }
                    catch (Exception ex) { thrown = ex; window?.Close(); }
                }));

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (_, __) => { timer.Stop(); window?.Close(); };
                timer.Start();

                bool saved;
                try { saved = DocumentRenderer.ShowAndSave(def, _services, _toasts); }
                catch (Exception ex) { thrown = ex; saved = false; }

                Assert.Null(thrown);
                Assert.True(saved, "لم يُحفظ مستند الشيكات: " + string.Join(" / ", _toasts.Errors));
            });

            var cheque = _services.GetRequiredService<IChequeService>()
                .GetPaged(1, 20).Value.Items.Single(c => c.ChequeNo == "CH-1");

            Assert.Equal("عميل الشيك الأول", cheque.PartyName);
            Assert.Equal(first.Id, cheque.PartyId);
            Assert.NotEqual(second.Id, cheque.PartyId);
        }

        private static void ClickButton(DependencyObject root, string localizationKey)
        {
            var text = PrimeERP.Platform.Localization.LocalizationService.Get(localizationKey);
            var button = FindAll<AppButton>(root).First(b => b.Text == text);
            FindVisualChild<Button>(button).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
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

        private static IEnumerable<T> FindAll<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) yield return typed;
                foreach (var nested in FindAll<T>(child)) yield return nested;
            }
        }
    }
}
