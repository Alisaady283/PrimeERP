using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using PrimeERP.Application.Analytics;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.Components.Charts
{
    public enum ChartKind { Column, Bar, Line, Area, Donut, Gauge }

    /// <summary>Auto: سلسلةٌ واحدة بلون الهوية وأكثر بالترتيب الثابت؛ Severity/Improving: فئاتٌ مرتّبة على تدرّج الحالة</summary>
    public enum ChartTone { Auto, Severity, Improving }

    /// <summary>
    /// رسمٌ بياني متحرّك من ChartData بألوان الهوية — نظير Chart في الويب (ECharts):
    /// علاماتٌ بتدرّج، والأعمدة تنمو بالتتابع، والمرور يُبرز العلامة ويُخفت غيرها بتلميحٍ غنيّ (ChartInsights)،
    /// والدائري من الأكبر وتبتعد شريحته بالنقر لحظة، والمقياس قوسٌ 240° بلون الشدّة. الحركة من رموز Motion.
    /// </summary>
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

        /// <summary>تأخير الدخول: اللوحة تبدأ بعد آخر بطاقة (chartsStart في الويب)</summary>
        public static readonly DependencyProperty DelayProperty =
            DependencyProperty.Register(nameof(Delay), typeof(TimeSpan), typeof(AppChart), new PropertyMetadata(TimeSpan.Zero));

        /// <summary>تقدّم الدخول خطّياً من 0 إلى 1؛ والمنحنى يُطبَّق لكل علامة (Local) ليتتابع</summary>
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
        public TimeSpan Delay { get => (TimeSpan)GetValue(DelayProperty); set => SetValue(DelayProperty, value); }

        /// <summary>نُقر عنصرٌ بفهرسه في Data.Labels — اللوحة تصفّي به (BI)؛ يُنتظر مهلة المزدوج فإن جاء كان فتحاً</summary>
        public event EventHandler<int> Pick;
        /// <summary>نُقر عنصرٌ نقراً مزدوجاً — اللوحة تفتح صفحته مصفّاةً به</summary>
        public event EventHandler<int> Open;

        /// <summary>مهلة النقر المزدوج — نظير DOUBLE_CLICK في الويب</summary>
        private const int DoubleClickMs = 250;
        private System.Windows.Threading.DispatcherTimer _single;
        private int _clicked;
        private double Progress => (double)GetValue(ProgressProperty);

        private static readonly string[] Palette = { "ChartSeries1", "ChartSeries2", "ChartSeries3", "ChartSeries4", "ChartSeries5" };
        private const double AxisWidth = 64, AxisHeight = 26, Gap = 8, MaxColumnWidth = 56, SlotShare = 0.55, SeriesGap = 4, PullOffset = 12, Dimmed = 0.35;

        private readonly Canvas _canvas = new() { ClipToBounds = true };
        private readonly WrapPanel _legend = new() { Margin = new Thickness(0, 0, 0, 6) };

        /// <summary>علامات الرسم الحالي — المرور يُخفت غير المختارة منها</summary>
        private readonly Dictionary<UIElement, double> _marks = new();

        /// <summary>امتداد الدخول بالملّي ثانية: مدّة العلامة وتتابع الأعمدة</summary>
        private double _span = Motion.Long2.TotalMilliseconds;

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

        /// <summary>مقاييس التلميح تُحسب مرّةً لكل بيانات، وتُعرض عند المرور فقط</summary>
        private readonly Dictionary<string, Insight> _insights = new();

        private bool Staggered => Kind is ChartKind.Column or ChartKind.Bar;

        private void Animate()
        {
            if (!IsLoaded) return;
            _insights.Clear();
            var count = Data?.Labels.Count ?? 0;
            _span = Motion.Long2.TotalMilliseconds + (Staggered ? Motion.Stagger(Math.Max(count - 1, 0), count).TotalMilliseconds : 0);
            // صفرٌ طوال التأخير ثم خطّيٌّ إلى 1 — فلا يظهر الرسم كاملاً قبل بدئه
            var entrance = new DoubleAnimationUsingKeyFrames();
            entrance.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            entrance.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(Delay)));
            entrance.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(Delay + TimeSpan.FromMilliseconds(_span))));
            BeginAnimation(ProgressProperty, entrance);
        }

        /// <summary>تقدّم العلامة i بمنحنى الدخول وتأخير تتابعها — كـanimationDelay في ECharts</summary>
        private double Local(int index, int count)
        {
            var delay = Staggered ? Motion.Stagger(index, count).TotalMilliseconds : 0;
            var t = Math.Clamp((Progress * _span - delay) / Motion.Long2.TotalMilliseconds, 0, 1);
            return Motion.Decelerate.GetSplineProgress(t);
        }

        private void Draw()
        {
            _canvas.Children.Clear();
            _legend.Children.Clear();
            _marks.Clear();
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
                    // التلميح يُبنى عند أول مرور بعد انتهاء الحلقة: نسختا الفهرسين لا متغيّرا for المشتركان
                    int point = i, at = s;
                    var value = (double)series.Values.ElementAtOrDefault(i);
                    var grown = Local(i, data.Labels.Count);
                    var length = Math.Abs(value) / (max - min) * height * grown;
                    var x = left + slot * i + (slot - group) / 2 + (barWidth + SeriesGap) * s;
                    var bar = new Border
                    {
                        Width = barWidth, Height = length, Background = PointBrush(data, s, i), Opacity = series.Comparison ? 0.5 : 1,
                        CornerRadius = value >= 0 ? new CornerRadius(5, 5, 0, 0) : new CornerRadius(0, 0, 5, 5),
                        ToolTip = Lazy($"c{at}:{point}", () => ChartInsights.Column(data, at, point, Value), k => PointBrush(data, k, point))
                    };
                    Mark(bar, i);
                    Place(bar, x, value >= 0 ? zero - length : zero);
                    if (grown >= 1 && data.Series.Count == 1 && data.Labels.Count <= 8 && value != 0)
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
            var shown = Local(0, 1);
            var reach = shown * (count - 1);

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
                    StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
                });

                if (shown < 1) continue;
                for (var i = 0; i < count; i++)
                {
                    var point = i;
                    var dot = new Ellipse { Width = 10, Height = 10, Fill = brush, Stroke = Brush("SurfaceDefault"), StrokeThickness = 2, ToolTip = Lazy($"p{point}", () => ChartInsights.Point(data, point, Value), k => PointBrush(data, k, -1)) };
                    Mark(dot, i);
                    Place(dot, X(i) - 5, Y(series.Values.ElementAtOrDefault(i)) - 5);
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
                var point = i;
                var value = series.Values.ElementAtOrDefault(i);
                var y = i * row;
                Place(Label(data.Labels[i], "TextPrimary", text, trim: true), 0, y + row / 2 - text * 0.75, labelWidth - Gap, TextAlignment.Left);
                var barTop = y + (row - thickness) / 2;
                _canvas.Children.Add(Positioned(new Border { Width = Math.Max(track, 1), Height = thickness, CornerRadius = new CornerRadius(thickness / 2), Background = Brush("SurfaceSunken") }, labelWidth, barTop));
                var bar = new Border
                {
                    Width = Math.Max(track * Math.Max((double)value, 0) / max * Local(i, count), 0), Height = thickness, CornerRadius = new CornerRadius(thickness / 2),
                    Background = PointBrush(data, 0, point), ToolTip = Lazy($"b{point}", () => ChartInsights.Slice(data.Labels[point], value, series.Values, Value), _ => PointBrush(data, 0, point))
                };
                Mark(bar, i);
                Place(bar, labelWidth, barTop);
                Place(Label(Value(value), "TextSecondary", text - 1), labelWidth + track + Gap, y + row / 2 - text * 0.7, valueWidth, TextAlignment.Left);
            }
        }

        /// <summary>الدائري: من الأكبر (إلا ترتيب الشدّة)، والمرور يكتب حصّة الشريحة في الوسط، والنقر يبعدها لحظةً ثم تعود</summary>
        private void DrawDonut(ChartData data)
        {
            var series = data.Series[0];
            var order = ChartInsights.DonutOrder(data, Tone == ChartTone.Auto);
            var values = order.Select(i => Math.Max(series.Values.ElementAtOrDefault(i), 0)).ToList();
            var total = values.Sum();
            var size = Math.Min(_canvas.ActualHeight, _canvas.ActualWidth * 0.5) - Gap * 2 - PullOffset * 2;
            var radius = size / 2;
            var thickness = radius * 0.36;
            var center = new Point(Gap + PullOffset + radius, _canvas.ActualHeight / 2);
            var start = -90.0;
            var sweepAll = Local(0, 1);

            var centerValue = Label(Short((double)total), "TextPrimary", 16, bold: true);
            var centerName = Label("", "TextMuted", 11, trim: true);

            for (var at = 0; at < order.Count; at++)
            {
                var i = order[at];
                var value = values[at];
                var color = PointBrush(data, 0, Tone == ChartTone.Auto ? at : i);
                var sweep = total == 0 ? 0 : (double)(value / total) * 360 * sweepAll;
                if (sweep > 0.2)
                {
                    var mid = (start + sweep / 2) * Math.PI / 180;
                    var pull = new TranslateTransform();
                    var slice = new Path
                    {
                        Data = Ring(center, radius, thickness, start, sweep), Fill = color, Stroke = Brush("SurfaceDefault"), StrokeThickness = 2,
                        RenderTransform = pull, Cursor = System.Windows.Input.Cursors.Hand,
                        ToolTip = Lazy($"d{i}", () => ChartInsights.Slice(data.Labels[i], value, values, Value), _ => color)
                    };
                    var share = total == 0 ? 0 : (double)(value / total * 100);
                    var name = data.Labels[i];
                    Mark(slice, i, () => { centerValue.Text = ChartInsights.Percent(share); centerName.Text = name; },
                                () => { centerValue.Text = Short((double)total); centerName.Text = ""; });
                    slice.MouseLeftButtonUp += (_, __) => Pull(pull, mid);
                    _canvas.Children.Add(slice);
                }
                start += sweep;

                var legendY = _canvas.ActualHeight / 2 - order.Count * 11 + at * 22;
                var legendX = center.X + radius + PullOffset + Gap * 2;
                Place(new Rectangle { Width = 10, Height = 10, RadiusX = 5, RadiusY = 5, Fill = color }, legendX, legendY + 4);
                Place(Label($"{data.Labels[i]}  {(total == 0 ? 0 : Math.Round(value / total * 100, 1)):N1}%", "TextPrimary", 12, trim: true),
                    legendX + 16, legendY, _canvas.ActualWidth - legendX - 16, TextAlignment.Left);
            }

            Place(centerValue, center.X - radius, center.Y - 14, radius * 2, TextAlignment.Center);
            Place(centerName, center.X - radius + thickness, center.Y + 8, radius * 2 - thickness * 2, TextAlignment.Center);
        }

        /// <summary>ابتعاد الشريحة على منتصف زاويتها، ثم عودتها بعد ChartInsights.PullHold</summary>
        private static void Pull(TranslateTransform pull, double mid)
        {
            void Move(double distance, TimeSpan? delay)
            {
                pull.BeginAnimation(TranslateTransform.XProperty, Motion.To(Math.Cos(mid) * distance, Motion.Medium2, Motion.Emphasized, delay));
                pull.BeginAnimation(TranslateTransform.YProperty, Motion.To(Math.Sin(mid) * distance, Motion.Medium2, Motion.Emphasized, delay));
            }
            Move(PullOffset, null);
            var back = new System.Windows.Threading.DispatcherTimer { Interval = Motion.Medium2 + ChartInsights.PullHold };
            back.Tick += (_, __) => { back.Stop(); Move(0, null); };
            back.Start();
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

        /// <summary>نسبةٌ واحدة على قوسٍ 240° — كمقياس الويب: مسارٌ غائر، والمُنجَز بتدرّج لون الشدّة وتوهّجه، والرقم يعدّ معه</summary>
        private void DrawGauge(ChartData data)
        {
            var value = (double)Math.Clamp(data.Series[0].Values.FirstOrDefault(), 0, 100);
            var size = Math.Min(_canvas.ActualHeight * 1.15, _canvas.ActualWidth) * 0.8;
            var thickness = Math.Max(size * 0.07, 8);
            var r = (size - thickness) / 2;
            var center = new Point(_canvas.ActualWidth / 2, _canvas.ActualHeight * 0.55);
            Point At(double deg) => new(center.X + r * Math.Cos(deg * Math.PI / 180), center.Y + r * Math.Sin(deg * Math.PI / 180));

            Path Arc(double sweep, Brush stroke)
            {
                var figure = new PathFigure { StartPoint = At(150), IsClosed = false };
                figure.Segments.Add(new ArcSegment(At(150 + sweep), new Size(r, r), 0, sweep > 180, SweepDirection.Clockwise, true));
                return new Path { Data = new PathGeometry { Figures = { figure } }, Stroke = stroke, StrokeThickness = thickness, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
            }

            _canvas.Children.Add(Arc(240, Brush("SurfaceSunken")));
            var shown = Local(0, 1);
            var color = PointBrush(data, 0, -1);
            if (value * shown > 0.3)
            {
                var arc = Arc(240 * value / 100 * shown, color);
                arc.ToolTip = Lazy("g", () => new Insight(data.Labels.ElementAtOrDefault(0) ?? data.Series[0].Name, new[] { new InsightRow(data.Series[0].Name, $"{value:N1}%", Strong: true) }), _ => color);
                Instant(arc);
                _canvas.Children.Add(arc);
            }

            Place(Label($"{value * shown:N0}%", "TextPrimary", Math.Max(r * 0.34, 16), bold: true), center.X - r, center.Y - r * 0.28, r * 2, TextAlignment.Center);
            Place(Label(data.Series[0].Name, "TextMuted", 12), center.X - r, center.Y + r * 0.16, r * 2, TextAlignment.Center);
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
        private SolidColorBrush Ramp(double t)
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

        /// <summary>النقرة الأولى تنتظر المهلة ثم تصفّي، والثانية قبلها تلغيها وتفتح</summary>
        private void Clicked(int index, int count)
        {
            if (_single == null)
            {
                _single = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DoubleClickMs) };
                _single.Tick += (_, __) => { _single.Stop(); Pick?.Invoke(this, _clicked); };
            }
            _single.Stop();
            if (count >= 2) { Open?.Invoke(this, index); return; }
            _clicked = index;
            _single.Start();
        }

        /// <summary>علامةٌ تفاعلية: المرور يُبقيها ويُخفت غيرها (focus: self في الويب)، ومعه ما تُظهره في الوسط إن وُجد</summary>
        private void Mark(UIElement mark, int index, Action enter = null, Action leave = null)
        {
            mark.MouseLeftButtonDown += (_, e) => Clicked(index, e.ClickCount);
            if (mark is FrameworkElement element) element.Cursor = System.Windows.Input.Cursors.Hand;
            _marks[mark] = mark.Opacity;
            Instant(mark);
            mark.MouseEnter += (_, __) =>
            {
                foreach (var (other, normal) in _marks)
                    other.BeginAnimation(OpacityProperty, Motion.To(other == mark ? normal : normal * Dimmed, Motion.Short3, Motion.Emphasized));
                enter?.Invoke();
            };
            mark.MouseLeave += (_, __) =>
            {
                foreach (var (other, normal) in _marks) other.BeginAnimation(OpacityProperty, Motion.To(normal, Motion.Short3, Motion.Emphasized));
                leave?.Invoke();
            };
        }

        /// <summary>
        /// بطاقة التلميح فوق العلامة عند المؤشّر — كتلميح الويب: تظهر فوراً بالمرور وتختفي بالابتعاد،
        /// ومحتواها يُبنى عند أول فتحٍ من مقاييس محسوبةٍ مرّةً للبيانات (_insights)، لا في كل إطار حركة.
        /// </summary>
        private ToolTip Lazy(string key, Func<Insight> make, Func<int, Brush> colorFor)
        {
            var tip = new ToolTip { Placement = PlacementMode.Mouse, HorizontalOffset = 14, VerticalOffset = 14 };
            tip.Opened += (_, __) =>
            {
                if (!_insights.TryGetValue(key, out var insight)) _insights[key] = insight = make();
                tip.Content ??= Tip(insight, colorFor);
            };
            return tip;
        }

        private static void Instant(UIElement mark)
        {
            ToolTipService.SetInitialShowDelay(mark, 0);
            ToolTipService.SetBetweenShowDelay(mark, 0);
            ToolTipService.SetShowDuration(mark, int.MaxValue);
        }

        /// <summary>التلميح: العنوان، ثم سطورٌ بنقطة لون السلسلة، والفرق أخضر صاعداً وأحمر هابطاً</summary>
        private FrameworkElement Tip(Insight insight, Func<int, Brush> colorFor)
        {
            var panel = new StackPanel { MinWidth = 180 };
            var title = Label(insight.Title, "TextPrimary", 12.5, bold: true);
            title.Margin = new Thickness(0, 0, 0, 4);
            panel.Children.Add(title);
            foreach (var row in insight.Rows)
            {
                var line = new DockPanel { Margin = new Thickness(0, 2, 0, 0), LastChildFill = true };
                var value = Label(row.Value, row.Trend > 0 ? "Success" : row.Trend < 0 ? "Danger" : "TextPrimary", 12, bold: row.Strong || row.Trend != 0);
                value.Margin = new Thickness(16, 0, 0, 0);
                DockPanel.SetDock(value, Dock.Right);
                line.Children.Add(value);
                var name = new StackPanel { Orientation = Orientation.Horizontal };
                if (row.Series is int k) name.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = colorFor(k), Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
                var label = Label(row.Label, "TextSecondary", 12);
                name.Children.Add(label);
                line.Children.Add(name);
                panel.Children.Add(line);
            }
            return panel;
        }

        private string Value(decimal value) => $"{value.ToString(Format)}{Suffix}";

        private string Short(double value) => $"{NumberText.Compact(value, Format)}{Suffix}";

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
