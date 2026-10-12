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

            var (paramPanel, readParameters) = ReportParameters.Build(report.Parameters, services);

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
                var result = Run(report, services, readParameters());
                if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return; }

                current = result.Value;
                currentTitle = LocalizationService.Get(report.TitleKey);

                if (report.TitleOverrideTotalKey != null &&
                    current.Totals.TryGetValue(report.TitleOverrideTotalKey, out var suffix))
                    currentTitle = $"{currentTitle} — {suffix}";

                resultGrid.RowHighlightSelector = report.RowKind;
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
            if (report.FixedArguments != null)
                foreach (var fixedArgument in report.FixedArguments) parameters[fixedArgument.Key] = fixedArgument.Value;

            return ReportParameters.Invoke<PrimeERP.Application.Reporting.ReportData>(report.ServiceType, report.Method, report.Arguments, parameters, services);
        }
    }
}
