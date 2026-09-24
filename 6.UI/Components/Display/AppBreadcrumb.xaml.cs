using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PrimeERP.UI.Components.Display
{
    /// <summary>عرض AppBreadcrumb</summary>
    public partial class AppBreadcrumb : UserControl
    {
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items), typeof(IEnumerable<string>), typeof(AppBreadcrumb),
                new PropertyMetadata(null, OnItemsChanged));

        public IEnumerable<string> Items { get => (IEnumerable<string>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

        public event EventHandler<int> ItemClick;

        public AppBreadcrumb()
        {
            InitializeComponent();
        }

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppBreadcrumb)d).Rebuild();

        private void Rebuild()
        {
            pnlItems.Children.Clear();
            var items = Items?.ToList() ?? new List<string>();

            for (int i = 0; i < items.Count; i++)
            {
                bool isLast = i == items.Count - 1;
                int index = i;

                var text = new TextBlock
                {
                    Text = items[i],
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = (double)FindResource("P.Font.Size.200"),
                    FontFamily = (FontFamily)FindResource("P.Font.Family.Primary"),

                    FontWeight = isLast ? FontWeights.SemiBold : FontWeights.Normal
                };
                text.SetResourceReference(TextBlock.ForegroundProperty, isLast ? "TextPrimary" : "TextMuted");

                if (!isLast)
                {
                    text.Cursor = System.Windows.Input.Cursors.Hand;
                    text.MouseLeftButtonUp += (s, e) => ItemClick?.Invoke(this, index);
                }

                pnlItems.Children.Add(text);

                if (!isLast)
                {
                    pnlItems.Children.Add(new Path
                    {
                        Data = (Geometry)FindResource("IconChevronLeft"),
                        Width = 9, Height = 9,
                        Stretch = Stretch.Uniform,

                        StrokeThickness = 2,
                        Margin = new Thickness(8, 0, 8, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }
            }
        }
    }
}
