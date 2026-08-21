using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PrimeERP.Core;
using PrimeERP.Core.Permissions;
using PrimeERP.Services;
using PrimeERP.ViewModels;
using PrimeERP.Views.Controls.Actions;
using PrimeERP.Views.Controls.Display;
using PrimeERP.Views.Controls.Documents;
using PrimeERP.Views.Controls.Inputs;
using PrimeERP.Views.Controls.Pickers;
using PrimeERP.Views.Controls.Shell;
using PrimeERP.Views.Controls.Tree;

namespace PrimeERP.Views.Dev
{
    /// <summary>
    /// صفحة مرجعية للمطوّرين تعرض كل قطعة واجهة وحالاتها للمراجعة البصرية بعد كل دفعة.
    /// غير مربوطة بأي تنقّل في MainWindow، وبالتالي لا تظهر في نسخة الإنتاج.
    /// </summary>
    public partial class ControlsGalleryPage : UserControl
    {
        private class DemoItem
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public override string ToString() => $"{Code} - {Name}";
        }

        private class DemoAccountRow
        {
            public string  Code    { get; set; }
            public string  Name    { get; set; }
            public string  Type    { get; set; }
            public decimal Balance { get; set; }
        }

        private class DemoNumberSequenceService : INumberSequenceService
        {
            private int _counter = 1;
            public string Next(string key) => $"DOC-{DateTime.Now:yyyy}-{_counter++:D5}";
            public string Next(DbConnection conn, DbTransaction tx, string key) => Next(key);
            public string Peek(string key) => $"DOC-{DateTime.Now:yyyy}-{_counter:D5}";
        }

        private List<DemoAccountRow> _demoRows;
        private List<GridColumn> _demoColumns;
        private readonly INumberSequenceService _numberSequenceService = new DemoNumberSequenceService();

        public ControlsGalleryPage()
        {
            InitializeComponent();
            LoadDemoData();
            BuildPermissionSimulator();
        }

        /// <summary>
        /// يسمح بتعطيل AppSession.DevMode وتبديل صلاحيات فردية يدوياً، لمراجعة PermissionButton/ActionToolbar/
        /// ColumnPermissions بصرياً قبل تفعيل تسجيل الدخول الحقيقي (المرحلة H).
        /// </summary>
        private void BuildPermissionSimulator()
        {
            devModeToggle.IsChecked = AppSession.DevMode;
            devModeToggle.CheckedChanged += (s, e) =>
            {
#if DEBUG
                AppSession.DevMode = devModeToggle.IsChecked;
#endif
                AppSession.RaisePermissionsChanged();
            };

            foreach (var key in PermissionKeys.All().OrderBy(k => k))
            {
                var cb = new AppCheckBox
                {
                    Label = key,
                    IsChecked = AppSession.Permissions.Contains(key),
                    Margin = new Thickness(0, 0, 18, 10)
                };

                cb.CheckedChanged += (s, e) =>
                {
                    if (cb.IsChecked) AppSession.Permissions.Add(key);
                    else AppSession.Permissions.Remove(key);
                    AppSession.RaisePermissionsChanged();
                };

                permissionsWrap.Children.Add(cb);
            }
        }

        private void LoadDemoData()
        {
            // ===== Batch 1 demo data =====
            var accounts = new List<DemoItem>
            {
                new() { Code = "1220", Name = "ذمم مدينة (العملاء)" },
                new() { Code = "1230", Name = "البنوك" },
                new() { Code = "1240", Name = "الصناديق" },
                new() { Code = "2110", Name = "ذمم دائنة (الموردون)" },
                new() { Code = "4100", Name = "إيرادات المبيعات" },
            };

            cmbSearchable.ItemsSource = accounts;
            cmbSearchable.DisplayMemberPath = nameof(DemoItem.Name);
            cmbSearchable.SelectedValuePath = nameof(DemoItem.Code);

            cmbPlain.ItemsSource = new List<string> { "أصول", "خصوم", "حقوق ملكية", "إيرادات", "مصروفات" };
            cmbError.ItemsSource = new List<string> { "ج.م", "دولار", "يورو" };
            cmbDisabled.ItemsSource = new List<string> { "الفرع الرئيسي", "فرع الإسكندرية" };
            cmbDisabled.SelectedItem = "الفرع الرئيسي";

            // ===== Batch 2 demo data =====
            _demoRows = new List<DemoAccountRow>
            {
                new() { Code = "1220", Name = "ذمم مدينة (العملاء)", Type = "أصول",  Balance = 45200.50m },
                new() { Code = "1230", Name = "البنوك",               Type = "أصول",  Balance = 128900.00m },
                new() { Code = "1240", Name = "الصناديق",             Type = "أصول",  Balance = 6200.75m },
                new() { Code = "2110", Name = "ذمم دائنة (الموردون)", Type = "خصوم",  Balance = 31000.00m },
                new() { Code = "4100", Name = "إيرادات المبيعات",     Type = "إيرادات", Balance = 210500.00m },
                new() { Code = "5100", Name = "مصروفات تشغيلية",     Type = "مصروفات", Balance = 18750.25m },
            };

            _demoColumns = new List<GridColumn>
            {
                new() { Header = "الكود",   Binding = nameof(DemoAccountRow.Code),   Width = 90,  Align = ColumnAlign.Center },
                new() { Header = "الاسم",   Binding = nameof(DemoAccountRow.Name),   Width = 240, IsStarWidth = true },
                new() { Header = "النوع",   Binding = nameof(DemoAccountRow.Type),   Width = 110, Align = ColumnAlign.Center },
                new() { Header = "الرصيد",  Binding = nameof(DemoAccountRow.Balance), Width = 130, Align = ColumnAlign.Center,
                        Format = "N2", Footer = FooterAggregate.Sum },
            };

            gridDemo.ColumnsSource = _demoColumns;
            gridDemo.ItemsSource = _demoRows;
            gridDemo.ShowRowNumbers = true;
            gridDemo.AllowExport = true;
            gridDemo.ShowRowActions = true;
            gridDemo.EmptyMessage = "لا توجد حسابات مطابقة";
            gridDemo.ExportRequested += GridDemo_ExportRequested;
            gridDemo.RowEditRequested += (s, item) => ToastService.Instance.Info($"تعديل: {((DemoAccountRow)item).Name}");
            gridDemo.RowDeleteRequested += (s, item) => ToastService.Instance.Warning($"حذف: {((DemoAccountRow)item).Name}");

            gridEmpty.ColumnsSource = _demoColumns;
            gridEmpty.ItemsSource = new List<DemoAccountRow>();
            gridEmpty.EmptyMessage = "لا توجد بيانات لعرضها";

            gridLoading.ColumnsSource = _demoColumns;
            gridLoading.ItemsSource = _demoRows;
            gridLoading.IsLoading = true;

            // AppTreeView demo — شجرة حسابات حقيقية عبر TreeNodeViewModel
            TreeNodeViewModel Node(string name) => new() { Name = name, DisplayText = name };

            var assets = Node("أصول");
            var currentAssets = Node("أصول متداولة");
            currentAssets.AddChild(Node("البنوك"));
            currentAssets.AddChild(Node("الصناديق"));
            assets.AddChild(currentAssets);
            assets.AddChild(Node("أصول ثابتة"));

            var liabilities = Node("خصوم");
            liabilities.AddChild(Node("ذمم دائنة (الموردون)"));

            treeDemo.ItemsSource = new List<TreeNodeViewModel> { assets, liabilities };
            treeSearchDemo.Search += (s, term) => { treeDemo.SearchText = term; };

            // AppTabControl demo
            tabsDemo.Tabs = new List<AppTabItem>
            {
                new() { Header = "البيانات الأساسية", Content = BuildTabContent("بيانات الحساب الأساسية هنا") },
                new() { Header = "الحركات",           Content = BuildTabContent("قائمة الحركات المالية هنا") },
                new() { Header = "المرفقات",          Content = BuildTabContent("الملفات المرفقة هنا") },
            };

            // AppBreadcrumb demo
            breadcrumbDemo.Items = new List<string> { "الرئيسية", "الحسابات", "شجرة الحسابات" };

            // AppEmptyState demo action
            emptyStateDemo.ActionCommand = new RelayCommand(
                () => ToastService.Instance.Info("سيتم فتح نموذج الإضافة هنا"));

            // ===== Batch 3 demo data =====
            var toolbarActions = new List<ToolbarAction>
            {
                ToolbarAction.New(new RelayCommand(() => ToastService.Instance.Success("تم الضغط على: جديد"))),
                ToolbarAction.Edit(new RelayCommand(() => ToastService.Instance.Info("تم الضغط على: تعديل"))),
                ToolbarAction.Delete(new RelayCommand(() => ToastService.Instance.Warning("تم الضغط على: حذف"))),
                ToolbarAction.SeparatorItem(),
                ToolbarAction.Print(new RelayCommand(() => ToastService.Instance.Info("تم الضغط على: طباعة"))),
                ToolbarAction.Export(new RelayCommand(() => ToastService.Instance.Info("تم الضغط على: تصدير"))),
                ToolbarAction.Refresh(new RelayCommand(() => ToastService.Instance.Success("تم التحديث"))),
                ToolbarAction.Post(new RelayCommand(() => ToastService.Instance.Success("تم الترحيل"))),
            };

            toolbarWide.ButtonsSource = toolbarActions;
            toolbarNarrow.ButtonsSource = toolbarActions;

            dropdownDemo.Items = new List<string> { "تصدير Excel", "تصدير CSV", "تصدير PDF" };
            dropdownDemo.ItemSelected += (s, item) => ToastService.Instance.Info($"تم اختيار: {item}");

            // ===== الدفعة 4 / الخطوة 3: الـ Pickers الخمسة (Mock DataSources) =====
            accountPickerDemo.DataSource = MockPickerDataSources.Accounts;
            accountPickerDemo.LeafOnly = true;
            accountPickerDemo.SelectionChanged += (s, acc) =>
                ToastService.Instance.Info(acc != null ? $"تم اختيار الحساب: {acc.Code} - {acc.Name}" : "تم مسح الاختيار");

            customerPickerDemo.DataSource = MockPickerDataSources.Customers;
            customerPickerDemo.SelectionChanged += (s, c) =>
                ToastService.Instance.Info(c != null ? $"تم اختيار العميل: {c.Name}" : "تم مسح الاختيار");
            customerPickerDemo.QuickAddRequested += (s, e) =>
                ToastService.Instance.Info("سيتم فتح نموذج إضافة عميل سريع هنا (يُبنى في المرحلة H)");

            supplierPickerDemo.DataSource = MockPickerDataSources.Suppliers;
            supplierPickerDemo.SelectionChanged += (s, sup) =>
                ToastService.Instance.Info(sup != null ? $"تم اختيار المورد: {sup.Name}" : "تم مسح الاختيار");

            productPickerDemo.DataSource = MockPickerDataSources.Products;
            productPickerDemo.SelectionChanged += (s, p) =>
                ToastService.Instance.Info(p != null ? $"تم اختيار الصنف: {p.Name} (الرصيد: {p.CurrentStock:N2})" : "تم مسح الاختيار");
            productPickerDemo.QuickAddRequested += (s, e) =>
                ToastService.Instance.Info("سيتم فتح نموذج إضافة صنف سريع هنا (يُبنى في المرحلة H)");

            employeePickerDemo.DataSource = MockPickerDataSources.Employees;
            employeePickerDemo.ActiveOnly = true;
            employeePickerDemo.SelectionChanged += (s, emp) =>
                ToastService.Instance.Info(emp != null ? $"تم اختيار الموظف: {emp.Name}" : "تم مسح الاختيار");

            // ===== الجزء 4.2/4.3: DocumentLinesGrid — الأنماط الأربعة، 8 أسطر افتراضية، Mock DataSources للأنواع الخمسة =====
            void WirePickerSources(DocumentLinesGrid g)
            {
                g.AccountDataSource  = MockPickerDataSources.Accounts;
                g.CustomerDataSource = MockPickerDataSources.Customers;
                g.SupplierDataSource = MockPickerDataSources.Suppliers;
                g.ProductDataSource  = MockPickerDataSources.Products;
                g.EmployeeDataSource = MockPickerDataSources.Employees;
            }

            linesGridJournal.ColumnsDefinition = LineColumnPresets.Journal();
            linesGridJournal.Mode = DocumentLinesMode.Journal;
            linesGridJournal.DefaultLinesCount = 8;
            WirePickerSources(linesGridJournal);

            linesGridSales.ColumnsDefinition = LineColumnPresets.SalesInvoice();
            linesGridSales.Mode = DocumentLinesMode.SalesInvoice;
            linesGridSales.DefaultLinesCount = 8;
            WirePickerSources(linesGridSales);

            linesGridPurchase.ColumnsDefinition = LineColumnPresets.PurchaseInvoice();
            linesGridPurchase.Mode = DocumentLinesMode.PurchaseInvoice;
            linesGridPurchase.DefaultLinesCount = 8;
            WirePickerSources(linesGridPurchase);

            linesGridStock.ColumnsDefinition = LineColumnPresets.StockVoucher();
            linesGridStock.Mode = DocumentLinesMode.Stock;
            linesGridStock.DefaultLinesCount = 8;
            WirePickerSources(linesGridStock);

            // ===== الجزء 4.4: DocumentHeader + DocumentFooter — مستند كامل مربوط فعلياً بالأنماط الأربعة =====
            void WireDocument(DocumentHeader header, DocumentLinesGrid linesGrid, DocumentFooter footer,
                string[] types, Func<List<FooterTotal>> computeTotals, Action<DocumentFooter> updateStatus = null)
            {
                header.DocumentTypes = types;
                header.DocumentType = types.FirstOrDefault();
                header.DocumentDate = DateTime.Today;
                header.Status = DocumentStatus.Draft;
                header.NumberSequenceService = _numberSequenceService;
                header.DocumentNoRequested += (s, e) => ToastService.Instance.Info("طُلب رقم مستند جديد من الخدمة");

                void Recompute()
                {
                    footer.UpdateTotals(computeTotals());
                    updateStatus?.Invoke(footer);
                }
                linesGrid.LineChanged += (s, e) => Recompute();
                linesGrid.TotalsChanged += (s, e) => Recompute();
                Recompute();
            }

            WireDocument(headerJournal, linesGridJournal, footerJournal,
                new[] { "قيد يومية عام", "قيد افتتاحي", "قيد تسوية" },
                () => FooterPresets.Journal(
                    linesGridJournal.Lines?.Sum(l => l.Debit) ?? 0,
                    linesGridJournal.Lines?.Sum(l => l.Credit) ?? 0),
                footer =>
                {
                    var difference = (linesGridJournal.Lines?.Sum(l => l.Debit) ?? 0) - (linesGridJournal.Lines?.Sum(l => l.Credit) ?? 0);
                    footer.StatusVariant = difference == 0 ? "success" : "danger";
                    footer.StatusMessage = difference == 0 ? "القيد متوازن" : $"غير متوازن بفرق {difference:N2}";
                });

            WireDocument(headerSales, linesGridSales, footerSales,
                new[] { "فاتورة مبيعات", "مرتجع مبيعات" },
                () => ComputeInvoiceTotals(linesGridSales));

            WireDocument(headerPurchase, linesGridPurchase, footerPurchase,
                new[] { "فاتورة مشتريات", "مرتجع مشتريات" },
                () => ComputeInvoiceTotals(linesGridPurchase));

            WireDocument(headerStock, linesGridStock, footerStock,
                new[] { "إذن إضافة", "إذن صرف", "تحويل مخزني" },
                () => FooterPresets.Stock(
                    linesGridStock.Lines?.Sum(l => l.Qty) ?? 0,
                    linesGridStock.Lines?.Sum(l => l.LineTotal) ?? 0));

            // ===== الدفعة 5: AppShell — NavItems وهمية لكل موديولات النظام + صفحات وهمية للتبديل =====
            shellDemo.NavItems = BuildShellNavItems();
            shellDemo.CompanyName = "شركة برايم للتجارة";
            shellDemo.UserName = "علي سعيد";
            shellDemo.UserRole = "مدير النظام";
            shellDemo.SidebarLogoContent = new TextBlock
            {
                Text = "PrimeERP", FontSize = 18, FontWeight = FontWeights.Bold,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), Foreground = System.Windows.Media.Brushes.White
            };
            shellDemo.TopBarActionsContent = null;
            shellDemo.ThemeToggled += (s, e) => ThemeService.Toggle();
            shellDemo.LanguageToggled += (s, e) => LocalizationService.Toggle();
            shellDemo.ProfileClicked += (s, e) => ToastService.Instance.Info("سيتم فتح صفحة الملف الشخصي هنا");
            shellDemo.PasswordChangeRequested += (s, e) => ToastService.Instance.Info("سيتم فتح نافذة تغيير كلمة المرور هنا");
            shellDemo.NotificationsClicked += (s, e) => ToastService.Instance.Info("سيتم فتح قائمة الإشعارات هنا");
            shellDemo.LogoutRequested += (s, e) => ToastService.Instance.Warning("سيتم تسجيل الخروج هنا");
            shellDemo.SelectedKey = "dashboard";
            shellDemo.CurrentPage = BuildShellMockPage("لوحة التحكم");
        }

        private static List<NavItem> BuildShellNavItems() => new()
        {
            new() { Key = "dashboard", Text = "لوحة التحكم",   IconKey = "IconDashboard" },
            new() { Key = "accounts",  Text = "شجرة الحسابات", IconKey = "IconAccounts", PermissionKey = PermissionKeys.Accounts.View },
            new() { Key = "journal",   Text = "قيود اليومية",  IconKey = "IconJournal",  PermissionKey = PermissionKeys.Journal.View },
            new() { IsSeparator = true },
            new() { Key = "customers", Text = "العملاء",  IconKey = "IconCustomers", PermissionKey = PermissionKeys.Customers.View },
            new() { Key = "suppliers", Text = "الموردين", IconKey = "IconSuppliers", PermissionKey = PermissionKeys.Suppliers.View },
            new() { Key = "products",  Text = "المنتجات", IconKey = "IconProducts",  PermissionKey = PermissionKeys.Products.View },
            new()
            {
                Key = "warehouse", Text = "المخزون", IconKey = "IconWarehouse", PermissionKey = PermissionKeys.Inventory.View,
                Children = new()
                {
                    new() { Key = "stockIn",   Text = "إذن إضافة",   PermissionKey = PermissionKeys.Inventory.StockIn },
                    new() { Key = "stockOut",  Text = "إذن صرف",     PermissionKey = PermissionKeys.Inventory.StockOut },
                    new() { Key = "transfer",  Text = "تحويل مخزني", PermissionKey = PermissionKeys.Inventory.Transfer },
                }
            },
            new()
            {
                Key = "sales", Text = "المبيعات", IconKey = "IconSales", PermissionKey = PermissionKeys.Sales.View,
                Children = new()
                {
                    new() { Key = "salesInvoice", Text = "فاتورة مبيعات" },
                    new() { Key = "salesReturn",  Text = "مرتجع مبيعات" },
                }
            },
            new()
            {
                Key = "purchases", Text = "المشتريات", IconKey = "IconPurchases", PermissionKey = PermissionKeys.Purchases.View,
                Children = new()
                {
                    new() { Key = "purchaseInvoice", Text = "فاتورة مشتريات" },
                    new() { Key = "purchaseReturn",  Text = "مرتجع مشتريات" },
                }
            },
            new() { IsSeparator = true },
            new() { Key = "hr",       Text = "الموارد البشرية", IconKey = "IconHR",       PermissionKey = PermissionKeys.HR.View },
            new() { Key = "reports",  Text = "التقارير",        IconKey = "IconReports",  PermissionKey = PermissionKeys.Reports.View, Badge = "3" },
            new() { Key = "settings", Text = "الإعدادات",       IconKey = "IconSettings", PermissionKey = PermissionKeys.Settings.View },
        };

        private void shellDemo_NavigationRequested(object sender, string key)
        {
            var title = FindNavText(shellDemo.NavItems, key) ?? key;
            shellDemo.CurrentPage = BuildShellMockPage(title);
        }

        private static string FindNavText(List<NavItem> items, string key)
        {
            if (items == null) return null;
            foreach (var item in items)
            {
                if (item.Key == key) return item.Text;
                var found = FindNavText(item.Children, key);
                if (found != null) return found;
            }
            return null;
        }

        private static FrameworkElement BuildShellMockPage(string title) => new Grid
        {
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

        private static List<FooterTotal> ComputeInvoiceTotals(DocumentLinesGrid grid)
        {
            var lines = grid.Lines ?? new ObservableCollection<DocumentLine>();
            decimal subtotal = 0, discount = 0, tax = 0, net = 0;

            foreach (var line in lines.Where(l => !l.IsEmpty))
            {
                subtotal += line.Qty * line.Price;
                discount += line.DiscountAmount;
                tax += line.TaxAmount;
                net += line.LineTotal;
            }

            return FooterPresets.Invoice(subtotal, discount, tax, net);
        }

        private void GridDemo_ExportRequested(object sender, EventArgs e)
        {
            try
            {
                var folder = Path.Combine(Path.GetTempPath(), "PrimeERP_Gallery_Export");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, $"accounts_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                ExportService.Instance.ExportToCsv(_demoRows, _demoColumns, path);

                ToastService.Instance.Success($"تم التصدير إلى: {path}", 6000);
            }
            catch (Exception ex)
            {
                ToastService.Instance.Error($"فشل التصدير: {ex.Message}");
            }
        }

        private static TextBlock BuildTabContent(string text) => new()
        {
            Text = text,
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 13
        };

        private void btnToggleTheme_Click(object sender, RoutedEventArgs e) => ThemeService.Toggle();

        private void btnToggleLanguage_Click(object sender, RoutedEventArgs e) => LocalizationService.Toggle();

        private void btnToggleLoadingOverlay_Click(object sender, RoutedEventArgs e) =>
            loadingOverlayDemo.IsBusy = !loadingOverlayDemo.IsBusy;

        // ===== Batch 3: Toast triggers =====
        private void btnToastSuccess_Click(object sender, RoutedEventArgs e) => ToastService.Instance.Success("تم الحفظ بنجاح");
        private void btnToastError_Click(object sender, RoutedEventArgs e) => ToastService.Instance.Error("فشل الاتصال بقاعدة البيانات");
        private void btnToastWarning_Click(object sender, RoutedEventArgs e) => ToastService.Instance.Warning("الرصيد أقل من الحد الأدنى");
        private void btnToastInfo_Click(object sender, RoutedEventArgs e) => ToastService.Instance.Info("تم تحديث البيانات");

        // ===== Batch 3: Dialog triggers =====
        private async void btnConfirmDangerous_Click(object sender, RoutedEventArgs e)
        {
            var ok = await DialogService.Instance.ConfirmAsync("حذف الحساب", "هل أنت متأكد من حذف هذا الحساب؟ لا يمكن التراجع.",
                "حذف", isDangerous: true);
            ToastService.Instance.Info(ok ? "تم التأكيد" : "تم الإلغاء");
        }

        private async void btnShowMessage_Click(object sender, RoutedEventArgs e) =>
            await DialogService.Instance.ShowMessageAsync("نجاح", "تم حفظ البيانات بنجاح.", Core.Common.StatusVariant.Success);

        private async void btnShowError_Click(object sender, RoutedEventArgs e) =>
            await DialogService.Instance.ShowErrorAsync("خطأ", "تعذّر الاتصال بالخادم.", new InvalidOperationException("Connection timeout"));

        private async void btnShowProgress_Click(object sender, RoutedEventArgs e)
        {
            var handle = DialogService.Instance.ShowProgress("جارٍ التصدير", "يتم تجهيز الملف...", allowCancel: true);
            handle.CancelRequested += (s, args) => handle.Close();

            for (int i = 0; i <= 100; i += 10)
            {
                await System.Threading.Tasks.Task.Delay(150);
                handle.Report(i, $"جارٍ التجهيز... {i}%");
            }

            handle.Close();
            ToastService.Instance.Success("اكتمل التصدير");
        }
    }
}
