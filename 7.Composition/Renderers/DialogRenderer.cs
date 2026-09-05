using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Composition.Definitions;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Feedback;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    // ⚠️ لا ShowDialog() — تُعلَّق للأبد في بيئة هذا الجهاز (توقف 10، ARCHITECTURE.md). Show() + DispatcherFrame
    // يدوية بدلاً منها، نفس نمط App.xaml.cs.LoginWindow بالضبط.
    internal class ComposedDialogWindow : AppDialogWindow
    {
        public bool Saved;

        // عرض النافذة مُثبَّت في XAML على C.Dialog.Width.Sm (420) — كافٍ لحوار حقول مسطّحة، يقصّ أي محتوى
        // أعرض (صف سطور مستند/شبكة متعددة الأعمدة). width هنا قيمة محلية تتغلّب على DynamicResource تلقائياً،
        // بلا لمس XAML.
        public ComposedDialogWindow(string title, FrameworkElement body, FrameworkElement footer, double? width = null)
        {
            Title = title;
            HeaderTitle = title;
            HeaderVariant = StatusVariant.Brand;
            Body = body;
            Footer = footer;
            if (width.HasValue) { Width = width.Value; MinWidth = width.Value; }
            MinHeight = 320;
        }

        // القاعدة AppDialogWindow.OnEscapePressed تضبط DialogResult قبل Close() — صحيح لحوارات ShowDialog()
        // (AppConfirmDialog/AppMessageDialog) لكن يرمي InvalidOperationException هنا تحديداً (النافذة
        // مفتوحة عبر Show() لا ShowDialog()، توقف 10) فيسقط الاستثناء بصمت ولا يُنفَّذ Close() إطلاقاً —
        // زر إغلاق الهيدر (X) ومفتاح Escape يبقيان بلا أثر، النافذة تظل مفتوحة.
        protected override void OnEscapePressed() => Close();
    }

    public static class DialogRenderer
    {
        /// <summary>Type.GetMethod على واجهة لا يبحث في الواجهات المُوَرَّثة إطلاقاً — و IQuotationService
        /// وأخواتها ترث Create/Update/GetById من ICycleDocumentService بلا إعادة تصريح، فكان البحث يُعيد null
        /// ويسقط الاستدعاء بـ NullReferenceException غير مُعالَج يُنهي التطبيق كله عند الحفظ.</summary>
        internal static System.Reflection.MethodInfo FindMethod(Type serviceType, string name, params Type[] argumentTypes)
        {
            var method = serviceType.GetMethod(name, argumentTypes);
            if (method != null) return method;

            foreach (var inherited in serviceType.GetInterfaces())
            {
                method = inherited.GetMethod(name, argumentTypes);
                if (method != null) return method;
            }

            return null;
        }

        public static bool ShowAndSave(DialogDefinition dialog, IServiceProvider services, IToastService toast,
            object editItem = null, int? addModeDefaultPickerId = null)
        {
            bool isEdit = editItem != null;
            var fields = BuildAndPopulateFields(dialog.Fields, services, editItem, isEdit, addModeDefaultPickerId);
            var width = ComputeDialogWidth(dialog.GridColumns);
            var grid = BuildGrid(dialog.Fields, dialog.GridColumns, fields, width);

            foreach (var field in dialog.Fields.Where(f => f.Kind == FieldKind.Picker && f.PickerType == "Category"))
                WireCategoryPickerAddOption((AppComboBox)fields[field.Key], field, services, toast);

            var btnCancel = new Btn { Text = LocalizationService.Get("Str.Cancel"), Variant = "secondary", Size = "sm" };
            var btnSave = new Btn { Text = LocalizationService.Get("Str.Save"), Variant = "primary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };

            var title = isEdit ? LocalizationService.Get(dialog.TitleEditKey) : LocalizationService.Get(dialog.TitleKey);
            var footer = new StackPanel { Orientation = Orientation.Horizontal, Children = { btnCancel, btnSave } };
            var window = new ComposedDialogWindow(title, grid, footer, width);

            btnCancel.Click += (_, __) => window.Close();
            btnSave.Click += (_, __) =>
            {
                if (!TrySave(dialog, services, toast, fields, editItem, isEdit)) return;
                window.Saved = true;
                window.Close();
            };

            var frame = new DispatcherFrame();
            window.Closed += (_, __) => frame.Continue = false;
            window.Show();
            Dispatcher.PushFrame(frame);

            return window.Saved;
        }

        // Reflection مباشرة لا dynamic — dynamic كان يرمي RuntimeBinderException غامضاً هنا
        // ("has some invalid arguments") رغم تطابق النوع الفعلي تماماً؛ MethodInfo.Invoke حتمي وواضح.
        // بوابة واحدة قبل أي حفظ: القواعد مُعلَنة على الحقول، والرسالة تظهر عند الحقل نفسه.
        private static bool TrySave(DialogDefinition dialog, IServiceProvider services, IToastService toast,
            System.Collections.Generic.Dictionary<string, FrameworkElement> fields, object editItem, bool isEdit)
        {
            if (!FieldValidation.Validate(dialog.Fields, fields)) return false;

            var service = services.GetRequiredService(dialog.ServiceType);

            if (isEdit)
            {
                var updateDto = Activator.CreateInstance(dialog.UpdateDtoType);
                var idValue = editItem.GetType().GetProperty("Id")?.GetValue(editItem);
                dialog.UpdateDtoType.GetProperty("Id")?.SetValue(updateDto, idValue);
                ApplyFields(dialog.Fields, fields, updateDto, editOnly: true);
                ApplyFixedValues(dialog, updateDto);

                var method = FindMethod(dialog.ServiceType, "Update", dialog.UpdateDtoType);
                if (method == null) { toast.Error($"الخدمة {dialog.ServiceType.Name} بلا Update({dialog.UpdateDtoType.Name})"); return false; }

                var result = (Result)method.Invoke(service, new[] { updateDto });
                if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return false; }
            }
            else
            {
                var createDto = Activator.CreateInstance(dialog.CreateDtoType);
                ApplyFields(dialog.Fields, fields, createDto, editOnly: false);
                ApplyFixedValues(dialog, createDto);

                var method = FindMethod(dialog.ServiceType, "Create", dialog.CreateDtoType);
                if (method == null) { toast.Error($"الخدمة {dialog.ServiceType.Name} بلا Create({dialog.CreateDtoType.Name})"); return false; }

                var result = (Result)method.Invoke(service, new[] { createDto });
                if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return false; }
            }

            toast.Success(LocalizationService.Get("Str.Success"));
            return true;
        }

        internal static void ApplyFields(List<FieldDefinition> fieldDefs, Dictionary<string, FrameworkElement> fields, object dto, bool editOnly)
        {
            var dtoType = dto.GetType();
            foreach (var field in fieldDefs)
            {
                if (editOnly && field.IsReadOnlyOnEdit) continue;
                var prop = dtoType.GetProperty(field.Key);
                if (prop == null || !prop.CanWrite) continue;

                var value = GetControlValue(fields[field.Key], field.Kind);
                if (value == null) continue;
                // كلمة مرور فارغة = "بلا تغيير" (وضع التعديل بلا حقل خاص يحمل القيمة الحالية أصلاً) — لا تُكتَب فوق الهاش الحالي.
                if (field.Kind == FieldKind.Password && string.IsNullOrEmpty((string)value)) continue;
                prop.SetValue(dto, Convert.ChangeType(value, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType));
            }
        }

        private static void ApplyFixedValues(DialogDefinition dialog, object dto)
        {
            if (dialog.FixedValues == null) return;
            var dtoType = dto.GetType();
            foreach (var (key, value) in dialog.FixedValues)
                dtoType.GetProperty(key)?.SetValue(dto, value);
        }

        // بناء + تعبئة القيم لقائمة حقول مسطّحة — يخدم كلاً من الحوار العادي (رأس فقط) ورأس المستند
        // (DocumentRenderer)؛ لا علاقة له بسطور المستند المتكرّرة (تلك منطق DocumentRenderer الخاص).
        internal static Dictionary<string, FrameworkElement> BuildAndPopulateFields(
            List<FieldDefinition> fieldDefs, IServiceProvider services, object editItem, bool isEdit, int? addModeDefaultPickerId = null)
        {
            var controls = fieldDefs.ToDictionary(f => f.Key, BuildField);

            foreach (var field in fieldDefs)
            {
                var control = controls[field.Key];

                // Picker: تحميل العناصر أولاً ثم التحديد بمطابقة Id — SelectedValue وحدها لا تُحدِّد شيئاً في
                // AppComboBox (لا بحث عكسي من القيمة للعنصر)، فتحتاج SelectedItem الفعلي من القائمة المُحمَّلة.
                if (field.Kind == FieldKind.Picker)
                {
                    LoadPickerItems((AppComboBox)control, field, services);
                    // في وضع الإضافة: القيمة الافتراضية المُعلَنة على الحقل أولاً (طريقة الدفع "نقداً" مثلاً)،
                    // وإلا الافتراضي المُمرَّر من الشاشة. بلا هذا يبدأ الحقل فارغاً فلا يرشِّح شيئاً.
                    var presetId = isEdit
                        ? editItem.GetType().GetProperty(field.Key)?.GetValue(editItem) as int?
                        : field.DefaultValue as int? ?? addModeDefaultPickerId;
                    if (presetId != null) SelectPickerItem((AppComboBox)control, presetId.Value);
                }
                else if (isEdit)
                {
                    SetControlValue(control, field, editItem.GetType().GetProperty(field.Key)?.GetValue(editItem));
                }
                else if (field.DefaultValue != null)
                {
                    SetControlValue(control, field, field.DefaultValue);
                }
                else if (field.Kind == FieldKind.Date)
                {
                    // حقل تاريخ بلا قيمة يُقرأ صفراً فيُحفظ 0001-01-01 — تاريخ اليوم هو الافتراضي الصحيح
                    // في كل مستند، ويبقى قابلاً للتجاوز بـ DefaultValue على الحقل.
                    SetControlValue(control, field, DateTime.Today);
                }

                if (isEdit && field.IsReadOnlyOnEdit) control.IsEnabled = false;
            }

            ApplyConditionalVisibility(fieldDefs, controls, services);
            ApplyPickerFilters(fieldDefs, controls, services);
            return controls;
        }

        /// <summary>يعيد تعبئة أي قائمة مرتبطة بحقل حاكم كلما تغيّر — بلا كود خاص في كل شاشة.</summary>
        internal static void ApplyPickerFilters(List<FieldDefinition> fieldDefs, Dictionary<string, FrameworkElement> controls, IServiceProvider services)
        {
            var filtered = fieldDefs.Where(f => !string.IsNullOrEmpty(f.PickerFilterField) && f.Kind == FieldKind.Picker).ToList();

            foreach (var field in filtered)
            {
                if (!controls.TryGetValue(field.PickerFilterField, out var source)) continue;
                if (!controls.TryGetValue(field.Key, out var target) || target is not AppComboBox combo) continue;

                var sourceField = fieldDefs.First(f => f.Key == field.PickerFilterField);
                var captured = field;

                void Reload()
                {
                    combo.SelectedItem = null;
                    LoadPickerItems(combo, captured, services, GetControlValue(source, sourceField.Kind));
                }

                if (source is AppComboBox sourceCombo) sourceCombo.SelectionChanged += (_, __) => Reload();
                else if (source is AppCheckBox sourceCheck) sourceCheck.CheckedChanged += (_, __) => Reload();

                Reload();
            }
        }

        /// <summary>يُظهر/يُخفي الحقول المشروطة ويعيد التقييم كلما تغيّر الحقل الحاكم — الشرط مُعلَن في التعريف
        /// لا مكتوب يدوياً لكل شاشة.</summary>
        internal static void ApplyConditionalVisibility(List<FieldDefinition> fieldDefs, Dictionary<string, FrameworkElement> controls, IServiceProvider services = null)
        {
            var conditional = fieldDefs.Where(f => !string.IsNullOrEmpty(f.VisibleWhenField) || f.VisibleWhen != null).ToList();
            if (conditional.Count == 0) return;

            void Evaluate()
            {
                foreach (var field in conditional)
                {
                    if (!controls.TryGetValue(field.VisibleWhenField, out var source)) continue;

                    var sourceField = fieldDefs.First(f => f.Key == field.VisibleWhenField);
                    var current = GetControlValue(source, sourceField.Kind);
                    var matches = field.VisibleWhen != null
                        ? field.VisibleWhen(current, services)
                        : current != null && current.ToString() == field.VisibleWhenValue?.ToString();
                    controls[field.Key].Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            foreach (var sourceKey in conditional.Select(f => f.VisibleWhenField).Distinct())
            {
                if (!controls.TryGetValue(sourceKey, out var source)) continue;

                switch (source)
                {
                    case AppComboBox combo:   combo.SelectionChanged += (_, __) => Evaluate(); break;
                    case AppCheckBox check:   check.CheckedChanged += (_, __) => Evaluate(); break;
                    case AppTextBox text:     text.TextChanged += (_, __) => Evaluate(); break;
                }
            }

            Evaluate();
        }

        // GridColumns=1→Sm(420), 2→Md(560), 3→Lg(760), أكثر→Xl(1000) — حوار الحقول المسطّحة العادي، لا حوار
        // مستند (ذاك يحسب عرضه من مجموع أعمدة سطوره في DocumentRenderer.ComputeDialogWidth).
        private static double ComputeDialogWidth(int gridColumns)
        {
            var key = gridColumns switch { <= 1 => "C.Dialog.Width.Sm", 2 => "C.Dialog.Width.Md", 3 => "C.Dialog.Width.Lg", _ => "C.Dialog.Width.Xl" };
            return (double)System.Windows.Application.Current.FindResource(key);
        }

        internal static Grid BuildGrid(List<FieldDefinition> fieldDefs, int gridColumns, Dictionary<string, FrameworkElement> fields, double? width = 420)
        {
            var grid = new Grid();
            if (width.HasValue) grid.Width = width.Value;
            for (int i = 0; i < gridColumns; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int col = 0, row = 0;
            foreach (var field in fieldDefs)
            {
                var control = fields[field.Key];
                var span = Math.Min(field.ColumnSpan, gridColumns);
                if (col + span > gridColumns) { col = 0; row++; }

                if (grid.RowDefinitions.Count <= row) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                control.Margin = new Thickness(0, 0, col + span < gridColumns ? 8 : 0, 12);
                control.MinWidth = 180;
                control.HorizontalAlignment = HorizontalAlignment.Stretch;
                Grid.SetRow(control, row);
                Grid.SetColumn(control, col);
                Grid.SetColumnSpan(control, span);
                grid.Children.Add(control);

                col += span;
                if (col >= gridColumns) { col = 0; row++; }
            }

            return grid;
        }

        internal static FrameworkElement BuildField(FieldDefinition field)
        {
            var label = LocalizationService.Get(field.LabelKey);
            return field.Kind switch
            {
                FieldKind.Text => new AppTextBox { Label = label, IsRequired = field.IsRequired, MaxLength = field.MaxLength },
                FieldKind.ReadOnly => new AppTextBox { Label = label, IsReadOnly = true },
                FieldKind.Number => new AppNumericBox { Label = label, IsRequired = field.IsRequired },
                FieldKind.Date => new AppDatePicker { Label = label, IsRequired = field.IsRequired },
                FieldKind.Check => new AppCheckBox { Label = label },
                FieldKind.TextArea => new AppTextArea { Label = label, IsRequired = field.IsRequired, MaxLength = field.MaxLength, Rows = 3 },
                FieldKind.Picker => new AppComboBox { Label = label, IsRequired = field.IsRequired, DisplayMemberPath = "Display", SelectedValuePath = field.PickerValueField },
                FieldKind.Password => new AppPasswordBox { Label = label, IsRequired = field.IsRequired },
                FieldKind.Image => new AppImagePicker(label),
                _ => new AppTextBox { Label = label }
            };
        }

        internal static void SetControlValue(FrameworkElement control, FieldDefinition field, object value)
        {
            if (value == null) return;
            switch (field.Kind)
            {
                case FieldKind.Text: ((AppTextBox)control).Text = value.ToString(); break;
                case FieldKind.ReadOnly:
                    ((AppTextBox)control).Text = !string.IsNullOrEmpty(field.DisplayFormat) && value is IFormattable f
                        ? f.ToString(field.DisplayFormat, null) : value.ToString();
                    break;
                case FieldKind.TextArea: ((AppTextArea)control).Text = value.ToString(); break;
                case FieldKind.Number: ((AppNumericBox)control).Value = Convert.ToDecimal(value); break;
                case FieldKind.Date: ((AppDatePicker)control).SelectedDate = value as DateTime?; break;
                case FieldKind.Check: ((AppCheckBox)control).IsChecked = Convert.ToBoolean(value); break;
                case FieldKind.Picker: ((AppComboBox)control).SelectedValue = value; break;
                case FieldKind.Image: ((AppImagePicker)control).Value = value.ToString(); break;
            }
        }

        internal static object GetControlValue(FrameworkElement control, FieldKind kind) => kind switch
        {
            FieldKind.Text or FieldKind.ReadOnly => ((AppTextBox)control).Text,
            FieldKind.TextArea => ((AppTextArea)control).Text,
            FieldKind.Number => ((AppNumericBox)control).Value,
            FieldKind.Date => ((AppDatePicker)control).SelectedDate,
            FieldKind.Check => ((AppCheckBox)control).IsChecked,
            FieldKind.Picker => ((AppComboBox)control).SelectedValue,
            FieldKind.Password => ((AppPasswordBox)control).Password,
            FieldKind.Image => ((AppImagePicker)control).Value ?? "",
            _ => null
        };

        // matchProperty="Id" افتراضياً (اختيار برقم داخلي) أو "Code" (سطر يحتاج كود الحساب نصاً — راجع
        // FieldDefinition.PickerValueField).
        internal static void SelectPickerItem(AppComboBox combo, object value, string matchProperty = "Id")
        {
            var match = (combo.ItemsSource as System.Collections.IEnumerable)?.Cast<object>()
                .FirstOrDefault(i => Equals(i.GetType().GetProperty(matchProperty)?.GetValue(i), value));
            if (match != null) combo.SelectedItem = match;
        }

        // PickerType="Account"/"Category" — عبر الخدمة مباشرة، لا IPickerDataSource<T> عام (غير مسجَّل في DI
        // بعد). نطاق مُبسَّط، راجع تقرير R11.
        internal static void LoadPickerItems(AppComboBox combo, FieldDefinition field, IServiceProvider services, object filterValue = null)
        {
            if (field.PickerType == "Account")
            {
                var accountService = services.GetRequiredService<IAccountService>();
                var filter = new AccountTreeFilter { LeafOnly = field.PickerLeafOnly };
                var pageResult = accountService.GetPaged(1, 5000, filter);
                if (!pageResult.IsSuccess) return;

                var accountRows = pageResult.Value.Items.AsEnumerable();
                if (field.PickerGroupsOnly) accountRows = accountRows.Where(a => !a.IsLeaf);

                combo.ItemsSource = accountRows
                    .Select(a => new PickerRow { Id = a.Id, Code = a.Code, Display = $"{a.Code} - {a.Name}" })
                    .ToList();
            }
            else if (field.PickerType == "Category")
            {
                var categoryService = services.GetRequiredService<PrimeERP.Application.Services.Common.ICategoryService>();
                var result = categoryService.GetAll(field.PickerCategoryModuleKey);
                if (!result.IsSuccess) return;

                var rows = result.Value.Select(c => new PickerRow { Id = c.Id, Code = null, Display = c.Name }).ToList();
                rows.Add(new PickerRow { Id = AddCategorySentinelId, Code = null, Display = "+ " + LocalizationService.Get("Str.AddCategory") });
                combo.ItemsSource = rows;
            }
            else if (field.PickerType == "Department")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Services.HR.IDepartmentService>().GetAll();
                if (result.IsSuccess) combo.ItemsSource = result.Value.Select(d => new PickerRow { Id = d.Id, Code = null, Display = d.Name }).ToList();
            }
            else if (field.PickerType == "JobTitle")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Services.HR.IJobTitleService>().GetAll();
                if (result.IsSuccess) combo.ItemsSource = result.Value.Select(j => new PickerRow { Id = j.Id, Code = null, Display = j.Name }).ToList();
            }
            else if (field.PickerType == "Role")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Services.Security.IRoleService>().GetAll();
                if (result.IsSuccess) combo.ItemsSource = result.Value.Select(r => new PickerRow { Id = r.Id, Code = null, Display = r.NameAr }).ToList();
            }
            else if (field.PickerType == "Customer")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Services.Parties.ICustomerService>().GetPaged(1, 5000);
                if (result.IsSuccess) combo.ItemsSource = result.Value.Items.Select(c => new PickerRow { Id = c.Id, Code = c.Code, Display = $"{c.Code} - {c.Name}" }).ToList();
            }
            else if (field.PickerType == "Supplier")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Services.Parties.ISupplierService>().GetPaged(1, 5000);
                if (result.IsSuccess) combo.ItemsSource = result.Value.Items.Select(s => new PickerRow { Id = s.Id, Code = s.Code, Display = $"{s.Code} - {s.Name}" }).ToList();
            }
            else if (field.PickerType == "Product")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Services.Inventory.IProductService>().GetPaged(1, 5000);
                if (result.IsSuccess) combo.ItemsSource = result.Value.Items.Select(p => new PickerRow { Id = p.Id, Code = p.Code, Display = $"{p.Code} - {p.Name}" }).ToList();
            }
            else if (field.PickerType == "Treasury")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Services.Treasury.ITreasuryService>().GetAll();
                if (!result.IsSuccess) return;

                // المرشِّح طريقة دفع لا نوع خزينة: نقداً يعرض الخزن، وتحويلاً بنكياً أو شيكاً يعرض البنوك.
                var items = result.Value.AsEnumerable();
                if (filterValue != null && int.TryParse(filterValue.ToString(), out var method) && method > 0)
                {
                    var kind = (PrimeERP.Domain.Enums.PaymentMethod)method == PrimeERP.Domain.Enums.PaymentMethod.Cash
                        ? PrimeERP.Domain.Enums.TreasuryKind.Cash
                        : PrimeERP.Domain.Enums.TreasuryKind.Bank;
                    items = items.Where(t => t.Kind == kind);
                }

                combo.ItemsSource = items.Select(t => new PickerRow { Id = t.Id, Code = t.Code, Display = t.Name }).ToList();
            }
            else if (field.PickerType == "TreasuryKind")
            {
                combo.ItemsSource = new List<PickerRow>
                {
                    new() { Id = (int)PrimeERP.Domain.Enums.TreasuryKind.Cash, Display = "خزينة" },
                    new() { Id = (int)PrimeERP.Domain.Enums.TreasuryKind.Bank, Display = "بنك" },
                };
            }
            else if (field.PickerType == "SalesInvoice")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Services.Sales.ISalesInvoiceService>().GetPaged(1, 2000);
                if (result.IsSuccess)
                    combo.ItemsSource = result.Value.Items
                        .Select(i => new PickerRow { Id = i.Id, Code = i.InvoiceNo, Display = $"{i.InvoiceNo} — {i.CustomerName} ({i.NetTotal:N2})" }).ToList();
            }
            else if (field.PickerType == "PurchaseInvoice")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Services.Purchasing.IPurchaseInvoiceService>().GetPaged(1, 2000);
                if (result.IsSuccess)
                    combo.ItemsSource = result.Value.Items
                        .Select(i => new PickerRow { Id = i.Id, Code = i.InvoiceNo, Display = $"{i.InvoiceNo} — {i.SupplierName} ({i.NetTotal:N2})" }).ToList();
            }
            else if (field.PickerType == "PaymentMethod")
            {
                combo.ItemsSource = new List<PickerRow>
                {
                    new() { Id = (int)PrimeERP.Domain.Enums.PaymentMethod.Cash,   Display = "نقدي" },
                    new() { Id = (int)PrimeERP.Domain.Enums.PaymentMethod.Bank,   Display = "تحويل بنكي" },
                    new() { Id = (int)PrimeERP.Domain.Enums.PaymentMethod.Cheque, Display = "شيك" },
                };
            }
            else if (field.PickerType == "Warehouse")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Services.Inventory.IWarehouseService>().GetAll();
                if (result.IsSuccess) combo.ItemsSource = result.Value.Select(w => new PickerRow { Id = w.Id, Code = w.Code, Display = w.Name }).ToList();
            }
            else if (field.PickerType == "Employee")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Services.HR.IEmployeeService>().GetPaged(1, 5000);
                if (result.IsSuccess) combo.ItemsSource = result.Value.Items.Select(e => new PickerRow { Id = e.Id, Code = e.Code, Display = $"{e.Code} - {e.Name}" }).ToList();
            }
        }

        // معرّف اصطناعي بند "+ إضافة فئة" في نهاية قائمة منتقي الفئة — راجع WireCategoryPickerAddOption.
        private const int AddCategorySentinelId = -1;

        private static void WireCategoryPickerAddOption(AppComboBox combo, FieldDefinition field, IServiceProvider services, IToastService toast)
        {
            combo.SelectionChanged += (_, __) =>
            {
                if (combo.SelectedItem is not PickerRow row || row.Id != AddCategorySentinelId) return;

                ShowAndSave(CategoryDialogFactory.Build(field.PickerCategoryModuleKey), services, toast);

                // إعادة تحميل تشمل أي فئة أُضيفت + بند الإضافة نفسه من جديد؛ التحديد يُترَك فارغاً — المستخدم
                // يختار الفئة الجديدة من القائمة المُحدَّثة مباشرة (بلا تعقيد لإرجاع الـId المُنشأ من ShowAndSave).
                LoadPickerItems(combo, field, services);
                combo.SelectedItem = null;
            };
        }

        internal class PickerRow { public int Id { get; set; } public string Code { get; set; } public string Display { get; set; } }
    }
}
