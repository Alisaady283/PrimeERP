using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.Components.Display
{
    /// <summary>سطرٌ في السلّة: مفتاحه، واسمه، وسعره نصّاً، وكميته، وصافيه نصّاً</summary>
    public sealed record CartLine(object Key, string Title, string Price, decimal Qty, string Total);

    /// <summary>
    /// سلّةٌ مضغوطة — نظير سلّة نقطة بيع الويب: الاسم والسعر · − الكمية + · الصافي · حذف.
    /// لا تعرف المستند: سطورها من خاصيتها، وأفعالها أحداثٌ بمفتاح السطر. الجديد يدخل منزلقاً بلون الهوية الخفيف.
    /// </summary>
    public class AppCartList : UserControl
    {
        public static readonly DependencyProperty LinesProperty =
            DependencyProperty.Register(nameof(Lines), typeof(IEnumerable<CartLine>), typeof(AppCartList), new PropertyMetadata(null, (d, _) => ((AppCartList)d).Rebuild()));

        public static readonly DependencyProperty EmptyTextProperty =
            DependencyProperty.Register(nameof(EmptyText), typeof(string), typeof(AppCartList), new PropertyMetadata("—"));

        public IEnumerable<CartLine> Lines { get => (IEnumerable<CartLine>)GetValue(LinesProperty); set => SetValue(LinesProperty, value); }
        public string EmptyText { get => (string)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

        /// <summary>تغيير الكمية بمقدار (+1 أو −1)</summary>
        public event EventHandler<(object Key, int Step)> Step;
        public event EventHandler<object> Remove;

        private readonly StackPanel _list = new();
        private HashSet<object> _known = new();

        public AppCartList()
        {
            Content = new ScrollViewer { Content = _list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }

        private void Rebuild()
        {
            _list.Children.Clear();
            var lines = Lines?.ToList() ?? new List<CartLine>();
            if (lines.Count == 0)
            {
                var empty = Text(EmptyText, "P.Font.Size.200", "TextTertiary", "P.Font.Weight.Normal");
                empty.HorizontalAlignment = HorizontalAlignment.Center;
                empty.Margin = new Thickness(0, 24, 0, 24);
                _list.Children.Add(empty);
            }

            foreach (var line in lines)
            {
                var row = Row(line);
                _list.Children.Add(row);
                if (!_known.Contains(line.Key)) Arrive(row);
            }
            _known = lines.Select(l => l.Key).ToHashSet();
        }

        /// <summary>السطر الجديد: ينزلق من البداية بخلفية الهوية الخفيفة ثم تخفت — كسطر سلّة الويب</summary>
        private static void Arrive(Border row)
        {
            Motion.Enter(row, rise: 0);
            var shift = new TranslateTransform(16, 0);
            row.RenderTransform = shift;
            shift.BeginAnimation(TranslateTransform.XProperty, Motion.FromTo(16, 0, Motion.Medium2, Motion.Decelerate));
            if (row.TryFindResource("BrandSoft") is SolidColorBrush soft)
            {
                var wash = new SolidColorBrush(soft.Color);
                row.Background = wash;
                wash.BeginAnimation(SolidColorBrush.ColorProperty, new System.Windows.Media.Animation.ColorAnimation(Colors.Transparent, Motion.Long2) { BeginTime = Motion.Medium2 });
            }
        }

        private static TextBlock Text(string text, string size, string brush, string weight)
        {
            var block = new TextBlock { Text = text ?? "", TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            block.SetResourceReference(TextBlock.FontSizeProperty, size);
            block.SetResourceReference(TextBlock.ForegroundProperty, brush);
            block.SetResourceReference(TextBlock.FontWeightProperty, weight);
            return block;
        }

        private static AppIconButton Icon(string key, string variant = "ghost") =>
            new() { Icon = (Geometry)System.Windows.Application.Current.FindResource(key), Variant = variant };

        private Border Row(CartLine line)
        {
            var grid = new Grid { Margin = new Thickness(4, 8, 4, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var names = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            names.Children.Add(Text(line.Title, "P.Font.Size.300", "TextPrimary", "P.Font.Weight.Medium"));
            names.Children.Add(Text(line.Price, "P.Font.Size.100", "TextTertiary", "P.Font.Weight.Normal"));

            var minus = Icon("IconMinus");
            var plus = Icon("IconAdd");
            var qty = Text(line.Qty.ToString("0.##"), "P.Font.Size.300", "TextPrimary", "P.Font.Weight.SemiBold");
            qty.MinWidth = 28;
            qty.TextAlignment = TextAlignment.Center;
            minus.Click += (_, __) => Step?.Invoke(this, (line.Key, -1));
            plus.Click += (_, __) => Step?.Invoke(this, (line.Key, 1));
            var stepper = new Border { Padding = new Thickness(2), VerticalAlignment = VerticalAlignment.Center, Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { minus, qty, plus } } };
            stepper.SetResourceReference(Border.BackgroundProperty, "SurfaceSunken");
            stepper.SetResourceReference(Border.CornerRadiusProperty, "P.Radius.Md");

            var total = Text(line.Total, "P.Font.Size.300", "TextPrimary", "P.Font.Weight.SemiBold");
            total.TextAlignment = TextAlignment.Right;
            total.HorizontalAlignment = HorizontalAlignment.Stretch;

            var trash = Icon("IconDelete", "danger");
            trash.Click += (_, __) => Remove?.Invoke(this, line.Key);

            Grid.SetColumn(stepper, 1);
            Grid.SetColumn(total, 2);
            Grid.SetColumn(trash, 3);
            grid.Children.Add(names);
            grid.Children.Add(stepper);
            grid.Children.Add(total);
            grid.Children.Add(trash);

            var row = new Border { Child = grid, BorderThickness = new Thickness(0, 0, 0, 1) };
            row.SetResourceReference(Border.BorderBrushProperty, "OutlineSubtle");
            return row;
        }
    }
}
