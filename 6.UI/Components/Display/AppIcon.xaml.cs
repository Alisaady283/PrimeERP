using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Display
{
    /// <summary>أيقونة بمواصفة واحدة — تقرأ الحجم والسماكة من C.Icon.* فلا موضع استدعاء يحدّد رقماً.
    /// Key هو مفتاح Geometry في 5.Design/Icons/Icons.xaml.</summary>
    public partial class AppIcon : UserControl
    {
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.Register(nameof(Key), typeof(string), typeof(AppIcon),
                new PropertyMetadata(null, (d, e) => ((AppIcon)d).Refresh()));

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(string), typeof(AppIcon),
                new PropertyMetadata("Md", (d, e) => ((AppIcon)d).Refresh()));

        public static readonly DependencyProperty BrushProperty =
            DependencyProperty.Register(nameof(Brush), typeof(Brush), typeof(AppIcon),
                new PropertyMetadata(null, (d, e) => ((AppIcon)d).Refresh()));

        public string Key   { get => (string)GetValue(KeyProperty);   set => SetValue(KeyProperty, value); }
        public string Size  { get => (string)GetValue(SizeProperty);  set => SetValue(SizeProperty, value); }
        public Brush  Brush { get => (Brush)GetValue(BrushProperty);  set => SetValue(BrushProperty, value); }

        public AppIcon()
        {
            InitializeComponent();
            Loaded += (_, __) => Refresh();
        }

        private void Refresh()
        {
            if (path == null) return;

            path.Data = string.IsNullOrEmpty(Key) ? null : TryFindResource(Key) as Geometry;
            path.StrokeThickness = Resource("C.Icon.Stroke", 1.75);
            if (Brush != null) path.Stroke = Brush;
            else path.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, "C.Icon.Fg");

            var size = Resource($"C.Icon.Size.{Size}", 18);
            path.Width = size;
            path.Height = size;
            Width = size;
            Height = size;
        }

        private double Resource(string key, double fallback) =>
            TryFindResource(key) is double value ? value : fallback;
    }
}
