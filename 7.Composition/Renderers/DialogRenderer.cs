using PrimeERP.Application.Legacy.Security;
using PrimeERP.Application.Legacy.Builder;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Composition.Definitions;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Feedback;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    // ⚠️ no ShowDialog() — ARCHITECTURE § المصائد
    internal class ComposedDialogWindow : AppDialogWindow
    {
        public bool Saved;

        public ComposedDialogWindow(string title, FrameworkElement body, FrameworkElement footer, double? width = null)
        {
            Title = title;
            HeaderTitle = title;
            HeaderVariant = StatusVariant.Brand;
            Body = body;
            Footer = footer;
            if (width.HasValue) CardWidth = width.Value;
            MinHeight = 320;
        }

        protected override void OnEscapePressed() => Close();
    }

    /// <summary>تصيير الحوار من وصفه</summary>
    public static class DialogRenderer
    {
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
            var grid = BuildGrid(dialog.Fields, dialog.GridColumns, fields, null);

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

        private static bool TrySave(DialogDefinition dialog, IServiceProvider services, IToastService toast,
            System.Collections.Generic.Dictionary<string, FrameworkElement> fields, object editItem, bool isEdit)
        {
            if (!FieldValidation.Validate(dialog.Fields, fields)) return false;

            var service = Resolve.Service(dialog, services);

            if (isEdit)
            {
                var loaded = Loaded(dialog, service, ReadValue(editItem, "Id"));
                if (loaded != null && !loaded.IsSuccess) { toast.Error(loaded.ErrorMessage); return false; }

                var updateDto = loaded?.GetType().GetProperty(nameof(Result<object>.Value))?.GetValue(loaded)
                                ?? Activator.CreateInstance(dialog.UpdateDtoType);
                WriteValue(updateDto, "Id", ReadValue(editItem, "Id"));
                ApplyFields(dialog.Fields, fields, updateDto, editOnly: true, writeEmpty: loaded != null);
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

        /// <summary>السجل المحمَّل للتعديل</summary>
        private static Result Loaded(DialogDefinition dialog, object service, object id)
        {
            var getById = FindMethod(dialog.ServiceType, "GetById", typeof(int));
            return getById?.ReturnType == typeof(Result<>).MakeGenericType(dialog.UpdateDtoType) && id is int key
                ? (Result)getById.Invoke(service, new object[] { key })
                : null;
        }

        private static object PickerValue(object value, string valueField) => value switch
        {
            null                      => null,
            Enum e                    => Convert.ToInt32(e),
            _ when valueField == "Id" => int.TryParse(value.ToString(), out var id) ? id : (int?)null,
            _                         => value
        };

        internal static object ReadValue(object source, string key)
        {
            if (source == null) return null;
            if (source is IDictionary<string, object> row) return row.TryGetValue(key, out var value) ? value : null;

            return source.GetType().GetProperty(key)?.GetValue(source);
        }

        internal static void WriteValue(object target, string key, object value)
        {
            if (target is IDictionary<string, object> row) { row[key] = value; return; }

            var property = target.GetType().GetProperty(key);
            if (property == null || !property.CanWrite) return;

            property.SetValue(target, Coerce(value, property.PropertyType));
        }

        internal static object Coerce(object value, Type targetType)
        {
            if (value == null) return null;

            var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (type.IsInstanceOfType(value)) return value;

            return type.IsEnum ? Enum.ToObject(type, value) : Convert.ChangeType(value, type);
        }

        internal static void ApplyFields(List<FieldDefinition> fieldDefs, Dictionary<string, FrameworkElement> fields, object dto, bool editOnly,
            bool writeEmpty = false)
        {
            var row = dto as IDictionary<string, object>;
            var dtoType = dto.GetType();

            foreach (var field in fieldDefs)
            {
                if (editOnly && field.IsReadOnlyOnEdit) continue;

                var prop = row == null ? dtoType.GetProperty(field.Key) : null;
                if (row == null && (prop == null || !prop.CanWrite)) continue;

                var value = GetControlValue(fields[field.Key], field.Kind);
                if (value == null && !writeEmpty) continue;
                if (field.Kind == FieldKind.Password && string.IsNullOrEmpty((string)value)) continue;

                if (row != null) row[field.Key] = value;
                else prop.SetValue(dto, Coerce(value, prop.PropertyType));
            }
        }

        private static void ApplyFixedValues(DialogDefinition dialog, object dto)
        {
            if (dialog.FixedValues == null) return;
            var dtoType = dto.GetType();
            foreach (var (key, value) in dialog.FixedValues)
                dtoType.GetProperty(key)?.SetValue(dto, value);
        }

        internal static Dictionary<string, FrameworkElement> BuildAndPopulateFields(
            List<FieldDefinition> fieldDefs, IServiceProvider services, object editItem, bool isEdit, int? addModeDefaultPickerId = null)
        {
            var controls = fieldDefs.ToDictionary(f => f.Key, BuildField);

            foreach (var field in fieldDefs)
            {
                var control = controls[field.Key];

                if (field.Kind == FieldKind.Picker)
                {
                    LoadPickerItems((AppComboBox)control, field, services);
                    var preset = isEdit
                        ? PickerValue(ReadValue(editItem, field.Key), field.PickerValueField)
                        : field.DefaultValue as int? ?? addModeDefaultPickerId;

                    if (preset != null) SelectPickerItem((AppComboBox)control, preset, field.PickerValueField);
                }
                else if (isEdit)
                {
                    SetControlValue(control, field, ReadValue(editItem, field.Key));
                }
                else if (field.DefaultValue != null)
                {
                    SetControlValue(control, field, field.DefaultValue);
                }
                else if (field.Kind == FieldKind.Date)
                {
                    SetControlValue(control, field, DateTime.Today);
                }

                if (field.IsReadOnly || (isEdit && field.IsReadOnlyOnEdit)) control.IsEnabled = false;
            }

            ApplyFlowScope(fieldDefs, controls, services);
            ApplyConditionalVisibility(fieldDefs, controls, services);
            ApplyPickerFilters(fieldDefs, controls, services);
            return controls;
        }

        private static void ApplyFlowScope(List<FieldDefinition> fieldDefs, Dictionary<string, FrameworkElement> controls, IServiceProvider services)
        {
            var scoped = fieldDefs.Where(f => f.FlowScope != PrimeERP.Composition.Definitions.FlowScope.Both).ToList();
            if (scoped.Count == 0) return;

            var settings = services?.GetService(typeof(PrimeERP.Platform.Settings.ISettingsProvider)) as PrimeERP.Platform.Settings.ISettingsProvider;
            var simplified = settings?.Get(PrimeERP.Platform.Settings.SettingKeys.Documents.SimplifiedFlow, true) ?? true;

            foreach (var field in scoped)
            {
                if (!controls.TryGetValue(field.Key, out var control)) continue;

                var matches = field.FlowScope == (simplified
                    ? PrimeERP.Composition.Definitions.FlowScope.SimplifiedOnly
                    : PrimeERP.Composition.Definitions.FlowScope.FullCycleOnly);

                control.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        internal static void OnChanged(FrameworkElement control, Action handler)
        {
            switch (control)
            {
                case AppComboBox combo:   combo.SelectionChanged += (_, __) => handler(); break;
                case AppCheckBox check:   check.CheckedChanged   += (_, __) => handler(); break;
                case AppDatePicker date:  date.SelectedDateChanged += (_, __) => handler(); break;
            }
        }

        internal static void ApplyPickerFilters(List<FieldDefinition> fieldDefs, Dictionary<string, FrameworkElement> controls, IServiceProvider services)
        {
            var filtered = fieldDefs.Where(f => !string.IsNullOrEmpty(f.PickerFilterField) && f.Kind == FieldKind.Picker).ToList();

            foreach (var field in filtered)
            {
                if (!controls.TryGetValue(field.PickerFilterField, out var source)) continue;
                if (!controls.TryGetValue(field.Key, out var target) || target is not AppComboBox combo) continue;

                var sourceField = fieldDefs.First(f => f.Key == field.PickerFilterField);
                var captured = field;

                void Reload(bool keepSelection)
                {
                    var selected = combo.SelectedValue;

                    LoadPickerItems(combo, captured, services, GetControlValue(source, sourceField.Kind));

                    if (keepSelection && selected != null) SelectPickerItem(combo, selected, captured.PickerValueField);
                    else combo.SelectedItem = null;
                }

                OnChanged(source, () => Reload(keepSelection: false));

                Reload(keepSelection: true);
            }
        }

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

        internal static void SelectPickerItem(AppComboBox combo, object value, string matchProperty = "Id")
        {
            var match = (combo.ItemsSource as System.Collections.IEnumerable)?.Cast<object>()
                .FirstOrDefault(i => Equals(i.GetType().GetProperty(matchProperty)?.GetValue(i), value));
            if (match != null) combo.SelectedItem = match;
        }

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
                var categoryService = services.GetRequiredService<PrimeERP.Application.Legacy.Common.ICategoryService>();
                var result = categoryService.GetAll(field.PickerCategoryModuleKey);
                if (!result.IsSuccess) return;

                var rows = result.Value.Select(c => new PickerRow { Id = c.Id, Code = null, Display = c.Name }).ToList();
                rows.Add(new PickerRow { Id = AddCategorySentinelId, Code = null, Display = "+ " + LocalizationService.Get("Str.AddCategory") });
                combo.ItemsSource = rows;
            }
            else if (field.PickerType == "Department")
            {
                combo.ItemsSource = LookupRows<PrimeERP.Domain.Entities.Department>(services, withCode: false);
            }
            else if (field.PickerType == "JobTitle")
            {
                combo.ItemsSource = LookupRows<PrimeERP.Domain.Entities.JobTitle>(services, withCode: false);
            }
            else if (field.PickerType == "Role")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Legacy.Security.IRoleService>().GetAll();
                if (result.IsSuccess) combo.ItemsSource = result.Value.Select(r => new PickerRow { Id = r.Id, Code = null, Display = r.NameAr }).ToList();
            }
            else if (field.PickerType?.StartsWith("Table:") == true)
            {
                var parts = field.PickerType.Split(':');
                var moduleKey = parts.Length > 1 ? parts[1] : null;
                var display = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : "Name";

                var builder = services.GetRequiredService<PrimeERP.Application.Legacy.Builder.IBuilderCatalog>();
                var target = builder.Modules().FirstOrDefault(m => m.Key == moduleKey);
                if (target == null || string.IsNullOrWhiteSpace(target.TableName)) return;

                combo.ItemsSource = builder.PickerRows(target.TableName, display)
                    .Select(r => new PickerRow { Id = r.Id, Code = r.Id.ToString(), Display = r.Display }).ToList();
            }
            else if (field.PickerType == "Customer")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Legacy.Parties.ICustomerService>().GetPaged(1, 5000);
                if (result.IsSuccess) combo.ItemsSource = result.Value.Items.Select(c => new PickerRow { Id = c.Id, Code = c.Code, Display = $"{c.Code} - {c.Name}" }).ToList();
            }
            else if (field.PickerType == "Supplier")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Legacy.Parties.ISupplierService>().GetPaged(1, 5000);
                if (result.IsSuccess) combo.ItemsSource = result.Value.Items.Select(s => new PickerRow { Id = s.Id, Code = s.Code, Display = $"{s.Code} - {s.Name}" }).ToList();
            }
            else if (field.PickerType == "Asset")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Legacy.Assets.IAssetService>().GetPaged(1, 5000);
                if (result.IsSuccess) combo.ItemsSource = result.Value.Items.Select(a => new PickerRow { Id = a.Id, Code = a.Code, Display = $"{a.Code} - {a.Name}" }).ToList();
            }
            else if (field.PickerType == "Product")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Legacy.Inventory.IProductService>().GetPaged(1, 5000);
                if (result.IsSuccess) combo.ItemsSource = result.Value.Items.Select(p => new PickerRow { Id = p.Id, Code = p.Code, Display = $"{p.Code} - {p.Name}" }).ToList();
            }
            else if (field.PickerType == "Treasury")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Legacy.Treasury.ITreasuryService>().GetAll();
                if (!result.IsSuccess) return;

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
            else if (field.PickerType == "AssetFunding")
            {
                if (!int.TryParse(filterValue?.ToString(), out var method) || method <= 0) return;

                if ((PrimeERP.Domain.Enums.AssetAcquisition)method == PrimeERP.Domain.Enums.AssetAcquisition.Supplier)
                {
                    var suppliers = services.GetRequiredService<PrimeERP.Application.Legacy.Parties.ISupplierService>().GetPaged(1, 5000);
                    if (suppliers.IsSuccess)
                        combo.ItemsSource = suppliers.Value.Items
                            .Select(s => new PickerRow { Id = s.Id, Code = s.Code, Display = $"{s.Code} - {s.Name}" }).ToList();

                    return;
                }

                var treasuries = services.GetRequiredService<PrimeERP.Application.Legacy.Treasury.ITreasuryService>().GetAll();
                if (treasuries.IsSuccess)
                    combo.ItemsSource = treasuries.Value
                        .Where(t => t.Kind == (PrimeERP.Domain.Enums.TreasuryKind)method)
                        .Select(t => new PickerRow { Id = t.Id, Code = t.Code, Display = t.Name }).ToList();
            }
            else if (field.PickerType == "Bank")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Legacy.Treasury.ITreasuryService>().GetAll();
                if (result.IsSuccess)
                    combo.ItemsSource = result.Value
                        .Where(t => t.Kind == PrimeERP.Domain.Enums.TreasuryKind.Bank)
                        .Select(t => new PickerRow { Id = t.Id, Code = t.Code, Display = t.Name }).ToList();
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
                var result = services.GetRequiredService<PrimeERP.Application.Legacy.Sales.ISalesInvoiceService>().GetPaged(1, 2000);
                if (result.IsSuccess)
                    combo.ItemsSource = result.Value.Items
                        .Select(i => new PickerRow { Id = i.Id, Code = i.InvoiceNo, Display = $"{i.InvoiceNo} — {i.CustomerName} ({i.NetTotal:N2})" }).ToList();
            }
            else if (field.PickerType == "PurchaseInvoice")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Legacy.Purchasing.IPurchaseInvoiceService>().GetPaged(1, 2000);
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
                };
            }
            else if (field.PickerType == "Warehouse")
            {
                combo.ItemsSource = LookupRows<PrimeERP.Domain.Entities.Warehouse>(services, withCode: true);
            }
            else if (field.PickerType == "Employee")
            {
                var result = services.GetRequiredService<PrimeERP.Application.Legacy.HR.IEmployeeService>().GetPaged(1, 5000);
                if (result.IsSuccess) combo.ItemsSource = result.Value.Items.Select(e => new PickerRow { Id = e.Id, Code = e.Code, Display = $"{e.Code} - {e.Name}" }).ToList();
            }
            else
            {
                var rows = BuilderPickers.Rows(field.PickerType, services, filterValue);
                if (rows != null) combo.ItemsSource = rows;
            }
        }

        private const int AddCategorySentinelId = -1;

        private static void WireCategoryPickerAddOption(AppComboBox combo, FieldDefinition field, IServiceProvider services, IToastService toast)
        {
            combo.SelectionChanged += (_, __) =>
            {
                if (combo.SelectedItem is not PickerRow row || row.Id != AddCategorySentinelId) return;

                ShowAndSave(CategoryDialogFactory.Build(field.PickerCategoryModuleKey), services, toast);

                LoadPickerItems(combo, field, services);
                combo.SelectedItem = null;
            };
        }

        internal class PickerRow { public int Id { get; set; } public string Code { get; set; } public string Display { get; set; } }

        private static List<PickerRow> LookupRows<T>(IServiceProvider services, bool withCode)
            where T : PrimeERP.Domain.Entities.Common.BaseModel, new() =>
            services.GetRequiredService<PrimeERP.Application.Services.Entities.Lookup<T>>().GetAll().Value
                .Select(r => new PickerRow { Id = Convert.ToInt32(r["Id"]), Code = withCode ? r["Code"] as string : null, Display = r["Name"] as string })
                .ToList();
    }
}
