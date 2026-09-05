using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Actions
{
    public partial class AppIconButton : UserControl
    {
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(AppIconButton),
                new PropertyMetadata(null, (d, e) => ((AppIconButton)d).icon.Data = (Geometry)e.NewValue));

        public static readonly DependencyProperty TooltipTextProperty =
            DependencyProperty.Register(nameof(TooltipText), typeof(string), typeof(AppIconButton),
                new PropertyMetadata("", (d, e) => ((AppIconButton)d).btn.ToolTip = e.NewValue));

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(nameof(Variant), typeof(string), typeof(AppIconButton),
                new PropertyMetadata("ghost", OnVariantChanged));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(AppIconButton),
                new PropertyMetadata(null, (d, e) => ((AppIconButton)d).btn.Command = (ICommand)e.NewValue));

        public Geometry Icon        { get => (Geometry)GetValue(IconProperty);        set => SetValue(IconProperty, value); }
        public string   TooltipText { get => (string)GetValue(TooltipTextProperty);   set => SetValue(TooltipTextProperty, value); }
        public string   Variant     { get => (string)GetValue(VariantProperty);       set => SetValue(VariantProperty, value); }
        public ICommand Command     { get => (ICommand)GetValue(CommandProperty);     set => SetValue(CommandProperty, value); }

        public event RoutedEventHandler Click;

        public AppIconButton()
        {
            InitializeComponent();
            Loaded += (s, e) => ApplyVariant();
        }

        private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppIconButton)d).ApplyVariant();

        private void ApplyVariant()
        {
            if (!IsLoaded) return;

            var stroke = Variant switch
            {
                "danger" => "Danger",
                "brand"  => "BrandDefault",
                _        => "TextSecondary"
            };

            btn.Background = Brushes.Transparent;
            icon.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, stroke);
        }

        private void btn_Click(object sender, RoutedEventArgs e) => Click?.Invoke(this, e);
    }
}
