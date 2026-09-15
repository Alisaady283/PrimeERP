using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Sales;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    // يثبت أن ReportRenderer (النمط 4، مبني حديثاً) يعمل فعلياً عبر PageRenderer العام — يزرع فاتورة بيع
    // حقيقية (تُنشئ رصيداً للعميل عبر القيد المُرحَّل)، يُصيِّر تقرير أرصدة العملاء (بلا معايير)، ويتحقق أن
    // Loaded يُشغِّل التقرير تلقائياً وأن النتيجة تعرض العميل بالرصيد الصحيح.
    [Collection("WpfApplication")]
    public class ReportRendererTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        public ReportRendererTests() => AppSession.DevMode = true;
        public void Dispose() => _db.Dispose();

        [Fact]
        public void CustomerBalancesReport_RunsOnLoad_ShowsCustomerWithBalance()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var accounts = _db.Services.GetRequiredService<IAccountService>();
                var settings = _db.Services.GetRequiredService<ISettingsService>();

                string LeafUnder(string parentCode, string name)
                {
                    var parent = accounts.GetByCode(parentCode).Value;
                    return accounts.Create(new CreateAccountDto { ParentId = parent.Id, Name = name, IsLeaf = true, SkipAutoLink = true }).Value.Code;
                }

                settings.Set(SettingKeys.Accounts.Sales, LeafUnder("41", "مبيعات"));
                settings.Set(SettingKeys.Accounts.COGS, LeafUnder("51", "تكلفة"));
                settings.Set(SettingKeys.Accounts.Inventory, LeafUnder("1201", "مخزون"));

                var categories = _db.Services.GetRequiredService<ICategoryService>();
                var category = categories.Create(new CreateCategoryDto { Name = "فئة", ModuleKey = "Products" }).Value;
                var products = _db.Services.GetRequiredService<IProductService>();
                var product = products.Create(new CreateProductDto { Name = "صنف", CategoryId = category.Id, CostPrice = 10, SalePrice = 25 }).Value;

                var warehouses = _db.Services.GetRequiredService<IWarehouseService>();
                var warehouseId = warehouses.Create(new CreateWarehouseDto { Name = "مخزن" }).Value.Id;

                var customers = _db.Services.GetRequiredService<ICustomerService>();
                var customer = customers.Create(new CreateCustomerDto { Name = "عميل التقرير" }).Value;

                var stock = _db.Services.GetRequiredService<IStockService>();
                Data.Core.DbHelper.RunTransaction((conn, tx) =>
                    stock.RecordMovement(conn, tx, product.Id, warehouseId, Domain.Enums.MovementType.In, 50, 10, "Seed", null, "SEED"));

                var invoices = _db.Services.GetRequiredService<ISalesInvoiceService>();
                var invoiceResult = invoices.Create(new CreateSalesInvoiceDto
                {
                    InvoiceDate = DateTime.Today, CustomerId = customer.Id, WarehouseId = warehouseId,
                    Lines = { new CreateSalesInvoiceLineDto { LineNo = 1, ProductCode = product.Code, Qty = 5, UnitPrice = 25 } }
                });
                Assert.True(invoiceResult.IsSuccess, invoiceResult.ErrorMessage);

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var definition = registry.Get("CustomerBalances");

                var element = PageRenderer.Render(definition, _db.Services);
                element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var grid = FindVisualChild<AppDataGrid>(element);
                Assert.NotNull(grid);

                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (grid.ItemsSource == null && DateTime.UtcNow < deadline)
                {
                    var frame = new DispatcherFrame();
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                    Dispatcher.PushFrame(frame);
                }

                var rows = ((IEnumerable)grid.ItemsSource).Cast<object>().ToList();
                Assert.Single(rows);

                // الشكل الرسمي: أول المدة ثم حركتا الفترة ثم آخر المدة — لا عمود رصيد واحد.
                decimal Value(string name) => (decimal)rows[0].GetType().GetProperty(name).GetValue(rows[0]);

                Assert.Equal(125, Value("Charged"));
                Assert.Equal(0, Value("Settled"));
                Assert.Equal(125, Value("Closing"));
            });
        }

        [Fact]
        public void AccountStatementReport_ForPostedEntry_ShowsOpeningRowAndPostedLine()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var accounts = _db.Services.GetRequiredService<IAccountService>();

                AccountDto LeafUnder(string parentCode, string name)
                {
                    var parent = accounts.GetByCode(parentCode).Value;
                    return accounts.Create(new CreateAccountDto { ParentId = parent.Id, Name = name, IsLeaf = true, SkipAutoLink = true }).Value;
                }

                var accountA = LeafUnder("41", "حساب أ");
                var accountB = LeafUnder("51", "حساب ب");

                var journal = _db.Services.GetRequiredService<IJournalService>();
                var createResult = journal.Create(new CreateJournalDto
                {
                    EntryDate = DateTime.Today,
                    Description = "قيد اختبار",
                    Lines =
                    {
                        new CreateJournalLineDto { LineNo = 1, AccountCode = accountA.Code, Debit = 500, Credit = 0 },
                        new CreateJournalLineDto { LineNo = 2, AccountCode = accountB.Code, Debit = 0, Credit = 500 },
                    }
                });
                Assert.True(createResult.IsSuccess, createResult.ErrorMessage);

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var definition = registry.Get("AccountStatement");

                var element = PageRenderer.Render(definition, _db.Services);

                var accountField = FindVisualChild<PrimeERP.UI.Components.Inputs.AppComboBox>(element);
                accountField.SelectedValue = accountA.Id;

                var window = new Window { Content = element, Width = 1200, Height = 800, ShowInTaskbar = false, WindowStyle = WindowStyle.None, ShowActivated = false };
                window.Show();
                window.UpdateLayout();

                var runButton = FindAllVisualChildren<PrimeERP.UI.Components.Actions.AppButton>(element)
                    .First(b => b.Text == PrimeERP.Platform.Localization.LocalizationService.Get("Str.Report.Run"));
                var innerButton = FindVisualChild<System.Windows.Controls.Button>(runButton);
                innerButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

                var grid = FindVisualChild<AppDataGrid>(element);
                var rows = ((IEnumerable)grid.ItemsSource).Cast<object>().ToList();

                // رصيد افتتاحي + السطر المُرحَّل + رصيد آخر المدة
                Assert.Equal(3, rows.Count);

                object Cell(int row, string name) => rows[row].GetType().GetProperty(name).GetValue(rows[row]);

                Assert.Equal(500m, (decimal)Cell(1, "Debit"));
                Assert.Equal("رصيد آخر المدة", (string)Cell(2, "Description"));
                Assert.Equal((decimal)Cell(1, "RunningBalance"), (decimal)Cell(2, "RunningBalance"));
                window.Close();
            });
        }

        private static System.Collections.Generic.List<T> FindAllVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var results = new System.Collections.Generic.List<T>();
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) results.Add(typed);
                results.AddRange(FindAllVisualChildren<T>(child));
            }
            return results;
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
