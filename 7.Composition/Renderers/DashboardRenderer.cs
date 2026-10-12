using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Analytics;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Components.Charts;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>تصيير لوحة التحليل: بطاقات المؤشرات ثم الرسوم، وكلٌّ يفتح تقريره</summary>
    public static class DashboardRenderer
    {
        private const double ChartHeight = 260;
        private const int Columns = 3;
        private const int PaletteSize = 5;

        /// <summary>الورقة العرضية: عرض الترتيب، ونسبة العرض إلى الارتفاع بعد رأسها، وأدنى ارتفاعٍ للرسم كي يبقى مقروءاً</summary>
        private const double PaperWidth = 1500, LandscapeAspect = 2.0, MinPaperChart = 110;
        private const int PaperColumns = 5;

        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services)
        {
            var dashboard = definition.Dashboard;
            var toast = services.GetRequiredService<IToastService>();
            var settings = services.GetRequiredService<ISettingsProvider>();
            var simplified = settings.Get(SettingKeys.Documents.SimplifiedFlow, true);
            bool Shown(FlowScope scope, Func<ISettingsProvider, bool> when) => scope.Allows(simplified) && (when == null || when(settings));

            var cards = dashboard.Cards.Where(c => Shown(c.FlowScope, c.ShownWhen)).ToList();
            var charts = dashboard.Charts.Where(c => Shown(c.FlowScope, c.ShownWhen)).ToList();

            var header = new PageHeader { Subtitle = dashboard.QuestionKey == null ? null : LocalizationService.Get(dashboard.QuestionKey), HeaderPadding = new Thickness(12, 8, 12, 0) };
            var (paramPanel, readParameters) = ReportParameters.Build(dashboard.Parameters, services);
            var runButton = new Btn { Text = LocalizationService.Get("Str.Report.Run"), Variant = "primary", Size = "sm" };
            paramPanel.Children.Add(runButton);
            paramPanel.Margin = new Thickness(12, 4, 12, 0);

            var cardRows = (int)Math.Ceiling(cards.Count / 6.0);
            var cardGrid = new UniformGrid { Columns = Math.Max((int)Math.Ceiling(cards.Count / (double)Math.Max(cardRows, 1)), 1), Margin = new Thickness(12, 8, 6, 0) };
            var cardViews = new Dictionary<string, AppStatCard>();
            foreach (var (card, index) in cards.Select((c, i) => (c, i)))
            {
                var view = new AppStatCard
                {
                    Title = LocalizationService.Get(card.TitleKey), Color = $"series{index % PaletteSize + 1}", Value = "—", TrendInverted = card.LowerIsBetter,
                    Icon = card.IconKey == null ? null : System.Windows.Application.Current.TryFindResource(card.IconKey) as Geometry,
                    Margin = new Thickness(0, 0, 12, 12)
                };
                if (CanDrill(card.DrillKey, services, simplified)) { view.Cursor = System.Windows.Input.Cursors.Hand; view.Click += (_, __) => Drill(card.DrillKey, services); }
                cardViews[card.Key] = view;
                cardGrid.Children.Add(view);
            }

            var chartViews = new Dictionary<string, (AppChart Chart, DashboardChart Definition)>();
            var questions = new List<TextBlock>();
            var panels = new List<(Border Panel, int Span)>();
            foreach (var chart in charts)
            {
                var (panel, view) = ChartPanel(chart, services, simplified, questions);
                panels.Add((panel, chart.Span));
                chartViews[chart.Key] = (view, chart);
            }
            var chartRows = Pack(panels, Columns);

            void Run()
            {
                var result = ReportParameters.Invoke<AnalyticsData>(dashboard.ServiceType, dashboard.Method, dashboard.Arguments, readParameters(), services);
                if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return; }

                foreach (var card in cards)
                {
                    if (!result.Value.Cards.TryGetValue(card.Key, out var kpi)) continue;
                    var view = cardViews[card.Key];
                    view.Value = Text(kpi.Value, card.Kind);
                    view.Trend = (double?)kpi.Change;
                    view.Subtitle = kpi.Note;
                }
                foreach (var (key, (view, chart)) in chartViews)
                    view.Data = result.Value.Charts.GetValueOrDefault(key);
            }

            runButton.Click += (_, __) => Run();

            var content = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            content.Children.Add(cardGrid);
            foreach (var chartRow in chartRows) content.Children.Add(chartRow);

            string Title() => LocalizationService.Get(definition.TitleKey);
            string Period() => string.Join(" — ", readParameters().Values.OfType<DateTime>().Select(d => d.ToString("yyyy-MM-dd")));
            var wash = System.Windows.Application.Current.TryFindResource("PageWash") as Brush;
            void Arrange(int columns)
            {
                foreach (var row in chartRows) { row.Children.Clear(); content.Children.Remove(row); }
                chartRows = Pack(panels, columns);
                foreach (var row in chartRows) content.Children.Add(row);
            }

            string Sheet()
            {
                var views = chartViews.Values.Select(v => v.Chart).ToList();
                var screenColumns = cardGrid.Columns;
                cardGrid.Columns = Math.Max(cards.Count, 1);
                questions.ForEach(q => q.Visibility = Visibility.Collapsed);
                Arrange(PaperColumns);
                scroll.Content = null;
                var sheet = new Grid { Width = PaperWidth, FlowDirection = scroll.FlowDirection, Children = { content } };
                void Layout()
                {
                    sheet.Measure(new Size(PaperWidth, double.PositiveInfinity));
                    sheet.Arrange(new Rect(0, 0, PaperWidth, sheet.DesiredSize.Height));
                    sheet.UpdateLayout();
                }
                Layout();
                var extra = (PaperWidth / LandscapeAspect - sheet.DesiredSize.Height) / Math.Max(chartRows.Count, 1);
                foreach (var chartView in views) chartView.Height = Math.Clamp(ChartHeight + extra, MinPaperChart, ChartHeight * 1.5);
                Layout();
                try { return VisualSnapshot.Png(sheet, wash); }
                finally
                {
                    sheet.Children.Clear();
                    scroll.Content = content;
                    foreach (var chartView in views) chartView.Height = ChartHeight;
                    cardGrid.Columns = screenColumns;
                    questions.ForEach(q => q.Visibility = Visibility.Visible);
                    Arrange(Columns);
                }
            }

            Domain.Contracts.IPrintable Paper() => Print.PrintDocuments.Snapshot(Title(), Period(), new[] { Sheet() });

            var viewPermission = $"{definition.PermissionPrefix}.View";
            header.ActionsContent = new PrimeERP.UI.Components.Actions.ActionToolbar
            {
                ButtonsSource = new List<PrimeERP.UI.Components.Actions.ToolbarAction>
                {
                    PrimeERP.UI.Components.Actions.ToolbarAction.Print(new PrimeERP.UI.ViewModels.RelayCommand(_ =>
                    {
                        var printed = services.GetRequiredService<IPrintService>().PrintPreview(Paper());
                        if (printed.IsFailure) toast.Error(printed.ErrorMessage);
                    }), viewPermission, LocalizationService.Get("Str.Output.PrintReport")),

                    PrimeERP.UI.Components.Actions.ToolbarAction.Export(new PrimeERP.UI.ViewModels.RelayCommand(_ =>
                    {
                        var dialog = new Microsoft.Win32.SaveFileDialog { FileName = $"{Title()}-{DateTime.Today:yyyy-MM-dd}", Filter = "PDF (*.pdf)|*.pdf" };
                        if (dialog.ShowDialog() != true) return;
                        var exported = services.GetRequiredService<IPrintService>().ExportToPdf(Paper(), dialog.FileName);
                        if (exported.IsFailure) toast.Error(exported.ErrorMessage);
                        else toast.Success(LocalizationService.Get("Str.Output.Exported", System.IO.Path.GetFileName(dialog.FileName)));
                    }), viewPermission, LocalizationService.Get("Str.Output.ExportPdf")),
                }
            };
            scroll.Content = content;

            var root = new Grid();
            root.SetResourceReference(Panel.BackgroundProperty, "PageWash");
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(paramPanel, 1);
            Grid.SetRow(scroll, 2);
            root.Children.Add(header);
            root.Children.Add(paramPanel);
            root.Children.Add(scroll);
            root.Loaded += (_, __) => Run();
            return root;
        }

        /// <summary>صفوفٌ من أعمدةٍ ثابتة: كل رسمٍ بعرضه، والرسم الأخير في صفٍّ ناقص يمتد ليملأه</summary>
        private static List<Grid> Pack(IReadOnlyList<(Border Panel, int Span)> panels, int columns)
        {
            var rows = new List<Grid>();
            var column = columns;
            Border last = null;
            void Fill()
            {
                if (last != null && column < columns) Grid.SetColumnSpan(last, Grid.GetColumnSpan(last) + columns - column);
            }

            foreach (var (panel, requested) in panels)
            {
                var span = Math.Clamp(requested, 1, columns);
                if (column + span > columns)
                {
                    Fill();
                    var row = new Grid { Margin = new Thickness(12, 0, 6, 0) };
                    for (var c = 0; c < columns; c++) row.ColumnDefinitions.Add(new ColumnDefinition());
                    rows.Add(row);
                    column = 0;
                }
                Grid.SetColumn(panel, column);
                Grid.SetColumnSpan(panel, span);
                rows[^1].Children.Add(panel);
                column += span;
                last = panel;
            }
            Fill();
            return rows;
        }

        private static (Border Panel, AppChart Chart) ChartPanel(DashboardChart chart, IServiceProvider services, bool simplified, List<TextBlock> questions)
        {
            var (format, suffix) = Format(chart.Value);
            var view = new AppChart { Kind = chart.Kind, Tone = chart.Tone, Smooth = chart.Smooth, Format = format, Suffix = suffix, Height = ChartHeight, EmptyText = LocalizationService.Get("Str.Analytics.NoData") };

            var title = new TextBlock { Text = LocalizationService.Get(chart.TitleKey), FontSize = 14, FontWeight = FontWeights.SemiBold };
            title.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
            var heading = new DockPanel { LastChildFill = true };
            if (CanDrill(chart.DrillKey, services, simplified))
            {
                var details = new TextBlock { Text = LocalizationService.Get("Str.Analytics.Details"), FontSize = 12, Cursor = System.Windows.Input.Cursors.Hand, VerticalAlignment = VerticalAlignment.Center };
                details.SetResourceReference(TextBlock.ForegroundProperty, "TextLink");
                details.MouseEnter += (_, __) => details.TextDecorations = TextDecorations.Underline;
                details.MouseLeave += (_, __) => details.TextDecorations = null;
                details.MouseLeftButtonUp += (_, __) => Drill(chart.DrillKey, services);
                DockPanel.SetDock(details, Dock.Right);
                heading.Children.Add(details);
            }
            heading.Children.Add(title);
            var stack = new StackPanel();
            stack.Children.Add(heading);
            if (chart.QuestionKey != null)
            {
                var question = new TextBlock { Text = LocalizationService.Get(chart.QuestionKey), FontSize = 12, Margin = new Thickness(0, 2, 0, 10), TextWrapping = TextWrapping.Wrap };
                question.SetResourceReference(TextBlock.ForegroundProperty, "TextMuted");
                questions.Add(question);
                stack.Children.Add(question);
            }
            stack.Children.Add(view);

            var panel = new Border
            {
                Padding = new Thickness(20, 18, 20, 16), Margin = new Thickness(0, 0, 12, 12), BorderThickness = new Thickness(1), Child = stack,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 18, ShadowDepth = 2, Direction = 270, Opacity = 0.06 }
            };
            panel.SetResourceReference(Border.BackgroundProperty, "SurfaceDefault");
            panel.SetResourceReference(Border.BorderBrushProperty, "OutlineDefault");
            panel.SetResourceReference(Border.CornerRadiusProperty, "P.Radius.Lg");

            return (panel, view);
        }

        private static bool CanDrill(string key, IServiceProvider services, bool simplified)
        {
            if (key == null) return false;
            var module = services.GetRequiredService<IModuleRegistry>().Get(key);
            return module != null && module.FlowScope.Allows(simplified)
                && services.GetRequiredService<IPermissionService>().Can($"{module.PermissionPrefix}.View");
        }

        /// <summary>التقرير الذي يفسّر الرقم، في نافذة فوق اللوحة</summary>
        private static void Drill(string key, IServiceProvider services)
        {
            var module = services.GetRequiredService<IModuleRegistry>().Get(key);
            var close = new Btn { Text = LocalizationService.Get("Str.Close"), Variant = "secondary", Size = "sm" };
            var body = new Grid { Height = 620, Children = { PageRenderer.Render(module, services) } };
            var window = new ComposedDialogWindow(LocalizationService.Get(module.TitleKey), body, close, width: 1100);
            close.Click += (_, __) => window.Close();
            window.ShowDialog();
        }

        private static (string Format, string Suffix) Format(ValueKind kind) => kind switch
        {
            ValueKind.Count => ("N0", ""),
            ValueKind.Percent => ("N1", "%"),
            ValueKind.Days => ("N1", ""),
            _ => ("N0", ""),
        };

        private static string Text(decimal value, ValueKind kind) => kind switch
        {
            ValueKind.Money => value.ToString("N2"),
            ValueKind.Count => value.ToString("N0"),
            ValueKind.Percent => $"{value:N1}%",
            _ => LocalizationService.Get("Str.Analytics.DaysValue", value),
        };
    }
}
