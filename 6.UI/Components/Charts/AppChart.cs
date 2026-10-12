using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using PrimeERP.Application.Analytics;

namespace PrimeERP.UI.Components.Charts
{
    public enum ChartKind { Column, Bar, Line, Area, Donut, Gauge }

    /// <summary>Auto: سلسلةٌ واحدة بلون الهوية وأكثر بالترتيب الثابت؛ Severity/Improving: فئاتٌ مرتّبة على تدرّج الحالة</summary>
    public enum ChartTone { Auto, Severity, Improving }

    /// <summary>رسمٌ بياني متحرّك من ChartData بألوان الهوية</summary>
    public class AppChart : UserControl
    {
        public static readonly DependencyProperty KindProperty =
            DependencyProperty.Register(nameof(Kind), typeof(ChartKind), typeof(AppChart), new PropertyMetadata(ChartKind.Column, (d, _) => ((AppChart)d).Animate()));

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(ChartData), typeof(AppChart), new PropertyMetadata(null, (d, _) => ((AppChart)d).Animate()));

        public static readonly DependencyProperty ToneProperty =
            DependencyProperty.Register(nameof(Tone), typeof(ChartTone), typeof(AppChart), new PropertyMetadata(ChartTone.Auto, (d, _) => ((AppChart)d).Draw()));

        public static readonly DependencyProperty SmoothProperty =
            DependencyProperty.Register(nameof(Smooth), typeof(bool), typeof(AppChart), new PropertyMetadata(false, (d, _) => ((AppChart)d).Draw()));

        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register(nameof(Format), typeof(string), typeof(AppChart), new PropertyMetadata("N0", (d, _) => ((AppChart)d).Draw()));

        public static readonly DependencyProperty SuffixProperty =
            DependencyProperty.Register(nameof(Suffix), typeof(string), typeof(AppChart), new PropertyMetadata("", (d, _) => ((AppChart)d).Draw()));

        public static readonly DependencyProperty EmptyTextProperty =
            DependencyProperty.Register(nameof(EmptyText), typeof(string), typeof(AppChart), new PropertyMetadata("—", (d, _) => ((AppChart)d).Draw()));

        private static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(AppChart), new PropertyMetadata(1.0, (d, _) => ((AppChart)d).Draw()));

        public ChartKind Kind { get => (ChartKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
        public ChartData Data { get => (ChartData)GetValue(DataProperty); set => SetValue(DataProperty, value); }
        public ChartTone Tone { get => (ChartTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

        /// <summary>منحنى ناعم أحاديّ الاتجاه بين النقاط بدل الخطوط المستقيمة، لا يخترع قمةً ولا قاعاً</summary>
        public bool Smooth { get => (bool)GetValue(SmoothProperty); set => SetValue(SmoothProperty, value); }
        public string Format { get => (string)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }
        public string Suffix { get => (string)GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }
        public string EmptyText { get => (string)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }
        private double Progress => (double)GetValue(ProgressProperty);

        private static readonly string[] Palette = { "ChartSeries1", "ChartSeries2", "ChartSeries3", "ChartSeries4", "ChartSeries5" };
        private const double AxisWidth = 64, AxisHeight = 26, Gap = 8, MaxColumnWidth = 56, SlotShare = 0.55, SeriesGap = 4;

        private readonly Canvas _canvas = new() { ClipToBounds = true };
        private readonly WrapPanel _legend = new() { Margin = new Thickness(0, 0, 0, 6) };

        public AppChart()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(_canvas, 1);
            root.Children.Add(_legend);
            root.Children.Add(_canvas);
            Content = root;
            SizeChanged += (_, __) => Draw();
            Loaded += (_, __) => Animate();
        }

        private void Animate()
        {
            if (!IsLoaded) return;
            BeginAnimation(ProgressProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(900))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private void Draw()
        {
            _canvas.Children.Clear();
            _legend.Children.Clear();
            var data = Visible(Data);
            if (_canvas.ActualWidth < 40 || _canvas.ActualHeight < 40) return;
            if (data == null || data.Labels.Count == 0 || data.Series.All(s => s.Values.All(v => v == 0)))
            {
                Place(Label(EmptyText, "TextMuted", 13), _canvas.ActualWidth / 2 - 20, _canvas.ActualHeight / 2 - 10);
                return;
            }

            switch (Kind)
            {
                case ChartKind.Donut: DrawDonut(data); break;
                case ChartKind.Gauge: DrawGauge(data); break;
                case ChartKind.Bar: DrawBars(data); break;
                case ChartKind.Line: DrawLines(data, area: false); break;
                case ChartKind.Area: DrawLines(data, area: true); break;
                default: DrawColumns(data); break;
            }
        }

        /// <summary>سلسلة المقارنة الخالية لا تُرسم: خطٌّ صفريّ ضجيجٌ لا معلومة</summary>
        private static ChartData Visible(ChartData data) =>
            data == null || data.Series.All(s => !s.Comparison || s.Values.Any(v => v != 0)) ? data
                : new ChartData { Labels = data.Labels, Series = data.Series.Where(s => !s.Comparison || s.Values.Any(v => v != 0)).ToList() };

        private (double Min, double Max) Range(ChartData data)
        {
            var values = data.Series.SelectMany(s => s.Values).Select(v => (double)v).ToList();
            var max = Nice(Math.Max(values.Max(), 0));
            var min = values.Min() < 0 ? -Nice(-values.Min()) : 0;
            return (min, max == min ? min + 1 : max);
        }

        private void DrawAxis(double min, double max, double left, double top, double width, double height)
        {
            for (var i = 0; i <= 4; i++)
            {
                var value = min + (max - min) * i / 4;
                var y = top + height - height * i / 4;
                _canvas.Children.Add(new Line { X1 = left, X2 = left + width, Y1 = y, Y2 = y, Stroke = Brush("OutlineDefault"), StrokeThickness = i == 0 ? 1 : 0.6, StrokeDashArray = i == 0 ? null : new DoubleCollection { 3, 3 } });
                Place(Label(Short(value), "TextMuted", 11), 0, y - 8, AxisWidth - 6, TextAlignment.Right);
            }
        }

        private void DrawCategories(IReadOnlyList<string> labels, double left, double slot, double y)
        {
            var step = Math.Max(1, (int)Math.Ceiling(labels.Count / Math.Max(1, slot * labels.Count / 64)));
            for (var i = 0; i < labels.Count; i += step)
                Place(Label(labels[i], "TextMuted", 11), left + slot * i, y + 4, slot * step, TextAlignment.Center);
        }

        private void DrawColumns(ChartData data)
        {
            Legend(data);
            var (min, max) = Range(data);
            double left = AxisWidth, top = Gap, width = _canvas.ActualWidth - left - Gap, height = _canvas.ActualHeight - top - AxisHeight;
            DrawAxis(min, max, left, top, width, height);

            var slot = width / data.Labels.Count;
            var count = data.Series.Count;
            var group = Math.Min(slot * SlotShare, MaxColumnWidth * count + SeriesGap * (count - 1));
            var barWidth = Math.Max((group - SeriesGap * (count - 1)) / count, 2);
            var zero = top + height * max / (max - min);
            for (var s = 0; s < count; s++)
            {
                var series = data.Series[s];
                for (var i = 0; i < data.Labels.Count; i++)
                {
                    var value = (double)series.Values.ElementAtOrDefault(i);
                    var length = Math.Abs(value) / (max - min) * height * Progress;
                    var x = left + slot * i + (slot - group) / 2 + (barWidth + SeriesGap) * s;
                    var bar = new Border
                    {
                        Width = barWidth, Height = length, Background = PointBrush(data, s, i), Opacity = series.Comparison ? 0.5 : 1,
                        CornerRadius = value >= 0 ? new CornerRadius(4, 4, 0, 0) : new CornerRadius(0, 0, 4, 4),
                        ToolTip = Tip(data.Labels[i], series, series.Values.ElementAtOrDefault(i))
                    };
                    Hover(bar);
                    Place(bar, x, value >= 0 ? zero - length : zero);
                    if (Progress >= 1 && data.Series.Count == 1 && data.Labels.Count <= 8 && value != 0)
                        Place(Label(Short(value), "TextSecondary", 11), x - 20, (value >= 0 ? zero - length : zero + length) - (value >= 0 ? 17 : -2), barWidth + 40, TextAlignment.Center);
                }
            }
            DrawCategories(data.Labels, left, slot, top + height);
        }

        private void DrawLines(ChartData data, bool area)
        {
            Legend(data);
            var (min, max) = Range(data);
            double left = AxisWidth, top = Gap, width = _canvas.ActualWidth - left - Gap, height = _canvas.ActualHeight - top - AxisHeight;
            DrawAxis(min, max, left, top, width, height);

            var count = data.Labels.Count;
            var slot = width / count;
            double X(int i) => left + slot * i + slot / 2;
            double Y(decimal v) => top + height - ((double)v - min) / (max - min) * height;
            var reach = Progress * (count - 1);

            for (var s = 0; s < data.Series.Count; s++)
            {
                var series = data.Series[s];
                var brush = PointBrush(data, s, -1);
                var points = new PointCollection();
                for (var i = 0; i < count; i++)
                {
                    if (i > reach + 1e-9)
                    {
                        var from = new Point(X(i - 1), Y(series.Values.ElementAtOrDefault(i - 1)));
                        var to = new Point(X(i), Y(series.Values.ElementAtOrDefault(i)));
                        var t = reach - (i - 1);
                        points.Add(new Point(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t));
                        break;
                    }
                    points.Add(new Point(X(i), Y(series.Values.ElementAtOrDefault(i))));
                }

                if (points.Count < 2) continue;
                if (area && !series.Comparison)
                {
                    var fill = Curve(points, Smooth);
                    fill.Segments.Add(new LineSegment(new Point(points[^1].X, Y(0)), false));
                    fill.Segments.Add(new LineSegment(new Point(points[0].X, Y(0)), false));
                    fill.IsClosed = true;
                    _canvas.Children.Add(new Path { Data = new PathGeometry { Figures = { fill } }, Fill = AreaFill(brush) });
                }
                _canvas.Children.Add(new Path
                {
                    Data = new PathGeometry { Figures = { Curve(points, Smooth) } }, Stroke = brush, StrokeThickness = 2.4,
                    StrokeDashArray = series.Comparison ? new DoubleCollection { 4, 3 } : null,
                    StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round
                });

                if (Progress < 1) continue;
                for (var i = 0; i < count; i++)
                {
                    var dot = new Ellipse { Width = 8, Height = 8, Fill = Brush("SurfaceDefault"), Stroke = brush, StrokeThickness = 2, ToolTip = Tip(data.Labels[i], series, series.Values.ElementAtOrDefault(i)) };
                    Hover(dot);
                    Place(dot, X(i) - 4, Y(series.Values.ElementAtOrDefault(i)) - 4);
                }
            }
            DrawCategories(data.Labels, left, slot, top + height);
        }

        private void DrawBars(ChartData data)
        {
            var series = data.Series[0];
            var count = data.Labels.Count;
            var width = _canvas.ActualWidth;
            var labelWidth = Math.Min(180, width * 0.34);
            var valueWidth = 90;
            var row = Math.Min(30, _canvas.ActualHeight / count);
            var thickness = Math.Min(10, row * 0.42);
            var text = Math.Clamp(row * 0.62, 7, 12);
            var max = (double)Math.Max(series.Values.Max(), 1);
            var track = width - labelWidth - valueWidth - Gap * 2;

            for (var i = 0; i < count; i++)
            {
                var value = series.Values.ElementAtOrDefault(i);
                var y = i * row;
                Place(Label(data.Labels[i], "TextPrimary", text, trim: true), 0, y + row / 2 - text * 0.75, labelWidth - Gap, TextAlignment.Left);
                var barTop = y + (row - thickness) / 2;
                _canvas.Children.Add(Positioned(new Border { Width = Math.Max(track, 1), Height = thickness, CornerRadius = new CornerRadius(thickness / 2), Background = Brush("SurfaceSunken") }, labelWidth, barTop));
                var bar = new Border
                {
                    Width = Math.Max(track * Math.Max((double)value, 0) / max * Progress, 0), Height = thickness, CornerRadius = new CornerRadius(thickness / 2),
                    Background = PointBrush(data, 0, i), ToolTip = Tip(data.Labels[i], series, value)
                };
                Hover(bar);
                Place(bar, labelWidth, barTop);
                Place(Label(Value(value), "TextSecondary", text - 1), labelWidth + track + Gap, y + row / 2 - text * 0.7, valueWidth, TextAlignment.Left);
            }
        }

        private void DrawDonut(ChartData data)
        {
            var series = data.Series[0];
            var total = series.Values.Where(v => v > 0).Sum();
            var size = Math.Min(_canvas.ActualHeight, _canvas.ActualWidth * 0.5) - Gap * 2;
            var radius = size / 2;
            var thickness = radius * 0.36;
            var center = new Point(Gap + radius, _canvas.ActualHeight / 2);
            var start = -90.0;

            for (var i = 0; i < data.Labels.Count; i++)
            {
                var value = Math.Max(series.Values.ElementAtOrDefault(i), 0);
                var sweep = total == 0 ? 0 : (double)(value / total) * 360 * Progress;
                if (sweep > 0.2)
                {
                    var slice = new Path { Data = Ring(center, radius, thickness, start, sweep), Fill = PointBrush(data, 0, i), Stroke = Brush("SurfaceDefault"), StrokeThickness = 2, ToolTip = Tip(data.Labels[i], series, value) };
                    Hover(slice);
                    _canvas.Children.Add(slice);
                }
                start += sweep;

                var legendY = _canvas.ActualHeight / 2 - data.Labels.Count * 11 + i * 22;
                var legendX = center.X + radius + Gap * 3;
                Place(new Rectangle { Width = 10, Height = 10, RadiusX = 2, RadiusY = 2, Fill = PointBrush(data, 0, i) }, legendX, legendY + 4);
                Place(Label($"{data.Labels[i]}  {(total == 0 ? 0 : Math.Round(value / total * 100, 1)):N1}%", "TextPrimary", 12, trim: true),
                    legendX + 16, legendY, _canvas.ActualWidth - legendX - 16, TextAlignment.Left);
            }

            Place(Label(Short((double)total), "TextPrimary", 15, bold: true), center.X - radius, center.Y - 11, radius * 2, TextAlignment.Center);
        }

        /// <summary>خطٌّ مستقيم بين النقاط، أو منحنى Fritsch–Carlson الأحادي الذي لا يتجاوز النقاط</summary>
        private static PathFigure Curve(PointCollection p, bool smooth)
        {
            var figure = new PathFigure { StartPoint = p[0] };
            if (!smooth || p.Count < 3)
            {
                for (var i = 1; i < p.Count; i++) figure.Segments.Add(new LineSegment(p[i], true));
                return figure;
            }

            var n = p.Count;
            var d = new double[n - 1];
            for (var k = 0; k < n - 1; k++) d[k] = (p[k + 1].Y - p[k].Y) / Math.Max(p[k + 1].X - p[k].X, 1e-9);
            var m = new double[n];
            m[0] = d[0];
            m[n - 1] = d[n - 2];
            for (var k = 1; k < n - 1; k++) m[k] = d[k - 1] * d[k] <= 0 ? 0 : (d[k - 1] + d[k]) / 2;
            for (var k = 0; k < n - 1; k++)
            {
                if (d[k] == 0) { m[k] = 0; m[k + 1] = 0; continue; }
                var a = m[k] / d[k];
                var b = m[k + 1] / d[k];
                var s = a * a + b * b;
                if (s <= 9) continue;
                var t = 3 / Math.Sqrt(s);
                m[k] = t * a * d[k];
                m[k + 1] = t * b * d[k];
            }
            for (var k = 0; k < n - 1; k++)
            {
                var h = (p[k + 1].X - p[k].X) / 3;
                figure.Segments.Add(new BezierSegment(new Point(p[k].X + h, p[k].Y + m[k] * h), new Point(p[k + 1].X - h, p[k + 1].Y - m[k + 1] * h), p[k + 1], true));
            }
            return figure;
        }

        /// <summary>نسبةٌ واحدة على حلقة: المسار فاتح والمُنجَز بلون الهوية ورأسٌ مستدير</summary>
        private void DrawGauge(ChartData data)
        {
            var value = (double)Math.Clamp(data.Series[0].Values.FirstOrDefault(), 0, 100);
            var size = Math.Min(_canvas.ActualHeight, _canvas.ActualWidth) * 0.78;
            var radius = size / 2;
            var thickness = Math.Max(radius * 0.13, 7);
            var center = new Point(_canvas.ActualWidth / 2, _canvas.ActualHeight / 2);
            var track = new Ellipse { Width = size - thickness, Height = size - thickness, Stroke = Brush("SurfaceSunken"), StrokeThickness = thickness };
            Place(track, center.X - (size - thickness) / 2, center.Y - (size - thickness) / 2);

            var sweep = value / 100 * 360 * Progress;
            if (sweep > 0.5)
            {
                var r = (size - thickness) / 2;
                Point At(double deg) => new(center.X + r * Math.Cos(deg * Math.PI / 180), center.Y + r * Math.Sin(deg * Math.PI / 180));
                var figure = new PathFigure { StartPoint = At(-90), IsClosed = false };
                figure.Segments.Add(new ArcSegment(At(-90 + Math.Min(sweep, 359.99)), new Size(r, r), 0, sweep > 180, SweepDirection.Clockwise, true));
                var arc = new Path { Data = new PathGeometry { Figures = { figure } }, Stroke = PointBrush(data, 0, -1), StrokeThickness = thickness,
                                     StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, ToolTip = Tip(data.Labels[0], data.Series[0], (decimal)value) };
                Hover(arc);
                _canvas.Children.Add(arc);
            }

            Place(Label($"{value * Progress:N1}%", "TextPrimary", Math.Max(radius * 0.32, 16), bold: true), center.X - radius, center.Y - radius * 0.24, radius * 2, TextAlignment.Center);
            Place(Label(data.Series[0].Name, "TextMuted", 12), center.X - radius, center.Y + radius * 0.18, radius * 2, TextAlignment.Center);
        }

        private static Geometry Ring(Point c, double r, double thickness, double startDeg, double sweepDeg)
        {
            sweepDeg = Math.Min(sweepDeg, 359.99);
            Point At(double radius, double deg) => new(c.X + radius * Math.Cos(deg * Math.PI / 180), c.Y + radius * Math.Sin(deg * Math.PI / 180));
            var inner = r - thickness;
            var large = sweepDeg > 180;
            var figure = new PathFigure { StartPoint = At(r, startDeg), IsClosed = true };
            figure.Segments.Add(new ArcSegment(At(r, startDeg + sweepDeg), new Size(r, r), 0, large, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(At(inner, startDeg + sweepDeg), true));
            figure.Segments.Add(new ArcSegment(At(inner, startDeg), new Size(inner, inner), 0, large, SweepDirection.Counterclockwise, true));
            return new PathGeometry { Figures = { figure } };
        }

        private void Legend(ChartData data)
        {
            if (data.Series.Count < 2) return;
            for (var s = 0; s < data.Series.Count; s++)
            {
                var item = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 14, 0) };
                item.Children.Add(new Rectangle { Width = 14, Height = data.Series[s].Comparison ? 3 : 10, RadiusX = 2, RadiusY = 2, Fill = PointBrush(data, s, -1), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
                item.Children.Add(Label(data.Series[s].Name, "TextSecondary", 12));
                _legend.Children.Add(item);
            }
        }

        /// <summary>لون العلامة: المقارنة رمادية، والفئات المرتّبة على تدرّج الحالة، والسلسلة الوحيدة بلون الهوية</summary>
        private Brush PointBrush(ChartData data, int seriesIndex, int pointIndex)
        {
            var series = data.Series[seriesIndex];
            if (series.Comparison) return Brush("TextMuted");
            if (Tone != ChartTone.Auto && pointIndex >= 0)
            {
                var t = data.Labels.Count <= 1 ? 0 : pointIndex / (double)(data.Labels.Count - 1);
                return Ramp(Tone == ChartTone.Improving ? 1 - t : t);
            }
            return Kind == ChartKind.Donut && pointIndex >= 0 ? SeriesBrush(pointIndex, series) : SeriesBrush(seriesIndex, series);
        }

        /// <summary>تدرّج الشدّة: جيد ← تنبيه ← حرج بدرجاتٍ مصقولة</summary>
        private Brush Ramp(double t)
        {
            Color Of(string key) => (Brush(key) as SolidColorBrush)?.Color ?? Colors.Gray;
            var (from, to, local) = t <= 0.5 ? (Of("ChartGood"), Of("ChartWarn"), t * 2) : (Of("ChartWarn"), Of("ChartBad"), (t - 0.5) * 2);
            byte Mix(byte a, byte b) => (byte)Math.Round(a + (b - a) * local);
            return new SolidColorBrush(Color.FromRgb(Mix(from.R, to.R), Mix(from.G, to.G), Mix(from.B, to.B)));
        }

        private static Brush AreaFill(Brush brush)
        {
            if (brush is not SolidColorBrush solid) return brush;
            return new LinearGradientBrush(Color.FromArgb(80, solid.Color.R, solid.Color.G, solid.Color.B), Color.FromArgb(6, solid.Color.R, solid.Color.G, solid.Color.B), 90);
        }

        private Brush SeriesBrush(int index, ChartSeries series) =>
            series.Comparison || index >= Palette.Length ? Brush("TextMuted") : Brush(Palette[index]);

        private Brush Brush(string key) => TryFindResource(key) as Brush ?? Brushes.Gray;

        private TextBlock Label(string text, string brush, double size, bool bold = false, bool trim = false) => new()
        {
            Text = text, FontSize = size, Foreground = Brush(brush), FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            FontFamily = TryFindResource("P.Font.Family.Primary") as FontFamily ?? new FontFamily("Segoe UI"),
            TextTrimming = trim ? TextTrimming.CharacterEllipsis : TextTrimming.None
        };

        private void Place(FrameworkElement element, double x, double y, double width = double.NaN, TextAlignment alignment = TextAlignment.Left)
        {
            if (!double.IsNaN(width) && element is TextBlock text) { text.Width = Math.Max(width, 0); text.TextAlignment = alignment; }
            _canvas.Children.Add(Positioned(element, x, y));
        }

        private static UIElement Positioned(UIElement element, double x, double y)
        {
            Canvas.SetLeft(element, x);
            Canvas.SetTop(element, y);
            return element;
        }

        private static void Hover(UIElement shape)
        {
            var normal = shape.Opacity;
            shape.MouseEnter += (_, __) => shape.Opacity = Math.Min(normal + 0.25, 1);
            shape.MouseLeave += (_, __) => shape.Opacity = normal;
        }

        private string Tip(string label, ChartSeries series, decimal value) => $"{label} — {series.Name}: {Value(value)}";

        private string Value(decimal value) => $"{value.ToString(Format)}{Suffix}";

        private string Short(double value)
        {
            var abs = Math.Abs(value);
            var text = abs >= 1_000_000 ? $"{value / 1_000_000:0.#}M" : abs >= 10_000 ? $"{value / 1_000:0.#}K" : value.ToString(Format);
            return $"{text}{Suffix}";
        }

        private static double Nice(double value)
        {
            if (value <= 0) return 0;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
            var normalized = value / magnitude;
            var step = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 2.5 ? 2.5 : normalized <= 5 ? 5 : 10;
            return step * magnitude;
        }
    }
}
