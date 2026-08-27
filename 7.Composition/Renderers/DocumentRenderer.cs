using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    // محرر مستند رأس+سطور عام (قيد يومية اليوم، فواتير لاحقاً) — يعيد استخدام بناء/تعبئة/قراءة حقول
    // DialogRenderer للرأس، ويبني شبكة سطور متكررة (إضافة/حذف صف) بنفس آلية FieldKind لكل عمود. لا رصيد
    // حيّ في الواجهة عمداً — الخادم (JournalValidator) يرفض القيد غير المتوازن برسالة واضحة عند الحفظ.
    public static class DocumentRenderer
    {
        public static bool ShowAndSave(DocumentDialogDefinition def, IServiceProvider services, IToastService toast, object editItem = null)
        {
            bool isEdit = editItem != null;

            // editItem الوارد من شبكة القائمة هو JournalEntryDto (بلا Lines) — يحتاج استبداله بالنسخة
            // الكاملة (JournalEntryDetailDto) قبل بناء السطور. GetById(int) اتفاقية اسم/توقيع موحّدة لأي
            // خدمة مستند تستهلك هذا المحرر.
            if (isEdit)
            {
                var id = (int)editItem.GetType().GetProperty("Id").GetValue(editItem);
                var service0 = services.GetRequiredService(def.ServiceType);
                var getById = def.ServiceType.GetMethod("GetById", new[] { typeof(int) });
                var detailResult = (Result)getById.Invoke(service0, new object[] { id });
                if (!detailResult.IsSuccess) { toast.Error(detailResult.ErrorMessage); return false; }
                editItem = detailResult.GetType().GetProperty("Value").GetValue(detailResult);
            }

            var headerControls = DialogRenderer.BuildAndPopulateFields(def.HeaderFields, services, editItem, isEdit);
            var headerGrid = DialogRenderer.BuildGrid(def.HeaderFields, 2, headerControls, width: null);
            headerGrid.HorizontalAlignment = HorizontalAlignment.Stretch;

            var rows = new List<(Grid Row, Dictionary<string, FrameworkElement> Controls)>();
            var linesHost = new StackPanel();

            void RemoveRow((Grid Row, Dictionary<string, FrameworkElement> Controls) entry)
            {
                linesHost.Children.Remove(entry.Row);
                rows.Remove(entry);
            }

            void AddRow(object lineItem)
            {
                var rowControls = new Dictionary<string, FrameworkElement>();
                var rowGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
                foreach (var lf in def.LineFields)
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(lf.Width) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                int col = 0;
                foreach (var lf in def.LineFields)
                {
                    var fieldDef = new FieldDefinition
                    {
                        Key = lf.Key, LabelKey = "", Kind = lf.Kind, IsRequired = lf.IsRequired,
                        PickerType = lf.PickerType, PickerLeafOnly = lf.PickerLeafOnly,
                        PickerValueField = lf.Kind == FieldKind.Picker ? "Code" : "Id"
                    };
                    var control = DialogRenderer.BuildField(fieldDef);

                    if (lf.Kind == FieldKind.Picker)
                        DialogRenderer.LoadPickerItems((AppComboBox)control, fieldDef, services);

                    if (lineItem != null)
                    {
                        var value = lineItem.GetType().GetProperty(lf.Key)?.GetValue(lineItem);
                        if (lf.Kind == FieldKind.Picker && value != null)
                            DialogRenderer.SelectPickerItem((AppComboBox)control, value, "Code");
                        else
                            DialogRenderer.SetControlValue(control, fieldDef, value);
                    }

                    control.Width = lf.Width - 8;
                    control.Margin = new Thickness(0, 0, 8, 0);
                    Grid.SetColumn(control, col);
                    rowGrid.Children.Add(control);
                    rowControls[lf.Key] = control;
                    col++;
                }

                var removeBtn = new AppIconButton
                {
                    Icon = (Geometry)System.Windows.Application.Current.FindResource("IconDelete"),
                    TooltipText = LocalizationService.Get("Str.RemoveLine"),
                    Variant = "danger"
                };
                Grid.SetColumn(removeBtn, col);
                rowGrid.Children.Add(removeBtn);

                var entry = (rowGrid, rowControls);
                removeBtn.Click += (_, __) => RemoveRow(entry);

                rows.Add(entry);
                linesHost.Children.Add(rowGrid);
            }

            var lineHeaderRow = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            foreach (var lf in def.LineFields)
                lineHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(lf.Width) });
            lineHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            int headerCol = 0;
            foreach (var lf in def.LineFields)
            {
                var label = new TextBlock { Text = lf.Header, Margin = new Thickness(0, 0, 8, 4) };
                label.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
                Grid.SetColumn(label, headerCol);
                lineHeaderRow.Children.Add(label);
                headerCol++;
            }

            if (isEdit)
            {
                var existingLines = editItem.GetType().GetProperty(def.LinesPropertyName)?.GetValue(editItem) as IEnumerable;
                if (existingLines != null)
                    foreach (var line in existingLines) AddRow(line);
            }
            else
            {
                AddRow(null);
                AddRow(null);
            }

            var addLineBtn = new Btn { Text = LocalizationService.Get("Str.AddLine"), Variant = "secondary", Size = "sm", Margin = new Thickness(0, 4, 0, 0) };
            addLineBtn.Click += (_, __) => AddRow(null);

            var linesSection = new StackPanel();
            linesSection.Children.Add(lineHeaderRow);
            linesSection.Children.Add(linesHost);
            linesSection.Children.Add(addLineBtn);

            var body = new StackPanel();
            body.Children.Add(headerGrid);
            body.Children.Add(new Separator { Margin = new Thickness(0, 4, 0, 12) });
            body.Children.Add(linesSection);

            var btnCancel = new Btn { Text = LocalizationService.Get("Str.Cancel"), Variant = "secondary", Size = "sm" };
            var btnSave = new Btn { Text = LocalizationService.Get("Str.Save"), Variant = "primary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };

            var title = isEdit ? LocalizationService.Get(def.TitleEditKey) : LocalizationService.Get(def.TitleKey);
            var footer = new StackPanel { Orientation = Orientation.Horizontal, Children = { btnCancel, btnSave } };
            var window = new ComposedDialogWindow(title, body, footer, width: ComputeDialogWidth(def));

            btnCancel.Click += (_, __) => window.Close();
            btnSave.Click += (_, __) =>
            {
                if (!TrySave(def, services, toast, headerControls, rows, editItem, isEdit)) return;
                window.Saved = true;
                window.Close();
            };

            var frame = new DispatcherFrame();
            window.Closed += (_, __) => frame.Continue = false;
            window.Show();
            Dispatcher.PushFrame(frame);

            return window.Saved;
        }

        // يقرّب لأقرب Token عرض موجود بدل رقم ثابت — النافذة الافتراضية (Sm=420) تكفي حواراً مسطّحاً فقط،
        // صف السطور هنا أعرض بكثير (حساب+مدين+دائن+ملاحظات+زر حذف).
        private static double ComputeDialogWidth(DocumentDialogDefinition def)
        {
            var contentWidth = def.LineFields.Sum(lf => lf.Width) + 40 + 64;
            var key = contentWidth <= 560 ? "C.Dialog.Width.Md" : contentWidth <= 760 ? "C.Dialog.Width.Lg" : "C.Dialog.Width.Xl";
            return (double)System.Windows.Application.Current.FindResource(key);
        }

        private static bool TrySave(DocumentDialogDefinition def, IServiceProvider services, IToastService toast,
            Dictionary<string, FrameworkElement> headerControls, List<(Grid Row, Dictionary<string, FrameworkElement> Controls)> rows,
            object editItem, bool isEdit)
        {
            var service = services.GetRequiredService(def.ServiceType);

            var dto = Activator.CreateInstance(def.DtoType);
            DialogRenderer.ApplyFields(def.HeaderFields, headerControls, dto, editOnly: isEdit);

            if (isEdit)
            {
                var idValue = editItem.GetType().GetProperty("Id")?.GetValue(editItem);
                def.DtoType.GetProperty("Id")?.SetValue(dto, idValue);
            }

            var linesList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(def.LineDtoType));
            int lineNo = 1;
            foreach (var (_, controls) in rows)
            {
                var lineDto = Activator.CreateInstance(def.LineDtoType);
                var lineDtoType = lineDto.GetType();
                foreach (var lf in def.LineFields)
                {
                    var prop = lineDtoType.GetProperty(lf.Key);
                    if (prop == null || !prop.CanWrite) continue;

                    var value = DialogRenderer.GetControlValue(controls[lf.Key], lf.Kind);
                    if (value == null) continue;
                    prop.SetValue(lineDto, Convert.ChangeType(value, prop.PropertyType));
                }
                lineDtoType.GetProperty("LineNo")?.SetValue(lineDto, lineNo++);
                linesList.Add(lineDto);
            }
            def.DtoType.GetProperty(def.LinesPropertyName).SetValue(dto, linesList);

            var method = def.ServiceType.GetMethod(isEdit ? "Update" : "Create", new[] { def.DtoType });
            var result = (Result)method.Invoke(service, new[] { dto });
            if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return false; }

            toast.Success(LocalizationService.Get("Str.Success"));
            return true;
        }
    }
}
