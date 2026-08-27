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

        public ComposedDialogWindow(string title, FrameworkElement body, FrameworkElement footer)
        {
            Title = title;
            HeaderTitle = title;
            HeaderVariant = StatusVariant.Brand;
            Body = body;
            Footer = footer;
        }
    }

    public static class DialogRenderer
    {
        public static bool ShowAndSave(DialogDefinition dialog, IServiceProvider services, IToastService toast,
            object editItem = null, int? addModeDefaultPickerId = null)
        {
            bool isEdit = editItem != null;
            var fields = BuildAndPopulateFields(dialog.Fields, services, editItem, isEdit, addModeDefaultPickerId);
            var grid = BuildGrid(dialog.Fields, dialog.GridColumns, fields);

            var btnCancel = new Btn { Text = LocalizationService.Get("Str.Cancel"), Variant = "secondary", Size = "sm" };
            var btnSave = new Btn { Text = LocalizationService.Get("Str.Save"), Variant = "primary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };

            var title = isEdit ? LocalizationService.Get(dialog.TitleEditKey) : LocalizationService.Get(dialog.TitleKey);
            var footer = new StackPanel { Orientation = Orientation.Horizontal, Children = { btnCancel, btnSave } };
            var window = new ComposedDialogWindow(title, grid, footer);

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
        private static bool TrySave(DialogDefinition dialog, IServiceProvider services, IToastService toast,
            System.Collections.Generic.Dictionary<string, FrameworkElement> fields, object editItem, bool isEdit)
        {
            var service = services.GetRequiredService(dialog.ServiceType);

            if (isEdit)
            {
                var updateDto = Activator.CreateInstance(dialog.UpdateDtoType);
                var idValue = editItem.GetType().GetProperty("Id")?.GetValue(editItem);
                dialog.UpdateDtoType.GetProperty("Id")?.SetValue(updateDto, idValue);
                ApplyFields(dialog.Fields, fields, updateDto, editOnly: true);

                var method = dialog.ServiceType.GetMethod("Update", new[] { dialog.UpdateDtoType });
                var result = (Result)method.Invoke(service, new[] { updateDto });
                if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return false; }
            }
            else
            {
                var createDto = Activator.CreateInstance(dialog.CreateDtoType);
                ApplyFields(dialog.Fields, fields, createDto, editOnly: false);

                var method = dialog.ServiceType.GetMethod("Create", new[] { dialog.CreateDtoType });
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
                prop.SetValue(dto, Convert.ChangeType(value, prop.PropertyType));
            }
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
                    var presetId = isEdit ? editItem.GetType().GetProperty(field.Key)?.GetValue(editItem) as int? : addModeDefaultPickerId;
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

                if (isEdit && field.IsReadOnlyOnEdit) control.IsEnabled = false;
            }

            return controls;
        }

        internal static Grid BuildGrid(List<FieldDefinition> fieldDefs, int gridColumns, Dictionary<string, FrameworkElement> fields)
        {
            var grid = new Grid { Width = 420 };
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

        // PickerType="Account" فقط مدعوم حالياً — عبر IAccountService.GetPaged مباشرة، لا IPickerDataSource<T>
        // عام (غير مسجَّل في DI بعد). نطاق مُبسَّط، راجع تقرير R11.
        internal static void LoadPickerItems(AppComboBox combo, FieldDefinition field, IServiceProvider services)
        {
            if (field.PickerType != "Account") return;

            var accountService = services.GetRequiredService<IAccountService>();
            var filter = new AccountTreeFilter { LeafOnly = field.PickerLeafOnly };
            var pageResult = accountService.GetPaged(1, 5000, filter);
            if (!pageResult.IsSuccess) return;

            combo.ItemsSource = pageResult.Value.Items
                .Select(a => new PickerRow { Id = a.Id, Code = a.Code, Display = $"{a.Code} - {a.Name}" })
                .ToList();
        }

        private class PickerRow { public int Id { get; set; } public string Code { get; set; } public string Display { get; set; } }
    }
}
