using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PrimeERP.UI.Components.Feedback
{
    /// <summary>حوارات وتنبيهات AppToast</summary>
    public partial class AppToast : UserControl
    {
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(AppToast),
                new PropertyMetadata("", (d, e) => ((AppToast)d).txtMessage.Text = (string)e.NewValue));

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(nameof(Variant), typeof(string), typeof(AppToast),
                new PropertyMetadata("info", OnVariantChanged));

        public static readonly DependencyProperty ShowIconProperty =
            DependencyProperty.Register(nameof(ShowIcon), typeof(bool), typeof(AppToast),
                new PropertyMetadata(true, (d, e) => ((AppToast)d).iconWrap.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed));

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(nameof(Duration), typeof(int), typeof(AppToast),
                new PropertyMetadata(3000));

        public string Message  { get => (string)GetValue(MessageProperty);  set => SetValue(MessageProperty, value); }
        public string Variant  { get => (string)GetValue(VariantProperty);  set => SetValue(VariantProperty, value); }
        public bool   ShowIcon { get => (bool)GetValue(ShowIconProperty);   set => SetValue(ShowIconProperty, value); }
        public int    Duration { get => (int)GetValue(DurationProperty);    set => SetValue(DurationProperty, value); }

        public event EventHandler Dismissed;

        private DispatcherTimer _timer;

        public AppToast()
        {
            InitializeComponent();
            Loaded += (s, e) => StartTimerIfNeeded();
        }

        private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppToast)d;
            var (accentBrush, iconBg, iconFg, iconKey) = ((string)e.NewValue) switch
            {
                "success" => ((Brush)c.FindResource("Success"),       (Brush)c.FindResource("SuccessSoft"),       (Brush)c.FindResource("Success"),       "IconCheck"),
                "error"   => ((Brush)c.FindResource("Danger"), (Brush)c.FindResource("DangerSoft"), (Brush)c.FindResource("Danger"), "IconX"),
                "warning" => ((Brush)c.FindResource("Warning"),  (Brush)c.FindResource("WarningSoft"),  (Brush)c.FindResource("Warning"),  "IconWarning"),
                _         => ((Brush)c.FindResource("Info"),     (Brush)c.FindResource("InfoSoft"),     (Brush)c.FindResource("Info"),     "IconInfo")
            };

            c.accent.Fill = accentBrush;
            c.iconWrap.Background = iconBg;
            c.icon.Stroke = iconFg;
            c.icon.Data = c.FindResource(iconKey) as Geometry;
        }

        private void StartTimerIfNeeded()
        {
            if (Duration <= 0) return;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Duration) };
            _timer.Tick += (s, e) => { _timer.Stop(); Dismissed?.Invoke(this, EventArgs.Empty); };
            _timer.Start();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            Dismissed?.Invoke(this, EventArgs.Empty);
        }
    }
}
