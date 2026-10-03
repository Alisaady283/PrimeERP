using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Pull;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>نافذة "سحب من"</summary>
    public static class PullDialog
    {
        public class PulledLine
        {
            public string  SourceType   { get; init; }
            public int     SourceId     { get; init; }
            public string  SourceNo     { get; init; }
            public int     SourceLineId { get; init; }
            public string  ProductCode  { get; init; }
            public decimal Qty          { get; init; }
            public decimal UnitValue    { get; init; }
        }

        public class PullResult
        {
            public List<PulledLine> Lines { get; init; } = new();

            public Dictionary<string, object> Header { get; init; } = new();
        }

        public static PullResult Show(PullSource source, IServiceProvider services, IDictionary<string, object> matchValues)
        {
            var toast = services.GetRequiredService<IToastService>();
            var result = services.GetRequiredService<IPullService>().GetAvailable(source, matchValues);
            if (result.IsFailure) { toast.Error(result.ErrorMessage); return null; }
            if (result.Value.Count == 0) { toast.Info(LocalizationService.Get("Str.Pull.NoDocuments")); return null; }

            var documents = new AppDataGrid
            {
                ColumnsSource = new List<GridColumn>
                {
                    new() { Header = LocalizationService.Get("Str.Doc"), Binding = nameof(PullCandidate.SourceNo), Width = 130 },
                    new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(PullCandidate.DocDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Party"), Binding = nameof(PullCandidate.PartyName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Pull.OpenLines"), Binding = nameof(PullCandidate.OpenLineCount), Width = 100, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Pull.Remaining"), Binding = nameof(PullCandidate.RemainingQty), Width = 100, Align = ColumnAlign.Center, Format = "N2" },
                },
                ItemsSource = result.Value,
                ShowRowActions = false,
                ShowPagination = false,
                Height = 190
            };

            var linesHost = new StackPanel();
            var rows = new List<(PullCandidateLine Line, AppCheckBox Check, AppNumericBox Qty)>();

            void ShowLines(PullCandidate candidate)
            {
                linesHost.Children.Clear();
                rows.Clear();
                if (candidate == null) return;

                linesHost.Children.Add(HeaderRow());
                foreach (var line in candidate.Lines)
                {
                    var grid = NewRowGrid();
                    var check = new AppCheckBox { IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
                    var name  = new TextBlock { Text = $"{line.ProductCode} - {line.ProductName}", VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
                    var info  = new TextBlock { Text = $"{line.OriginalQty:N2} / {line.PulledQty:N2}", VerticalAlignment = VerticalAlignment.Center };
                    var qty   = new AppNumericBox { Value = line.RemainingQty, Width = 100, Max = line.RemainingQty };
                    name.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
                    info.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");

                    Place(grid, check, 0); Place(grid, name, 1); Place(grid, info, 2); Place(grid, qty, 3);
                    linesHost.Children.Add(grid);
                    rows.Add((line, check, qty));
                }
            }

            documents.SelectionChanged += (_, __) => ShowLines(documents.SelectedItem as PullCandidate);
            documents.SelectedItem = result.Value[0];
            ShowLines(result.Value[0]);

            var body = new Grid { Margin = new Thickness(4) };
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var documentsCaption = Caption(LocalizationService.Get("Str.Pull.Documents"));
            var linesCaption = Caption(LocalizationService.Get("Str.Pull.SelectedLines"));
            var scroll = new ScrollViewer { Content = linesHost, MinHeight = 220, MaxHeight = 320, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

            Grid.SetRow(documentsCaption, 0); Grid.SetRow(documents, 1); Grid.SetRow(linesCaption, 2); Grid.SetRow(scroll, 3);
            body.Children.Add(documentsCaption); body.Children.Add(documents); body.Children.Add(linesCaption); body.Children.Add(scroll);

            var btnCancel = new Btn { Text = LocalizationService.Get("Str.Cancel"), Variant = "secondary", Size = "sm" };
            var btnPull   = new Btn { Text = LocalizationService.Get("Str.Pull.Pull"), Variant = "primary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
            var footer    = new StackPanel { Orientation = Orientation.Horizontal, Children = { btnCancel, btnPull } };

            var window = new ComposedDialogWindow(source.Label, body, footer,
                width: (double)System.Windows.Application.Current.FindResource("C.Dialog.Width.Xl"));

            PullResult picked = null;
            btnCancel.Click += (_, __) => window.Close();
            btnPull.Click += (_, __) =>
            {
                if (documents.SelectedItem is not PullCandidate candidate) { toast.Error(LocalizationService.Get("Str.Document.PickFirst")); return; }

                var over = rows.FirstOrDefault(r => r.Check.IsChecked == true && r.Qty.Value > r.Line.RemainingQty);
                if (over.Line != null)
                {
                    toast.Error(LocalizationService.Get("Str.Pull.Exceeds", over.Line.RemainingQty, over.Line.ProductCode));
                    return;
                }

                var lines = rows
                    .Where(r => r.Check.IsChecked == true && r.Qty.Value > 0)
                    .Select(r => new PulledLine
                    {
                        SourceType = candidate.SourceType, SourceId = candidate.SourceId, SourceNo = candidate.SourceNo,
                        SourceLineId = r.Line.SourceLineId, ProductCode = r.Line.ProductCode,
                        Qty = r.Qty.Value, UnitValue = r.Line.UnitValue
                    })
                    .ToList();

                if (lines.Count == 0) { toast.Error(LocalizationService.Get("Str.Pull.NoLines")); return; }

                picked = new PullResult { Lines = lines, Header = candidate.HeaderValues };
                window.Close();
            };

            var frame = new System.Windows.Threading.DispatcherFrame();
            window.Closed += (_, __) => frame.Continue = false;
            window.Show();
            System.Windows.Threading.Dispatcher.PushFrame(frame);

            return picked;
        }

        private static readonly double[] Widths = { 32, 340, 130, 110 };

        private static Grid NewRowGrid()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            foreach (var width in Widths) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width) });
            return grid;
        }

        private static Grid HeaderRow()
        {
            var grid = NewRowGrid();
            var headers = new[] { "", LocalizationService.Get("Str.Product"), LocalizationService.Get("Str.Pull.OriginalPulled"), LocalizationService.Get("Str.Qty") };
            for (int i = 0; i < headers.Length; i++)
            {
                var text = new TextBlock { Text = headers[i], Margin = new Thickness(0, 0, 8, 4) };
                text.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
                Place(grid, text, i);
            }
            return grid;
        }

        private static TextBlock Caption(string text)
        {
            var caption = new TextBlock { Text = text, Margin = new Thickness(0, 8, 0, 6) };
            caption.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
            return caption;
        }

        private static void Place(Grid grid, FrameworkElement element, int column)
        {
            element.Margin = new Thickness(0, 0, 8, 0);
            Grid.SetColumn(element, column);
            grid.Children.Add(element);
        }
    }
}
