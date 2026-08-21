using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Display
{
    public partial class AppBadge : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(AppBadge),
                new PropertyMetadata("", OnTextChanged));

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(nameof(Variant), typeof(string), typeof(AppBadge),
                new PropertyMetadata("neutral", OnVariantChanged));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(AppBadge),
                new PropertyMetadata(null, OnIconChanged));

        public string   Text    { get => (string)GetValue(TextProperty);    set => SetValue(TextProperty, value); }
        public string   Variant { get => (string)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
        public Geometry Icon    { get => (Geometry)GetValue(IconProperty);  set => SetValue(IconProperty, value); }

        public AppBadge()
        {
            InitializeComponent();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppBadge)d).txt.Text = (string)e.NewValue;

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppBadge)d;
            c.icon.Data = (Geometry)e.NewValue;
            c.icon.Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppBadge)d;
            var (bg, fg) = ((string)e.NewValue) switch
            {
                "success" => ((Brush)c.FindResource("SuccessSoft"),       (Brush)c.FindResource("Success")),
                "danger"  => ((Brush)c.FindResource("DangerSoft"), (Brush)c.FindResource("Danger")),
                "warning" => ((Brush)c.FindResource("WarningSoft"),  (Brush)c.FindResource("Warning")),
                "info"    => ((Brush)c.FindResource("InfoSoft"),     (Brush)c.FindResource("Info")),
                "brand"   => ((Brush)c.FindResource("BrandSoft"),    (Brush)c.FindResource("BrandDefault")),
                _         => ((Brush)c.FindResource("SurfaceSunken"),      (Brush)c.FindResource("TextSecondary"))
            };

            c.border.Background  = bg;
            c.border.BorderBrush = fg;
            c.txt.Foreground     = fg;
            c.icon.Stroke        = fg;
            c.icon.StrokeThickness = 2;
        }
    }
}
