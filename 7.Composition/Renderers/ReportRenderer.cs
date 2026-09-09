using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Services;
using PrimeERP.Domain.Results;

namespace PrimeERP.Composition.Renderers
{
    // معايير + تشغيل + نتيجة — يبني لوحة المعايير بإعادة استخدام DialogRenderer.BuildField/LoadPickerItems/
    // GetControlValue (نفس آلية الحقول المسطّحة)، والنتيجة عبر AppDataGrid العادية (Columns/Rows من
    // ReportDefinition.Generate). صفر XAML جديد.
    public static class ReportRenderer
    {
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services)
        {
            var report = definition.Report;
            var toast = services.GetRequiredService<IToastService>();

            var header = new PageHeader();

            var paramPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(24, 16, 24, 0) };
            var controls = new Dictionary<string, FrameworkElement>();
            foreach (var p in report.Parameters)
            {
                var fieldDef = new FieldDefinition
                {
                    Key = p.Key, LabelKey = p.LabelKey, Kind = p.Kind,
                    PickerType = p.PickerType, PickerCategoryModuleKey = p.PickerCategoryModuleKey,
                    PickerLeafOnly = p.PickerLeafOnly
                };
                var control = DialogRenderer.BuildField(fieldDef);
                if (p.Kind == FieldKind.Picker) DialogRenderer.LoadPickerItems((PrimeERP.UI.Components.Inputs.AppComboBox)control, fieldDef, services);
                if (p.DefaultValue != null) DialogRenderer.SetControlValue(control, fieldDef, p.DefaultValue);
                control.Width = 200;
                control.Margin = new Thickness(0, 0, 12, 0);
                controls[p.Key] = control;
                paramPanel.Children.Add(control);
            }

            var runButton = new PrimeERP.UI.Components.Actions.AppButton { Text = LocalizationService.Get("Str.Report.Run"), Variant = "primary", Size = "sm" };
            paramPanel.Children.Add(runButton);

            var resultGrid = new AppDataGrid { ShowRowActions = false };
            var totalsText = new TextBlock { Margin = new Thickness(24, 8, 24, 8), FontWeight = FontWeights.SemiBold };

            // نتيجة آخر تشغيل — أزرار الطباعة والتصدير تعمل عليها، ومعطَّلة قبل أول تشغيل.
            PrimeERP.Application.Reporting.ReportData current = null;
            string currentTitle = null;
            List<object> Rows() => ((System.Collections.IEnumerable)current.Rows).Cast<object>().ToList();

            var view = $"{definition.PermissionPrefix}.View";
            header.ActionsContent = new PrimeERP.UI.Components.Actions.ActionToolbar
            {
                ButtonsSource = new List<PrimeERP.UI.Components.Actions.ToolbarAction>
                {
                    PrimeERP.UI.Components.Actions.ToolbarAction.Print(new PrimeERP.UI.ViewModels.RelayCommand(
                        _ => ListOutput.Print(services, currentTitle, report.Columns, Rows()),
                        _ => current != null), view, "طباعة التقرير"),

                    PrimeERP.UI.Components.Actions.ToolbarAction.Export(new PrimeERP.UI.ViewModels.RelayCommand(
                        _ => ListOutput.Export(services, currentTitle, report.Columns, Rows()),
                        _ => current != null), view, "تصدير التقرير"),
                }
            };

            void RunReport()
            {
                var paramValues = new Dictionary<string, object>();
                foreach (var p in report.Parameters)
                    paramValues[p.Key] = DialogRenderer.GetControlValue(controls[p.Key], p.Kind);

                var result = Run(report, services, paramValues);
                if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return; }

                current = result.Value;
                currentTitle = LocalizationService.Get(report.TitleKey);

                // عنوان يُكمِّله التقرير نفسه (اسم الصنف في بطاقة الصنف مثلاً) — مفتاحٌ في الإجماليات
                // يُرفَع للعنوان بدل أن يبني كل تقرير عنوانه بكود.
                if (report.TitleOverrideTotalKey != null &&
                    current.Totals.TryGetValue(report.TitleOverrideTotalKey, out var suffix))
                    currentTitle = $"{currentTitle} — {suffix}";

                resultGrid.RowHighlightSelector = report.RowKind;
                resultGrid.UseAlternatingRows = report.AlternatingRows;
                resultGrid.ColumnsSource = report.Columns;
                resultGrid.ItemsSource = current.Rows;
                totalsText.Text = current.Totals is { Count: > 0 }
                    ? string.Join("   |   ", current.Totals
                        .Where(t => t.Key != report.TitleOverrideTotalKey).Select(t => t.Value))
                    : "";
            }

            runButton.Click += (_, __) => RunReport();

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetRow(header, 0);
            Grid.SetRow(paramPanel, 1);
            Grid.SetRow(resultGrid, 2);
            Grid.SetRow(totalsText, 3);
            root.Children.Add(header);
            root.Children.Add(paramPanel);
            root.Children.Add(resultGrid);
            root.Children.Add(totalsText);

            root.Loaded += (_, __) => RunReport();

            return root;
        }

        /// <summary>
        /// ينفّذ التقرير المُعلَن: يحلّ خدمته، ويربط وسائط دالته بقيم بارامتراته بالاسم والترتيب، ثم
        /// يستدعيها. لا كود لكل تقرير — طريقة واحدة يستوردها الثلاثة عشر وما يُبنى بعدها.
        /// </summary>
        public static Result<PrimeERP.Application.Reporting.ReportData> Run(
            ReportDefinition report, IServiceProvider services, Dictionary<string, object> parameters)
        {
            var service = services.GetRequiredService(report.ServiceType);
            var method = report.ServiceType.GetMethod(report.Method);

            if (method == null)
                return Result.Fail<PrimeERP.Application.Reporting.ReportData>(
                    $"الخدمة {report.ServiceType.Name} بلا {report.Method}");

            var signature = method.GetParameters();
            var args = new object[signature.Length];

            for (var i = 0; i < signature.Length; i++)
            {
                var key = i < report.Arguments.Length ? report.Arguments[i] : signature[i].Name;
                parameters.TryGetValue(key, out var value);

                var target = Nullable.GetUnderlyingType(signature[i].ParameterType) ?? signature[i].ParameterType;
                args[i] = value == null
                    ? (signature[i].ParameterType.IsValueType && Nullable.GetUnderlyingType(signature[i].ParameterType) == null
                        ? Activator.CreateInstance(signature[i].ParameterType) : null)
                    : Convert.ChangeType(value, target);
            }

            return (Result<PrimeERP.Application.Reporting.ReportData>)method.Invoke(service, args);
        }
    }
}
