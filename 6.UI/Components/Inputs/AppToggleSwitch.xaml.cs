using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PrimeERP.UI.Components.Inputs
{
    /// <summary>حقل إدخال AppToggleSwitch</summary>
    public partial class AppToggleSwitch : UserControl
    {
        private const double ThumbTravel = 18;

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(AppToggleSwitch),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsCheckedChanged));

        public static readonly DependencyProperty OnLabelProperty =
            DependencyProperty.Register(nameof(OnLabel), typeof(string), typeof(AppToggleSwitch),
                new PropertyMetadata("مفعّل", OnStatusTextChanged));

        public static readonly DependencyProperty OffLabelProperty =
            DependencyProperty.Register(nameof(OffLabel), typeof(string), typeof(AppToggleSwitch),
                new PropertyMetadata("معطّل", OnStatusTextChanged));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(AppToggleSwitch),
                new PropertyMetadata("", OnLabelChanged));

        public bool   IsChecked { get => (bool)GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
        public string OnLabel   { get => (string)GetValue(OnLabelProperty); set => SetValue(OnLabelProperty, value); }
        public string OffLabel  { get => (string)GetValue(OffLabelProperty);set => SetValue(OffLabelProperty, value); }
        public string Label     { get => (string)GetValue(LabelProperty);   set => SetValue(LabelProperty, value); }

        public event EventHandler CheckedChanged;

        public AppToggleSwitch()
        {
            InitializeComponent();
            IsEnabledChanged += (s, e) => ApplyEnabledVisual();
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppToggleSwitch)d;
            var isChecked = (bool)e.NewValue;

            var anim = new DoubleAnimation
            {
                To = isChecked ? ThumbTravel : 0,
                Duration = TimeSpan.FromMilliseconds(120)
            };
            c.thumbTransform.BeginAnimation(TranslateTransform.XProperty, anim);

            c.track.Background = isChecked ? (Brush)c.FindResource("BrandDefault") : (Brush)c.FindResource("OutlineStrong");
            c.UpdateStatusText();
        }

        private static void OnStatusTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AppToggleSwitch)d).UpdateStatusText();
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppToggleSwitch)d;
            c.txtLabel.Text = c.Label;
            c.txtLabel.Visibility = string.IsNullOrEmpty(c.Label) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateStatusText()
        {
            txtStatus.Text = IsChecked ? OnLabel : OffLabel;
        }

        private void ApplyEnabledVisual()
        {
            root.Opacity = IsEnabled ? 1.0 : 0.5;
            root.Cursor = IsEnabled ? Cursors.Hand : Cursors.Arrow;
        }

        private void Root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsEnabled) return;
            IsChecked = !IsChecked;
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
