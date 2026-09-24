using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Display
{
    /// <summary>عرض AppBadge</summary>
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
                "success" => ("SuccessSoft", "Success"),
                "danger"  => ("DangerSoft",  "Danger"),
                "warning" => ("WarningSoft", "Warning"),
                "info"    => ("InfoSoft",    "Info"),
                "brand"   => ("BrandSoft",   "BrandDefault"),
                _         => ("SurfaceSunken", "TextSecondary")
            };

            c.border.SetResourceReference(Border.BackgroundProperty, bg);
            c.border.SetResourceReference(Border.BorderBrushProperty, fg);
            c.txt.SetResourceReference(TextBlock.ForegroundProperty, fg);
            c.icon.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, fg);
            c.icon.StrokeThickness = 2;
        }
    }
}
