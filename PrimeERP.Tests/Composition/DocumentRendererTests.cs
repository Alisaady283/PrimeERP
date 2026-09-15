using System;
using PrimeERP.UI.Components.Feedback;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    [Collection("WpfApplication")]
    public class DocumentRendererTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public DocumentRendererTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private (string Cash, string Sales) MakeTwoLeafAccounts()
        {
            var accounts = _db.Services.GetRequiredService<IAccountRepository>();
            var svc = _db.Services.GetRequiredService<IAccountService>();

            var cashParentId = accounts.GetByCode("1204").Id;
            var salesParentId = accounts.GetByCode("41").Id;

            var cash = svc.Create(new CreateAccountDto { ParentId = cashParentId, Name = "صندوق اختباري", IsLeaf = true });
            var sales = svc.Create(new CreateAccountDto { ParentId = salesParentId, Name = "إيراد اختباري", IsLeaf = true });
            Assert.True(cash.IsSuccess, cash.ErrorMessage);
            Assert.True(sales.IsSuccess, sales.ErrorMessage);

            return (cash.Value.Code, sales.Value.Code);
        }

        [Fact]
        public void CrudPageRenderer_AddCommand_OpensDocumentDialog_AndReloadsGridAfterSave()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var (cashCode, salesCode) = MakeTwoLeafAccounts();

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var definition = registry.Get("Journals");
                Assert.NotNull(definition);
                Assert.NotNull(definition.DocumentDialog);

                var element = CrudPageRenderer.Render(definition, _db.Services);
                element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                dynamic vm = element.DataContext;
                Assert.NotNull(vm);

                Exception thrown = null;
                Window window = null;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        window = System.Windows.Application.Current.Windows.OfType<Window>().Last();

                        FindVisualChild<AppDatePicker>(window).SelectedDate = DateTime.Today;
                        FindVisualChild<AppTextBox>(window).Text = "قيد من الصفحة الحقيقية";

                        // المستند يبدأ بسطرٍ واحد، والقيد يحتاج طرفين — فيُضاف الثاني كما يفعل المستخدم.
                        ClickButton(window, "Str.AddLine");

                        var combos = FindAllVisualChildren<AppComboBox>(window).ToList();
                        SelectByCode(combos[0], cashCode);
                        SelectByCode(combos[1], salesCode);

                        var numerics = FindAllVisualChildren<AppNumericBox>(window).ToList();
                        numerics[0].Value = 40m;
                        numerics[3].Value = 40m;

                        ClickButton(window, "Str.Save");
                    }
                    catch (Exception ex) { thrown = ex; window?.Close(); }
                }));

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (_, __) => { timer.Stop(); window?.Close(); };
                timer.Start();

                ((System.Windows.Input.ICommand)vm.AddCommand).Execute(null);

                Assert.Null(thrown);

                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (!((System.Collections.IEnumerable)vm.Items).Cast<object>().Any() && DateTime.UtcNow < deadline)
                {
                    var frame = new DispatcherFrame();
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                    Dispatcher.PushFrame(frame);
                }

                var items = ((System.Collections.IEnumerable)vm.Items).Cast<object>().ToList();
                Assert.Contains(items, i => (string)i.GetType().GetProperty("Description").GetValue(i) == "قيد من الصفحة الحقيقية");
            });
        }

        [Fact]
        public void ShowAndSave_AddMode_CreatesBalancedJournalEntry()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var (cashCode, salesCode) = MakeTwoLeafAccounts();

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var def = registry.Get("Journals").DocumentDialog;
                var toast = _db.Services.GetRequiredService<IToastService>();

                Exception thrown = null;
                Window window = null;
                double windowWidth = 0;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        window = System.Windows.Application.Current.Windows.OfType<Window>().Last();
                        windowWidth = ((AppDialogWindow)window).CardWidth;

                        FindVisualChild<AppDatePicker>(window).SelectedDate = DateTime.Today;
                        FindVisualChild<AppTextBox>(window).Text = "قيد اختباري";

                        // المستند يبدأ بسطرٍ واحد، والقيد يحتاج طرفين — فيُضاف الثاني كما يفعل المستخدم.
                        ClickButton(window, "Str.AddLine");

                        var combos = FindAllVisualChildren<AppComboBox>(window).ToList();
                        SelectByCode(combos[0], cashCode);
                        SelectByCode(combos[1], salesCode);

                        var numerics = FindAllVisualChildren<AppNumericBox>(window).ToList();
                        numerics[0].Value = 100m; // مدين السطر الأول
                        numerics[3].Value = 100m; // دائن السطر الثاني

                        ClickButton(window, "Str.Save");
                    }
                    catch (Exception ex) { thrown = ex; window?.Close(); }
                }));

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (_, __) => { timer.Stop(); window?.Close(); };
                timer.Start();

                bool saved = false;
                try { saved = DocumentRenderer.ShowAndSave(def, _db.Services, toast); }
                catch (Exception ex) { thrown = ex; }

                Assert.Null(thrown);
                Assert.True(saved);
                // إثبات فعلي لإصلاح توقّف 12 (نافذة مقصوصة 420px بغضّ النظر عن عرض صف السطور الحقيقي) —
                // عرض السطور (حساب220+مدين110+دائن110+ملاحظات180=620) + الهوامش يتجاوز 560، فيُتوقَّع Lg=760.
                Assert.Equal(760d, windowWidth);

                var journal = _db.Services.GetRequiredService<IJournalService>();
                var created = journal.GetPaged(1, 20).Value.Items.Single(e => e.Description == "قيد اختباري");
                Assert.Equal(100m, created.TotalDebit);
                Assert.Equal(100m, created.TotalCredit);
                Assert.True(created.IsBalanced);

                var detail = journal.GetById(created.Id).Value;
                Assert.Equal(2, detail.Lines.Count);
                Assert.Contains(detail.Lines, l => l.AccountCode == cashCode && l.Debit == 100m);
                Assert.Contains(detail.Lines, l => l.AccountCode == salesCode && l.Credit == 100m);
            });
        }

        [Fact]
        public void ShowAndSave_EditMode_LoadsExistingLines_AndUpdatesDescription()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var (cashCode, salesCode) = MakeTwoLeafAccounts();

                var journal = _db.Services.GetRequiredService<IJournalService>();
                var created = journal.Create(new CreateJournalDto
                {
                    EntryDate = DateTime.Today,
                    Description = "قبل التعديل",
                    Lines =
                    {
                        new CreateJournalLineDto { AccountCode = cashCode, Debit = 50m },
                        new CreateJournalLineDto { AccountCode = salesCode, Credit = 50m },
                    }
                });
                Assert.True(created.IsSuccess, created.ErrorMessage);

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var def = registry.Get("Journals").DocumentDialog;
                var toast = _db.Services.GetRequiredService<IToastService>();

                Exception thrown = null;
                Window window = null;
                int lineRowsFoundOnOpen = 0;
                string[] accountsShownOnOpen = null;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        window = System.Windows.Application.Current.Windows.OfType<Window>().Last();

                        var combos = FindAllVisualChildren<AppComboBox>(window).ToList();
                        lineRowsFoundOnOpen = combos.Count;

                        // السطر يظهر بحسابه لا فارغاً: عدّ الصفوف وحده لا يكشف قائمةً حُمّلت بلا تحديد.
                        accountsShownOnOpen = combos.Select(c => c.SelectedValue as string).ToArray();

                        FindVisualChild<AppTextBox>(window).Text = "بعد التعديل";
                        ClickButton(window, "Str.Save");
                    }
                    catch (Exception ex) { thrown = ex; window?.Close(); }
                }));

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (_, __) => { timer.Stop(); window?.Close(); };
                timer.Start();

                bool saved = false;
                try { saved = DocumentRenderer.ShowAndSave(def, _db.Services, toast, created.Value); }
                catch (Exception ex) { thrown = ex; }

                Assert.Null(thrown);
                Assert.True(saved);
                Assert.Equal(2, lineRowsFoundOnOpen);
                Assert.Equal(new[] { cashCode, salesCode }, accountsShownOnOpen);

                var updated = journal.GetById(created.Value.Id).Value;
                Assert.Equal("بعد التعديل", updated.Description);
                Assert.Equal(2, updated.Lines.Count);
            });
        }

        private static void SelectByCode(AppComboBox combo, string code)
        {
            var item = ((System.Collections.IEnumerable)combo.ItemsSource).Cast<object>()
                .First(i => (string)i.GetType().GetProperty("Code").GetValue(i) == code);
            combo.SelectedItem = item;
        }

        private static void ClickButton(DependencyObject root, string localizationKey)
        {
            var text = PrimeERP.Platform.Localization.LocalizationService.Get(localizationKey);
            var button = FindAllVisualChildren<AppButton>(root).First(b => b.Text == text);
            var innerButton = FindVisualChild<Button>(button);
            innerButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
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
