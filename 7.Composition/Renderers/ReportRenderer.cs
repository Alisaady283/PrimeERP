using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Services;

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
                var fieldDef = new FieldDefinition { Key = p.Key, LabelKey = p.LabelKey, Kind = p.Kind, PickerType = p.PickerType, PickerCategoryModuleKey = p.PickerCategoryModuleKey };
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

            void RunReport()
            {
                var paramValues = new Dictionary<string, object>();
                foreach (var p in report.Parameters)
                    paramValues[p.Key] = DialogRenderer.GetControlValue(controls[p.Key], p.Kind);

                var result = report.Generate(services, paramValues);
                if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return; }

                resultGrid.ColumnsSource = result.Value.Columns;
                resultGrid.ItemsSource = result.Value.Rows;
                totalsText.Text = result.Value.Totals is { Count: > 0 }
                    ? string.Join("   |   ", result.Value.Totals.Values)
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
    }
}
