using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Pull;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>نافذة "سحب من": مستند مصدر ← سطوره غير المسحوبة بالكامل، بكميّة قابلة للتعديل حتى المتبقي.
    /// لا تعرف نوع مستند بعينه — تستهلك PullService العام وتُعيد سطوراً جاهزة للإضافة لمحرِّر المستند الحالي.</summary>
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

        public static List<PulledLine> Show(PullSource source, IServiceProvider services, IDictionary<string, object> matchValues)
        {
            var toast = services.GetRequiredService<IToastService>();
            var result = services.GetRequiredService<IPullService>().GetAvailable(source, matchValues);
            if (result.IsFailure) { toast.Error(result.ErrorMessage); return null; }
            if (result.Value.Count == 0) { toast.Info("لا توجد مستندات متاحة للسحب"); return null; }

            var picker = new AppComboBox
            {
                Placeholder = source.Label, DisplayMemberPath = nameof(PullCandidate.Display),
                ItemsSource = result.Value, Margin = new Thickness(0, 0, 0, 12)
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
                    var name  = new TextBlock { Text = $"{line.ProductCode} - {line.ProductName}", VerticalAlignment = VerticalAlignment.Center };
                    var info  = new TextBlock { Text = $"{line.OriginalQty:N2} / {line.PulledQty:N2}", VerticalAlignment = VerticalAlignment.Center };
                    var qty   = new AppNumericBox { Value = line.RemainingQty, Width = 90 };
                    info.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");

                    Place(grid, check, 0); Place(grid, name, 1); Place(grid, info, 2); Place(grid, qty, 3);
                    linesHost.Children.Add(grid);
                    rows.Add((line, check, qty));
                }
            }

            picker.SelectionChanged += (_, __) => ShowLines(picker.SelectedItem as PullCandidate);
            picker.SelectedItem = result.Value[0];
            ShowLines(result.Value[0]);

            var body = new StackPanel { Margin = new Thickness(4) };
            body.Children.Add(picker);
            body.Children.Add(new ScrollViewer { Content = linesHost, MaxHeight = 360, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });

            var btnCancel = new Btn { Text = LocalizationService.Get("Str.Cancel"), Variant = "secondary", Size = "sm" };
            var btnPull   = new Btn { Text = "سحب", Variant = "primary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
            var footer    = new StackPanel { Orientation = Orientation.Horizontal, Children = { btnCancel, btnPull } };

            var window = new ComposedDialogWindow(source.Label, body, footer,
                width: (double)System.Windows.Application.Current.FindResource("C.Dialog.Width.Lg"));

            List<PulledLine> picked = null;
            btnCancel.Click += (_, __) => window.Close();
            btnPull.Click += (_, __) =>
            {
                var candidate = picker.SelectedItem as PullCandidate;
                if (candidate == null) return;

                var over = rows.FirstOrDefault(r => r.Check.IsChecked == true && r.Qty.Value > r.Line.RemainingQty);
                if (over.Line != null)
                {
                    toast.Error($"الكمية تتجاوز المتبقي ({over.Line.RemainingQty:N2}) للصنف {over.Line.ProductCode}");
                    return;
                }

                picked = rows
                    .Where(r => r.Check.IsChecked == true && r.Qty.Value > 0)
                    .Select(r => new PulledLine
                    {
                        SourceType = candidate.SourceType, SourceId = candidate.SourceId, SourceNo = candidate.SourceNo,
                        SourceLineId = r.Line.SourceLineId, ProductCode = r.Line.ProductCode,
                        Qty = r.Qty.Value, UnitValue = r.Line.UnitValue
                    })
                    .ToList();

                if (picked.Count == 0) { toast.Error("لم يُحدَّد أي سطر"); picked = null; return; }
                window.Close();
            };

            var frame = new System.Windows.Threading.DispatcherFrame();
            window.Closed += (_, __) => frame.Continue = false;
            window.Show();
            System.Windows.Threading.Dispatcher.PushFrame(frame);

            return picked;
        }

        private static readonly double[] Widths = { 32, 300, 110, 100 };

        private static Grid NewRowGrid()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            foreach (var w in Widths) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(w) });
            return grid;
        }

        private static Grid HeaderRow()
        {
            var grid = NewRowGrid();
            var headers = new[] { "", "الصنف", "الأصلي / المسحوب", "الكمية" };
            for (int i = 0; i < headers.Length; i++)
            {
                var text = new TextBlock { Text = headers[i], Margin = new Thickness(0, 0, 8, 4) };
                text.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
                Place(grid, text, i);
            }
            return grid;
        }

        private static void Place(Grid grid, FrameworkElement element, int column)
        {
            element.Margin = new Thickness(0, 0, 8, 0);
            Grid.SetColumn(element, column);
            grid.Children.Add(element);
        }
    }
}
