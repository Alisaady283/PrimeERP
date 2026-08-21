using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Display
{
    public partial class AppStatCard : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(AppStatCard),
                new PropertyMetadata("", OnTitleChanged));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(string), typeof(AppStatCard),
                new PropertyMetadata("", OnValueChanged));

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(AppStatCard),
                new PropertyMetadata("", OnSubtitleChanged));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(AppStatCard),
                new PropertyMetadata(null, OnIconChanged));

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(string), typeof(AppStatCard),
                new PropertyMetadata("brand", OnColorChanged));

        public static readonly DependencyProperty TrendProperty =
            DependencyProperty.Register(nameof(Trend), typeof(double?), typeof(AppStatCard),
                new PropertyMetadata(null, OnTrendChanged));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(AppStatCard),
                new PropertyMetadata(null));

        public string   Title    { get => (string)GetValue(TitleProperty);    set => SetValue(TitleProperty, value); }
        public string   Value    { get => (string)GetValue(ValueProperty);    set => SetValue(ValueProperty, value); }
        public string   Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
        public Geometry Icon     { get => (Geometry)GetValue(IconProperty);   set => SetValue(IconProperty, value); }
        public string   Color    { get => (string)GetValue(ColorProperty);    set => SetValue(ColorProperty, value); }
        public double?  Trend    { get => (double?)GetValue(TrendProperty);   set => SetValue(TrendProperty, value); }
        public ICommand Command  { get => (ICommand)GetValue(CommandProperty);set => SetValue(CommandProperty, value); }

        /// <summary>يُطلق عند النقر — استخدمه أو Command حسب ما يناسب الاستدعاء.</summary>
        public event EventHandler Click;

        public AppStatCard()
        {
            InitializeComponent();
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppStatCard)d).txtTitle.Text = (string)e.NewValue;

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppStatCard)d).txtValue.Text = (string)e.NewValue;

        private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppStatCard)d;
            c.txtSubtitle.Text = (string)e.NewValue;
            c.txtSubtitle.Visibility = string.IsNullOrEmpty((string)e.NewValue) ? Visibility.Collapsed : Visibility.Visible;
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppStatCard)d;
            c.icon.Data = (Geometry)e.NewValue;
            c.iconWrap.Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
            c.ApplyColor();
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppStatCard)d).ApplyColor();

        private static void OnTrendChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppStatCard)d;
            var trend = (double?)e.NewValue;

            if (trend == null)
            {
                c.trendBadge.Visibility = Visibility.Collapsed;
                return;
            }

            c.trendBadge.Visibility = Visibility.Visible;
            var isUp = trend.Value >= 0;
            c.txtTrend.Text = $"{(isUp ? "▲" : "▼")} {Math.Abs(trend.Value):N1}%";

            var bg = isUp ? (Brush)c.FindResource("OkTintBrush") : (Brush)c.FindResource("CriticalTintBrush");
            var fg = isUp ? (Brush)c.FindResource("OkBrush")     : (Brush)c.FindResource("CriticalBrush");
            c.trendBadge.Background = bg;
            c.txtTrend.Foreground = fg;
        }

        private void ApplyColor()
        {
            var (bg, fg) = Color switch
            {
                "success" => ((Brush)FindResource("OkTintBrush"),       (Brush)FindResource("OkBrush")),
                "danger"  => ((Brush)FindResource("CriticalTintBrush"), (Brush)FindResource("CriticalBrush")),
                "warning" => ((Brush)FindResource("CautionTintBrush"),  (Brush)FindResource("CautionBrush")),
                "info"    => ((Brush)FindResource("InfoTintBrush"),     (Brush)FindResource("InfoBrush")),
                _         => ((Brush)FindResource("BrandTintBrush"),    (Brush)FindResource("BrandBrush"))
            };

            iconWrap.Background = bg;
            icon.Stroke = fg;
        }

        private void Root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Command?.CanExecute(null) == true)
                Command.Execute(null);
            Click?.Invoke(this, EventArgs.Empty);
        }
    }
}
