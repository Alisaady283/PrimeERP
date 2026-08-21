using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Display
{
    public partial class AppTabControl : UserControl
    {
        public static readonly DependencyProperty TabsProperty =
            DependencyProperty.Register(nameof(Tabs), typeof(IEnumerable<AppTabItem>), typeof(AppTabControl),
                new PropertyMetadata(null, OnTabsChanged));

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(AppTabControl),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedIndexChanged));

        public IEnumerable<AppTabItem> Tabs          { get => (IEnumerable<AppTabItem>)GetValue(TabsProperty); set => SetValue(TabsProperty, value); }
        public int                     SelectedIndex { get => (int)GetValue(SelectedIndexProperty);            set => SetValue(SelectedIndexProperty, value); }

        private List<AppTabItem> _tabs = new();
        private readonly List<Button> _headerButtons = new();

        public AppTabControl()
        {
            InitializeComponent();
        }

        private static void OnTabsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppTabControl)d).Rebuild();

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppTabControl)d).ApplySelection();

        private void Rebuild()
        {
            _tabs = Tabs?.ToList() ?? new List<AppTabItem>();
            pnlTabs.Children.Clear();
            _headerButtons.Clear();

            for (int i = 0; i < _tabs.Count; i++)
            {
                int index = i;
                var btn = new Button
                {
                    Content = _tabs[i].Header,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0, 0, 0, 2),
                    BorderBrush = Brushes.Transparent,
                    Padding = new Thickness(4, 10, 4, 10),
                    Margin = new Thickness(0, 0, 24, 0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontSize = (double)FindResource("P.Font.Size.300"),
                    FontFamily = (FontFamily)FindResource("P.Font.Family.Primary"),
                    Foreground = (Brush)FindResource("TextMuted")
                };

                btn.Template = BuildFlatTemplate();
                btn.Click += (s, e) => SelectedIndex = index;

                _headerButtons.Add(btn);
                pnlTabs.Children.Add(btn);
            }

            ApplySelection();
        }

        private ControlTemplate BuildFlatTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);

            template.VisualTree = borderFactory;
            return template;
        }

        private void ApplySelection()
        {
            if (_tabs.Count == 0) return;

            var index = SelectedIndex >= 0 && SelectedIndex < _tabs.Count ? SelectedIndex : 0;

            for (int i = 0; i < _headerButtons.Count; i++)
            {
                var active = i == index;
                _headerButtons[i].BorderBrush = active ? (Brush)FindResource("BrandDefault") : Brushes.Transparent;
                _headerButtons[i].Foreground = active ? (Brush)FindResource("BrandDefault") : (Brush)FindResource("TextMuted");
                _headerButtons[i].FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
            }

            contentHost.Content = _tabs[index].Content;
        }
    }
}
