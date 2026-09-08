using System;
using System.Collections;
using System.Collections.Generic;
using PrimeERP.Domain.Helpers;
using System.ComponentModel;
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
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    // محرر مستند رأس+سطور عام (قيد يومية اليوم، فواتير لاحقاً) — يعيد استخدام بناء/تعبئة/قراءة حقول
    // DialogRenderer للرأس، ويبني شبكة سطور متكررة (إضافة/حذف صف) بنفس آلية FieldKind لكل عمود. لا رصيد
    // حيّ في الواجهة عمداً — الخادم (JournalValidator) يرفض القيد غير المتوازن برسالة واضحة عند الحفظ.
    public static class DocumentRenderer
    {
        // سطر واحد في شبكة السطور — Link غير فارغ يعني أن السطر جاء بسحب من مستند آخر، فتُنسَخ حقوله
        // الأربعة على DTO السطر عند الحفظ فيسجّل الرابط في DocumentLinks.
        internal class EditorRow
        {
            public Grid Row;
            public Dictionary<string, FrameworkElement> Controls;
            public PullDialog.PulledLine Link;
        }

        internal class DocumentEditor
        {
            public FrameworkElement Body;
            public Dictionary<string, FrameworkElement> HeaderControls;
            public List<EditorRow> Rows;
            public object EditItem;
            public bool IsEdit;

            /// <summary>يضيف سطراً جاهزاً للشبكة — نافذة السحب تستهلكها.</summary>
            public Action<PullDialog.PulledLine> AddPulledRow;

            /// <summary>ينسخ قيم رأس المستند المصدر (الطرف/المخزن) للحقول الفارغة في الرأس الحالي.</summary>
            public Action<Dictionary<string, object>> ApplyPulledHeader;
        }

        // بناء المحرِّر مفصول عن غلافه — الحوار والصفحة يستهلكانه معاً بلا تكرار.
        internal static DocumentEditor BuildEditor(DocumentDialogDefinition def, IServiceProvider services, IToastService toast, object editItem)
        {
            bool isEdit = editItem != null;

            // editItem الوارد من شبكة القائمة هو JournalEntryDto (بلا Lines) — يحتاج استبداله بالنسخة
            // الكاملة (JournalEntryDetailDto) قبل بناء السطور. GetById(int) اتفاقية اسم/توقيع موحّدة لأي
            // خدمة مستند تستهلك هذا المحرر.
            if (isEdit)
            {
                var id = (int)editItem.GetType().GetProperty("Id").GetValue(editItem);
                var service0 = services.GetRequiredService(def.ServiceType);
                var getById = DialogRenderer.FindMethod(def.ServiceType, "GetById", typeof(int));
                if (getById == null) { toast.Error($"الخدمة {def.ServiceType.Name} بلا GetById(int)"); return null; }

                var detailResult = (Result)getById.Invoke(service0, new object[] { id });
                if (!detailResult.IsSuccess) { toast.Error(detailResult.ErrorMessage); return null; }
                editItem = detailResult.GetType().GetProperty("Value").GetValue(detailResult);
            }

            var headerControls = DialogRenderer.BuildAndPopulateFields(def.HeaderFields, services, editItem, isEdit);
            var headerGrid = DialogRenderer.BuildGrid(def.HeaderFields, 2, headerControls, width: null);
            headerGrid.HorizontalAlignment = HorizontalAlignment.Stretch;

            var rows = new List<EditorRow>();
            var linesHost = new StackPanel();

            // الإجماليات تتبع كل تغيير: إضافة صف، حذفه، أو تعديل رقم فيه. تُسنَد بعد بناء الشريط أدناه.
            Action refreshTotals = () => { };

            void RemoveRow(EditorRow entry)
            {
                linesHost.Children.Remove(entry.Row);
                rows.Remove(entry);
                refreshTotals();
            }

            EditorRow AddRow(object lineItem)
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
                        PickerValueField = PickerValueFieldOf(def.LineDtoType, lf)
                    };
                    var control = DialogRenderer.BuildField(fieldDef);

                    if (lf.Kind == FieldKind.Picker)
                        DialogRenderer.LoadPickerItems((AppComboBox)control, fieldDef, services);

                    if (lineItem == null && lf.Kind == FieldKind.Date)
                        DialogRenderer.SetControlValue(control, fieldDef, NewRowDate);

                    if (lineItem != null)
                    {
                        var value = lineItem.GetType().GetProperty(lf.Key)?.GetValue(lineItem);
                        if (lf.Kind == FieldKind.Picker && value != null)
                            DialogRenderer.SelectPickerItem((AppComboBox)control, value, PickerValueFieldOf(def.LineDtoType, lf));
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

                WireLineMath(def, rowControls);

                foreach (var key in def.LineTotals?.Keys ?? new List<string>())
                {
                    if (!rowControls.TryGetValue(key, out var totalled) || totalled is not AppNumericBox box) continue;

                    DependencyPropertyDescriptor.FromProperty(AppNumericBox.ValueProperty, typeof(AppNumericBox))
                        .AddValueChanged(box, (_, _) => refreshTotals());
                }

                refreshTotals();

                var entry = new EditorRow { Row = rowGrid, Controls = rowControls };
                removeBtn.Click += (_, __) => RemoveRow(entry);

                rows.Add(entry);
                linesHost.Children.Add(rowGrid);
                return entry;
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

            if (def.LineTotals != null)
            {
                var (totalsBar, refresh) = BuildTotalsBar(def, rows);
                linesSection.Children.Add(totalsBar);
                refreshTotals = refresh;
                refresh();
            }

            var body = new StackPanel();
            var pullBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            body.Children.Add(pullBar);
            body.Children.Add(headerGrid);
            body.Children.Add(new Separator { Margin = new Thickness(0, 4, 0, 12) });
            body.Children.Add(linesSection);

            var editor = new DocumentEditor
            {
                Body = body, HeaderControls = headerControls, Rows = rows,
                EditItem = editItem, IsEdit = isEdit
            };

            // السحب فعل يسبق تعبئة السطور — مكانه أعلى النموذج لا في فوتر الحفظ.
            foreach (var pullButton in BuildPullButtons(def, services, () => editor)) pullBar.Children.Add(pullButton);
            pullBar.Visibility = pullBar.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            editor.ApplyPulledHeader = header =>
            {
                foreach (var (key, value) in header)
                {
                    if (value == null || !headerControls.TryGetValue(key, out var control)) continue;

                    var headerField = def.HeaderFields.FirstOrDefault(f => f.Key == key);
                    if (headerField == null) continue;

                    // لا يُطمس اختيار قائم — السحب يملأ الفارغ فقط.
                    if (DialogRenderer.GetControlValue(control, headerField.Kind) != null) continue;

                    if (headerField.Kind == FieldKind.Picker)
                        DialogRenderer.SelectPickerItem((AppComboBox)control, value, headerField.PickerValueField);
                    else
                        DialogRenderer.SetControlValue(control, headerField, value);
                }
            };

            editor.AddPulledRow = pulled =>
            {
                // آخر صف فارغ (بلا صنف) يُستهلَك بدل تركه معلّقاً أسفل السطور المسحوبة.
                var target = rows.LastOrDefault(r => IsEmptyRow(r, def)) ?? AddRow(null);
                target.Link = pulled;

                SetRowValue(target, def, "ProductCode", pulled.ProductCode);
                SetRowValue(target, def, "Qty", pulled.Qty);
                SetRowValue(target, def, "UnitPrice", pulled.UnitValue);
                SetRowValue(target, def, "UnitCost", pulled.UnitValue);
            };

            return editor;
        }

        /// <summary>
        /// شريط الإجماليات أسفل السطور: مجموع كل مفتاح بعنوان عموده، ويظهر الفرق بلون التحذير متى اختلّ
        /// التوازن. يقرأ الخانات المعروضة نفسها، فما يراه المُدخِل هو ما يُرسَل للخدمة.
        /// </summary>
        private static (FrameworkElement Bar, Action Refresh) BuildTotalsBar(DocumentDialogDefinition def, List<EditorRow> rows)
        {
            var bar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var values = new Dictionary<string, TextBlock>();

            TextBlock AddCell(string caption, out TextBlock valueBlock)
            {
                var label = new TextBlock { Text = caption + ": ", VerticalAlignment = VerticalAlignment.Center };
                label.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");

                valueBlock = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 20, 0) };
                valueBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
                valueBlock.SetResourceReference(TextBlock.FontWeightProperty, "P.Font.Weight.Bold");

                bar.Children.Add(label);
                bar.Children.Add(valueBlock);
                return label;
            }

            foreach (var key in def.LineTotals.Keys)
            {
                var caption = def.LineFields.FirstOrDefault(f => f.Key == key)?.Header ?? key;
                AddCell(caption, out var cell);
                values[key] = cell;
            }

            TextBlock differenceLabel = null, difference = null;
            if (def.LineTotals.MustBalance is { Length: 2 })
                differenceLabel = AddCell("الفرق", out difference);

            decimal SumOf(string key) => rows
                .Select(r => r.Controls.TryGetValue(key, out var c) && c is AppNumericBox box ? box.Value : 0m)
                .Sum();

            void Refresh()
            {
                foreach (var (key, cell) in values) cell.Text = SumOf(key).ToString("N2");

                if (difference == null) return;

                var gap = SumOf(def.LineTotals.MustBalance[0]) - SumOf(def.LineTotals.MustBalance[1]);
                difference.Text = Math.Abs(gap).ToString("N2");

                // الصفر ليس تحذيراً — اللون يفرّق بين قيدٍ متزن وآخر ينقصه مبلغ.
                var brush = gap == 0m ? "TextSecondary" : "Danger";
                difference.SetResourceReference(TextBlock.ForegroundProperty, brush);
                differenceLabel.SetResourceReference(TextBlock.ForegroundProperty, brush);
            }

            return (bar, Refresh);
        }

        /// <summary>يربط صافي السطر بمدخلاته: أي تغيير في الكمية أو السعر أو النِّسَب يعيد الحساب فوراً
        /// بنفس DocumentTotals التي يرحّل بها الحفظ — فما يراه المستخدم هو ما يُخزَّن، لا معادلة ثانية.</summary>
        private static void WireLineMath(DocumentDialogDefinition def, Dictionary<string, FrameworkElement> controls)
        {
            var math = def.LineMath;
            if (math == null || !controls.TryGetValue(math.NetKey, out var netControl) || netControl is not AppTextBox net) return;

            decimal Value(string key) =>
                key != null && controls.TryGetValue(key, out var control) && control is AppNumericBox box ? box.Value : 0m;

            void Recalculate() => net.Text = DocumentTotals.ForLine(
                Value(math.QtyKey), Value(math.PriceKey),
                Value(math.DiscountPercentKey), Value(math.VatPercentKey), Value(math.WithholdingPercentKey))
                .Net.ToString("N2");

            foreach (var key in new[] { math.QtyKey, math.PriceKey, math.DiscountPercentKey, math.VatPercentKey, math.WithholdingPercentKey })
            {
                if (key == null || !controls.TryGetValue(key, out var control) || control is not AppNumericBox box) continue;

                // AppNumericBox.Value خاصية اعتمادية بلا حدث عام — هذا الأسلوب القياسي للاستماع لها.
                DependencyPropertyDescriptor.FromProperty(AppNumericBox.ValueProperty, typeof(AppNumericBox))
                    .AddValueChanged(box, (_, _) => Recalculate());
            }

            Recalculate();
        }

        /// <summary>
        /// التاريخ الذي يُملأ به حقل تاريخٍ على صفٍّ جديد. تقرؤه التعبئة و IsBlankRow معاً فلا يفترقان:
        /// قيمةٌ وضعها المُصيِّر ليست إدخالاً من المستخدم.
        /// </summary>
        private static DateTime NewRowDate => DateTime.Today;

        // فارغ = كل حقوله بلا قيمة فعلية (نص فارغ/صفر/بلا اختيار/تاريخ لم يلمسه أحد).
        private static bool IsBlankRow(EditorRow row, DocumentDialogDefinition def)
        {
            foreach (var lf in def.LineFields)
            {
                var value = DialogRenderer.GetControlValue(row.Controls[lf.Key], lf.Kind);
                if (value == null) continue;
                if (value is string text && string.IsNullOrWhiteSpace(text)) continue;
                if (value is decimal number && number == 0) continue;
                // مستند سطوره تحمل تاريخاً (استحقاق الشيك) كان يفتح بصفّين "غير فارغين" لأن المُصيِّر ملأ
                // تاريخهما — فيُرسَل صفٌّ لم يُلمَس وتردّ الخدمة الحفظ كله بـ"رقم الشيك مطلوب".
                if (value is DateTime date && date == NewRowDate) continue;
                return false;
            }
            return true;
        }

        private static bool IsEmptyRow(EditorRow row, DocumentDialogDefinition def)
        {
            var key = def.LineFields.FirstOrDefault(f => f.Kind == FieldKind.Picker)?.Key;
            return key != null && DialogRenderer.GetControlValue(row.Controls[key], FieldKind.Picker) == null;
        }

        /// <summary>
        /// القائمة تُرجع ما تطلبه خانة الـDTO لا ما يفترضه المُصيِّر: خانة نصية تأخذ الكود ورقمية تأخذ
        /// المُعرِّف. كان السطر يفرض "Code" على كل قائمة — وهو صحيح للصنف والحساب والموظف لأن خاناتها نصّية،
        /// وخاطئ لقائمة الطرف في مستند الشيكات (PartyId رقم) فكان الحفظ يسقط بمحاولة تحويل كود العميل لرقم.
        /// </summary>
        private static string PickerValueFieldOf(Type lineDtoType, LineFieldDefinition field)
        {
            if (field.Kind != FieldKind.Picker) return "Id";
            if (field.PickerValueField != null) return field.PickerValueField;

            var property = lineDtoType.GetProperty(field.Key);
            if (property == null) return "Code";

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            return type == typeof(string) ? "Code" : "Id";
        }

        private static void SetRowValue(EditorRow row, DocumentDialogDefinition def, string key, object value)
        {
            var field = def.LineFields.FirstOrDefault(f => f.Key == key);
            if (field == null || !row.Controls.TryGetValue(key, out var control)) return;

            if (field.Kind == FieldKind.Picker)
                DialogRenderer.SelectPickerItem((AppComboBox)control, value, PickerValueFieldOf(def.LineDtoType, field));
            else
                DialogRenderer.SetControlValue(control, new FieldDefinition { Key = key, LabelKey = "", Kind = field.Kind }, value);
        }

        // زر لكل مصدر سحب مُعلَن على الوحدة — يقرأ حقول المطابقة من رأس المستند الحالي وقت الضغط لا وقت البناء.
        internal static List<Btn> BuildPullButtons(DocumentDialogDefinition def, IServiceProvider services, Func<DocumentEditor> current)
        {
            var permissions = services.GetRequiredService<IPermissionService>();
            var buttons = new List<Btn>();

            foreach (var source in def.PullSources ?? new List<PullSource>())
            {
                if (!string.IsNullOrEmpty(source.PermissionKey) && !permissions.Can(source.PermissionKey)) continue;

                var button = new Btn { Text = source.Label, Variant = "secondary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
                var captured = source;
                button.Click += (_, __) =>
                {
                    var editor = current();
                    if (editor == null) return;

                    var match = new Dictionary<string, object>();
                    foreach (var field in def.HeaderFields)
                        if (editor.HeaderControls.TryGetValue(field.Key, out var control))
                            match[field.Key] = DialogRenderer.GetControlValue(control, field.Kind);

                    var picked = PullDialog.Show(captured, services, match);
                    if (picked == null) return;

                    editor.ApplyPulledHeader(picked.Header);
                    foreach (var line in picked.Lines) editor.AddPulledRow(line);
                };
                buttons.Add(button);
            }

            return buttons;
        }

        internal static bool TrySaveEditor(DocumentDialogDefinition def, IServiceProvider services, IToastService toast, DocumentEditor editor) =>
            TrySave(def, services, toast, editor.HeaderControls, editor.Rows, editor.EditItem, editor.IsEdit);

        public static bool ShowAndSave(DocumentDialogDefinition def, IServiceProvider services, IToastService toast, object editItem = null)
        {
            var editor = BuildEditor(def, services, toast, editItem);
            if (editor == null) return false;

            var btnCancel = new Btn { Text = LocalizationService.Get("Str.Cancel"), Variant = "secondary", Size = "sm" };
            var btnSave = new Btn { Text = LocalizationService.Get("Str.Save"), Variant = "primary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };

            var title = editor.IsEdit ? LocalizationService.Get(def.TitleEditKey) : LocalizationService.Get(def.TitleKey);
            var footer = new StackPanel { Orientation = Orientation.Horizontal, Children = { btnCancel, btnSave } };
            var window = new ComposedDialogWindow(title, editor.Body, footer, width: ComputeDialogWidth(def));

            btnCancel.Click += (_, __) => window.Close();
            btnSave.Click += (_, __) =>
            {
                if (!TrySave(def, services, toast, editor.HeaderControls, editor.Rows, editor.EditItem, editor.IsEdit)) return;
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
            var key = contentWidth <= 560 ? "C.Dialog.Width.Md"
                    : contentWidth <= 760 ? "C.Dialog.Width.Lg"
                    : contentWidth <= 1000 ? "C.Dialog.Width.Xl" : "C.Dialog.Width.Xxl";
            return (double)System.Windows.Application.Current.FindResource(key);
        }

        private static bool TrySave(DocumentDialogDefinition def, IServiceProvider services, IToastService toast,
            Dictionary<string, FrameworkElement> headerControls, List<EditorRow> rows, object editItem, bool isEdit)
        {
            if (!FieldValidation.Validate(def.HeaderFields, headerControls)) return false;

            var service = services.GetRequiredService(def.ServiceType);

            var dto = Activator.CreateInstance(def.DtoType);
            DialogRenderer.ApplyFields(def.HeaderFields, headerControls, dto, editOnly: isEdit);

            if (isEdit)
            {
                var idValue = editItem.GetType().GetProperty("Id")?.GetValue(editItem);
                def.DtoType.GetProperty("Id")?.SetValue(dto, idValue);
            }

            var linesList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(def.LineDtoType));
            var lineFieldDefs = def.LineFields
                .Select(lf => new FieldDefinition { Key = lf.Key, LabelKey = lf.Header, Kind = lf.Kind, IsRequired = lf.IsRequired })
                .ToList();
            var rowsValid = true;
            int lineNo = 1;
            foreach (var row in rows)
            {
                // المحرِّر يفتح بصفّين فارغين افتراضياً — إرسالهما للخدمة يفشل الحفظ كله برسالة "الصنف غير موجود"
                // على صف لم يلمسه المستخدم أصلاً. الصف الفارغ يُتجاهَل، والفراغ الكامل يُرفض برسالة واضحة أدناه.
                if (IsBlankRow(row, def)) continue;

                // الإلزام على السطر كان مُعلَناً وغير مفحوص: التحقق يمرّ على الرأس وحده، فيُحفظ شيك بلا
                // طرف ولا بنك رغم أن حقليهما مُعلَنان مطلوبان. نفس المُتحقِّق يفحص السطر الآن.
                if (!FieldValidation.Validate(lineFieldDefs, row.Controls)) rowsValid = false;

                var lineDto = Activator.CreateInstance(def.LineDtoType);
                var lineDtoType = lineDto.GetType();
                foreach (var lf in def.LineFields)
                {
                    var prop = lineDtoType.GetProperty(lf.Key);
                    if (prop == null || !prop.CanWrite) continue;

                    var value = DialogRenderer.GetControlValue(row.Controls[lf.Key], lf.Kind);
                    if (value == null) continue;
                    prop.SetValue(lineDto, Convert.ChangeType(value, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType));
                }
                lineDtoType.GetProperty("LineNo")?.SetValue(lineDto, lineNo++);

                if (row.Link != null && lineDto is PrimeERP.Application.DTOs.Documents.IPullableLine pullable)
                {
                    pullable.SourceType   = row.Link.SourceType;
                    pullable.SourceId     = row.Link.SourceId;
                    pullable.SourceNo     = row.Link.SourceNo;
                    pullable.SourceLineId = row.Link.SourceLineId;
                }

                linesList.Add(lineDto);
            }
            // كل الصفوف تُفحص قبل الخروج، فيرى المستخدم كل نواقصه دفعةً لا واحداً بعد واحد.
            if (!rowsValid) return false;
            if (linesList.Count == 0) { toast.Error("المستند يحتاج سطراً واحداً على الأقل"); return false; }

            def.DtoType.GetProperty(def.LinesPropertyName).SetValue(dto, linesList);

            var method = DialogRenderer.FindMethod(def.ServiceType, isEdit ? "Update" : "Create", def.DtoType);
            if (method == null) { toast.Error($"الخدمة {def.ServiceType.Name} بلا {(isEdit ? "Update" : "Create")}({def.DtoType.Name})"); return false; }

            var result = (Result)method.Invoke(service, new[] { dto });
            if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return false; }

            toast.Success(LocalizationService.Get("Str.Success"));
            return true;
        }
    }
}
