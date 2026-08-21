using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace PrimeERP.Views.Controls.Display
{
    public partial class AppLoadingOverlay : UserControl
    {
        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(AppLoadingOverlay),
                new PropertyMetadata(false, OnIsBusyChanged));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(AppLoadingOverlay),
                new PropertyMetadata("", OnMessageChanged));

        public bool   IsBusy  { get => (bool)GetValue(IsBusyProperty);  set => SetValue(IsBusyProperty, value); }
        public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }

        private readonly DoubleAnimation _spin;

        public AppLoadingOverlay()
        {
            InitializeComponent();
            _spin = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(0.9),
                RepeatBehavior = RepeatBehavior.Forever
            };
        }

        private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppLoadingOverlay)d;
            var visible = (bool)e.NewValue;
            c.root.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            if (visible)
                c.spinnerTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, c._spin);
            else
                c.spinnerTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null);
        }

        private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppLoadingOverlay)d;
            c.txtMessage.Text = (string)e.NewValue;
            c.txtMessage.Visibility = string.IsNullOrEmpty((string)e.NewValue) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
