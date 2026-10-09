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
    /// <summary>تصيير التقرير</summary>
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

            PrimeERP.Application.Reporting.ReportData current = null;
            string currentTitle = null;
            List<object> Rows() => ((System.Collections.IEnumerable)current.Rows).Cast<object>().ToList();

            var view = $"{definition.PermissionPrefix}.View";
            header.ActionsContent = new PrimeERP.UI.Components.Actions.ActionToolbar
            {
                ButtonsSource = new List<PrimeERP.UI.Components.Actions.ToolbarAction>
                {
                    PrimeERP.UI.Components.Actions.ToolbarAction.Print(new PrimeERP.UI.ViewModels.RelayCommand(
                        _ => ListOutput.Print(services, currentTitle, report.Columns, Rows(), current.Totals),
                        _ => current != null), view, LocalizationService.Get("Str.Output.PrintReport")),

                    PrimeERP.UI.Components.Actions.ToolbarAction.Export(new PrimeERP.UI.ViewModels.RelayCommand(
                        _ => ListOutput.Export(services, currentTitle, report.Columns, Rows(), current.Totals),
                        _ => current != null), view, LocalizationService.Get("Str.Output.ExportReport")),
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

                if (report.TitleOverrideTotalKey != null &&
                    current.Totals.TryGetValue(report.TitleOverrideTotalKey, out var suffix))
                    currentTitle = $"{currentTitle} — {suffix}";

                resultGrid.RowHighlightSelector = report.RowKind;
                resultGrid.UseAlternatingRows = report.AlternatingRows;
                resultGrid.ColumnsSource = report.Columns;
                resultGrid.ItemsSource = current.Rows;
                resultGrid.SummaryText = current.Totals is { Count: > 0 }
                    ? string.Join("   |   ", current.Totals
                        .Where(t => t.Key != report.TitleOverrideTotalKey).Select(t => t.Value))
                    : "";
            }

            runButton.Click += (_, __) => RunReport();

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(header, 0);
            Grid.SetRow(paramPanel, 1);
            Grid.SetRow(resultGrid, 2);
            root.Children.Add(header);
            root.Children.Add(paramPanel);
            root.Children.Add(resultGrid);

            root.Loaded += (_, __) => RunReport();

            return root;
        }

        public static Result<PrimeERP.Application.Reporting.ReportData> Run(
            ReportDefinition report, IServiceProvider services, Dictionary<string, object> parameters)
        {
            var service = services.GetRequiredService(report.ServiceType);
            var method = report.ServiceType.GetMethod(report.Method);

            if (method == null)
                return Result.Fail<PrimeERP.Application.Reporting.ReportData>(
                    LocalizationService.Get("Str.Composition.MissingMethod", report.ServiceType.Name, report.Method));

            if (report.FixedArguments != null)
                foreach (var fixedArgument in report.FixedArguments) parameters[fixedArgument.Key] = fixedArgument.Value;

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
