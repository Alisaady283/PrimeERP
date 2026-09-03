using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Backup;
using PrimeERP.Composition.Definitions;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    // صفحة إعدادات واحدة بسبعة تبويبات، مبنية من SettingKeys.All() عبر DialogRenderer.BuildField.
    public static class SettingsPageRenderer
    {
        private static readonly (string Category, string TitleKey)[] CategoryOrder =
        {
            ("Company", "Str.Settings.Company"), ("Financial", "Str.Settings.Financial"), ("Accounts", "Str.Settings.Accounts"),
            ("Documents", "Str.Settings.Documents"), ("UI", "Str.Settings.UI"), ("Backup", "Str.Settings.Backup"), ("Security", "Str.Settings.Security"),
        };

        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services)
        {
            var settingsService = services.GetRequiredService<ISettingsService>();
            var toast = services.GetRequiredService<IToastService>();
            var allDefs = SettingKeys.All();

            var header = new PageHeader();
            var controls = new Dictionary<string, (FieldDefinition Field, FrameworkElement Control)>();

            var tabs = CategoryOrder.Select(cat =>
            {
                var panel = new WrapPanel { Margin = new Thickness(24) };
                foreach (var def in allDefs.Where(d => d.Category == cat.Category))
                {
                    var isAccount = cat.Category == "Accounts";
                    var field = new FieldDefinition
                    {
                        // LabelKey هنا نص العرض مباشرة — Get تُعيد المفتاح نفسه لو غير موجود بالقاموس.
                        Key = def.Key, LabelKey = LabelFor(def.Key),
                        Kind = isAccount ? FieldKind.Picker : def.DataType switch { "bool" => FieldKind.Check, "int" => FieldKind.Number, _ => FieldKind.Text },
                        PickerType = isAccount ? "Account" : null,
                        PickerValueField = isAccount ? "Code" : "Id",
                    };

                    var control = DialogRenderer.BuildField(field);
                    if (isAccount) DialogRenderer.LoadPickerItems((AppComboBox)control, field, services);
                    control.Width = 260;
                    control.Margin = new Thickness(0, 0, 16, 16);

                    var currentValue = settingsService.Get<string>(def.Key, def.DefaultValue);
                    object typedValue = field.Kind switch
                    {
                        FieldKind.Check => bool.TryParse(currentValue, out var b) && b,
                        FieldKind.Number => decimal.TryParse(currentValue, out var n) ? n : 0,
                        FieldKind.Picker => currentValue,
                        _ => currentValue
                    };
                    if (field.Kind == FieldKind.Picker) DialogRenderer.SelectPickerItem((AppComboBox)control, typedValue, "Code");
                    else DialogRenderer.SetControlValue(control, field, typedValue);

                    controls[def.Key] = (field, control);
                    panel.Children.Add(control);
                }

                if (cat.Category == "Backup") panel.Children.Add(BuildBackupPanel(services, toast));

                return new AppTabItem { Header = LocalizationService.Get(cat.TitleKey), Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } };
            }).ToList();

            var tabControl = new AppTabControl { Tabs = tabs };

            var saveButton = new Btn { Text = LocalizationService.Get("Str.Save"), Variant = "primary", Size = "sm", Margin = new Thickness(24) };
            saveButton.Click += (_, __) =>
            {
                var values = new Dictionary<string, object>();
                foreach (var (key, (field, control)) in controls)
                    values[key] = DialogRenderer.GetControlValue(control, field.Kind);

                var result = settingsService.SetMany(values);
                if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return; }
                toast.Success(LocalizationService.Get("Str.Success"));
            };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(header, 0);
            Grid.SetRow(tabControl, 1);
            Grid.SetRow(saveButton, 2);
            root.Children.Add(header);
            root.Children.Add(tabControl);
            root.Children.Add(saveButton);

            return root;
        }

        private static FrameworkElement BuildBackupPanel(IServiceProvider services, IToastService toast)
        {
            var backup = services.GetRequiredService<IBackupService>();
            var dialogs = services.GetRequiredService<IDialogService>();

            var panel = new StackPanel { Width = 500 };
            var createBtn = new Btn { Text = LocalizationService.Get("Str.Backup.CreateNow"), Variant = "primary", Size = "sm", Margin = new Thickness(0, 0, 0, 12) };
            var listPanel = new StackPanel();

            void RefreshList()
            {
                listPanel.Children.Clear();
                foreach (var b in backup.List())
                {
                    var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var text = new TextBlock { Text = $"{System.IO.Path.GetFileName(b.FilePath)} — {b.CreatedAt:yyyy-MM-dd HH:mm}", VerticalAlignment = VerticalAlignment.Center };
                    var restoreBtn = new Btn { Text = LocalizationService.Get("Str.Backup.Restore"), Variant = "secondary", Size = "sm" };
                    restoreBtn.Click += async (_, __) =>
                    {
                        var confirmed = await dialogs.ConfirmAsync(LocalizationService.Get("Str.Backup.Restore"), LocalizationService.Get("Str.Backup.RestoreConfirm"), isDangerous: true);
                        if (!confirmed) return;

                        var result = backup.Restore(b.FilePath);
                        if (!result.IsSuccess) toast.Error(result.ErrorMessage);
                        else toast.Success(LocalizationService.Get("Str.Success"));
                    };

                    Grid.SetColumn(text, 0);
                    Grid.SetColumn(restoreBtn, 1);
                    row.Children.Add(text);
                    row.Children.Add(restoreBtn);
                    listPanel.Children.Add(row);
                }
            }

            createBtn.Click += (_, __) =>
            {
                var result = backup.Create();
                if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return; }
                toast.Success(LocalizationService.Get("Str.Success"));
                RefreshList();
            };

            RefreshList();
            panel.Children.Add(createBtn);
            panel.Children.Add(listPanel);
            return panel;
        }

        private static readonly Dictionary<string, string> Labels = new()
        {
            [SettingKeys.Company.Name] = "اسم الشركة", [SettingKeys.Company.NameEn] = "اسم الشركة (إنجليزي)",
            [SettingKeys.Company.TaxNumber] = "الرقم الضريبي", [SettingKeys.Company.CommercialRegNo] = "السجل التجاري",
            [SettingKeys.Company.Address] = "العنوان", [SettingKeys.Company.Phone] = "الهاتف", [SettingKeys.Company.Email] = "البريد الإلكتروني",
            [SettingKeys.Company.LogoPath] = "مسار الشعار",

            [SettingKeys.Financial.BaseCurrencyId] = "العملة الأساسية (معرّف)", [SettingKeys.Financial.DecimalPlaces] = "عدد الخانات العشرية",
            [SettingKeys.Financial.FiscalYearStartMonth] = "شهر بداية السنة المالية", [SettingKeys.Financial.AllowNegativeStock] = "السماح برصيد مخزون سالب",
            [SettingKeys.Financial.DefaultCostMethod] = "طريقة التكلفة الافتراضية", [SettingKeys.Financial.RoundingMethod] = "طريقة التقريب",
            [SettingKeys.Financial.RequireFiscalPeriod] = "إلزام وجود فترة مالية", [SettingKeys.Financial.AllowDuplicateAccountInEntry] = "السماح بتكرار الحساب بالقيد",
            [SettingKeys.Financial.WarnOnDuplicateCustomerName] = "تحذير عند تكرار اسم عميل", [SettingKeys.Financial.WarnOnDuplicatePhone] = "تحذير عند تكرار الهاتف",

            [SettingKeys.Accounts.Customers] = "حساب العملاء", [SettingKeys.Accounts.Suppliers] = "حساب الموردين",
            [SettingKeys.Accounts.Inventory] = "حساب المخزون", [SettingKeys.Accounts.Cash] = "حساب الصندوق", [SettingKeys.Accounts.Bank] = "حساب البنك",
            [SettingKeys.Accounts.Sales] = "حساب المبيعات", [SettingKeys.Accounts.SalesReturns] = "حساب مرتجعات المبيعات",
            [SettingKeys.Accounts.COGS] = "حساب تكلفة البضاعة المباعة", [SettingKeys.Accounts.Salaries] = "حساب الرواتب",
            [SettingKeys.Accounts.RetainedEarnings] = "حساب الأرباح المحتجزة", [SettingKeys.Accounts.VATInput] = "حساب ضريبة المدخلات",
            [SettingKeys.Accounts.VATOutput] = "حساب ضريبة المخرجات", [SettingKeys.Accounts.AutoLinkEnabled] = "تفعيل الربط التلقائي بالشجرة",

            [SettingKeys.Documents.JournalPrefix] = "بادئة قيود اليومية", [SettingKeys.Documents.SalesInvoicePrefix] = "بادئة فاتورة البيع",
            [SettingKeys.Documents.PurchaseInvoicePrefix] = "بادئة فاتورة الشراء", [SettingKeys.Documents.StockVoucherPrefix] = "بادئة إذن المخزون",
            [SettingKeys.Documents.NumberPadding] = "عدد أصفار الترقيم", [SettingKeys.Documents.ResetNumbersYearly] = "إعادة الترقيم كل سنة",
            [SettingKeys.Documents.SimplifiedFlow] = "الوضع المبسّط (بلا طلب/أمر/أذون دورة)",
            [SettingKeys.Documents.CustomerPrefix] = "بادئة كود العميل", [SettingKeys.Documents.SupplierPrefix] = "بادئة كود المورد",
            [SettingKeys.Documents.ProductPrefix] = "بادئة كود الصنف",

            [SettingKeys.UI.Theme] = "المظهر", [SettingKeys.UI.Language] = "اللغة", [SettingKeys.UI.UseArabicNumerals] = "أرقام عربية",
            [SettingKeys.UI.DateFormat] = "صيغة التاريخ", [SettingKeys.UI.PageSize] = "عدد الصفوف بالصفحة",
            [SettingKeys.UI.SidebarCollapsed] = "طي الشريط الجانبي افتراضياً", [SettingKeys.UI.Identity] = "حزمة الهوية البصرية",

            [SettingKeys.Backup.AutoBackupEnabled] = "تفعيل النسخ التلقائي", [SettingKeys.Backup.AutoBackupPath] = "مسار النسخ الاحتياطي",
            [SettingKeys.Backup.AutoBackupIntervalHours] = "الفاصل بالساعات", [SettingKeys.Backup.RetentionCount] = "عدد النسخ المحتفَظ بها",

            [SettingKeys.Security.PasswordMinLength] = "أقل طول لكلمة المرور", [SettingKeys.Security.SessionTimeoutMinutes] = "مهلة الجلسة (دقائق)",
            [SettingKeys.Security.RequirePasswordChange] = "إلزام تغيير كلمة المرور",
        };

        private static string LabelFor(string settingKey) => Labels.TryGetValue(settingKey, out var label) ? label : settingKey;
    }
}
