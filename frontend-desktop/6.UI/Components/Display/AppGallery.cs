using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.Components.Display
{
    /// <summary>طريقتا عرض المعرض</summary>
    public enum GalleryView { Tiles, Details }

    /// <summary>عنصرٌ في المعرض: صورته Base64 (ImageData)، وإلا أيقونة البديل</summary>
    public sealed record GalleryItem(object Key, string Title, string Hint, string Value, string ImageData, string Details = null);

    /// <summary>
    /// معرضٌ بطريقتين — مربّعاتٌ بالصورة والاسم والقيمة، أو صفوفُ تفاصيل — نظير Gallery في الويب.
    /// لا يعرف وحدةً بعينها: عناصره من خاصيته، والنقر حدثٌ بالعنصر. يدخل بالتتابع، ويرتفع عند المرور.
    /// </summary>
    public class AppGallery : UserControl
    {
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items), typeof(IEnumerable<GalleryItem>), typeof(AppGallery), new PropertyMetadata(null, (d, _) => ((AppGallery)d).Rebuild()));

        public static readonly DependencyProperty ViewProperty =
            DependencyProperty.Register(nameof(View), typeof(GalleryView), typeof(AppGallery), new PropertyMetadata(GalleryView.Tiles, (d, _) => ((AppGallery)d).Rebuild()));

        public static readonly DependencyProperty PlaceholderIconProperty =
            DependencyProperty.Register(nameof(PlaceholderIcon), typeof(string), typeof(AppGallery), new PropertyMetadata("IconProducts"));

        public IEnumerable<GalleryItem> Items { get => (IEnumerable<GalleryItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
        public GalleryView View { get => (GalleryView)GetValue(ViewProperty); set => SetValue(ViewProperty, value); }
        public string PlaceholderIcon { get => (string)GetValue(PlaceholderIconProperty); set => SetValue(PlaceholderIconProperty, value); }

        /// <summary>نقرةٌ واحدة: تحديد العنصر (وفي نقطة البيع إضافته)</summary>
        public event EventHandler<GalleryItem> ItemClick;

        /// <summary>نقرتان: فتح العنصر — كالنقر المزدوج على صفّ الجدول</summary>
        public event EventHandler<GalleryItem> ItemOpen;

        /// <summary>المحدَّد يبقى بخلفية التحديد حتى يُحدَّد غيره</summary>
        private Border _selected;

        /// <summary>أدنى عرضٍ للمربّع والفاصل بينها: minmax(9.5rem,1fr) وgap-3 في الويب — والأعمدة ما يتّسع له العرض</summary>
        private const double MinTile = 152, TileGap = 12;

        private readonly ScrollViewer _scroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };

        private readonly UniformGrid _tiles = new() { VerticalAlignment = VerticalAlignment.Top };

        public AppGallery()
        {
            Content = _scroll;
            SizeChanged += (_, __) => Columns();
        }

        /// <summary>عدد الأعمدة من العرض المتاح، والمربّعات تتمدّد لتملأه</summary>
        private void Columns()
        {
            var width = _scroll.ViewportWidth > 0 ? _scroll.ViewportWidth : ActualWidth;
            _tiles.Columns = Math.Max(1, (int)((width - TileGap) / (MinTile + TileGap)));
        }

        private void Rebuild()
        {
            var items = Items?.ToList() ?? new List<GalleryItem>();
            _tiles.Children.Clear();
            Panel panel = View == GalleryView.Tiles ? _tiles : new StackPanel { Margin = new Thickness(0, 4, 0, 4) };
            _tiles.Margin = new Thickness(TileGap / 2);
            for (var i = 0; i < items.Count; i++)
            {
                var element = View == GalleryView.Tiles ? Tile(items[i]) : Row(items[i]);
                panel.Children.Add(element);
                Motion.Enter(element, Motion.Stagger(i, items.Count));
            }
            _scroll.Content = panel;
            _scroll.ScrollToTop();
            Columns();
        }

        /// <summary>الصورة خلفيةً بحوافّ الإطار، وإلا أيقونة البديل في وسطه</summary>
        private Border Picture(GalleryItem item, double width, double height, string radius)
        {
            var frame = new Border { Width = width, Height = height };
            frame.SetResourceReference(Border.CornerRadiusProperty, radius);
            var image = ImageData.Decode(item.ImageData);
            if (image != null) frame.Background = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
            else
            {
                frame.SetResourceReference(Border.BackgroundProperty, "SurfaceSunken");
                frame.Child = new AppIcon { Key = PlaceholderIcon, Size = "Lg", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            }
            return frame;
        }

        private static TextBlock Caption(string text, string size, string brush, string weight, int lines = 1)
        {
            var block = new TextBlock { Text = text ?? "", TextTrimming = TextTrimming.CharacterEllipsis, TextWrapping = lines > 1 ? TextWrapping.Wrap : TextWrapping.NoWrap };
            if (lines > 1) block.MaxHeight = lines * 18;
            block.SetResourceReference(TextBlock.FontSizeProperty, size);
            block.SetResourceReference(TextBlock.ForegroundProperty, brush);
            block.SetResourceReference(TextBlock.FontWeightProperty, weight);
            return block;
        }

        /// <summary>مربّع: الصورة 4:3 ثم الاسم في سطرين والقيمة بلون الهوية</summary>
        private FrameworkElement Tile(GalleryItem item)
        {
            var text = new StackPanel { Margin = new Thickness(10, 8, 10, 10) };
            text.Children.Add(Caption(item.Title, "P.Font.Size.200", "TextPrimary", "P.Font.Weight.Medium", lines: 2));
            if (!string.IsNullOrEmpty(item.Value))
            {
                var value = Caption(item.Value, "P.Font.Size.300", "BrandDefault", "P.Font.Weight.Bold");
                value.Margin = new Thickness(0, 4, 0, 0);
                text.Children.Add(value);
            }

            var body = new StackPanel();
            var picture = Picture(item, double.NaN, MinTile * 3 / 4, "P.Radius.Lg");
            body.Children.Add(picture);
            body.Children.Add(text);

            var card = new Border { Margin = new Thickness(TileGap / 2), Child = body, Cursor = Cursors.Hand, BorderThickness = new Thickness(1), VerticalAlignment = VerticalAlignment.Top };
            card.SizeChanged += (_, e) => { if (e.WidthChanged) picture.Height = Math.Max(e.NewSize.Width - 2, 0) * 3 / 4; };
            card.SetResourceReference(Border.BackgroundProperty, "SurfaceDefault");
            card.Tag = "SurfaceDefault";
            card.SetResourceReference(Border.BorderBrushProperty, "OutlineDefault");
            card.SetResourceReference(Border.CornerRadiusProperty, "P.Radius.Lg");
            Interact(card, item, lift: true);
            return card;
        }

        /// <summary>صفّ تفاصيل: صورةٌ صغيرة · الاسم وتلميحه · القيمة</summary>
        private FrameworkElement Row(GalleryItem item)
        {
            var grid = new Grid { Margin = new Thickness(8, 6, 8, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var picture = Picture(item, 40, 40, "P.Radius.Md");
            var names = new StackPanel { Margin = new Thickness(12, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
            names.Children.Add(Caption(item.Title, "P.Font.Size.300", "TextPrimary", "P.Font.Weight.Medium"));
            if (!string.IsNullOrEmpty(item.Hint)) names.Children.Add(Caption(item.Hint, "P.Font.Size.100", "TextTertiary", "P.Font.Weight.Normal"));
            var value = Caption(item.Value, "P.Font.Size.300", "BrandDefault", "P.Font.Weight.Bold");
            value.VerticalAlignment = VerticalAlignment.Center;

            Grid.SetColumn(names, 1);
            Grid.SetColumn(value, 2);
            grid.Children.Add(picture);
            grid.Children.Add(names);
            grid.Children.Add(value);

            var row = new Border { Child = grid, Cursor = Cursors.Hand, Background = Brushes.Transparent, BorderThickness = new Thickness(0, 0, 0, 1), Tag = "SurfaceDefault" };
            row.SetResourceReference(Border.BorderBrushProperty, "OutlineSubtle");
            Interact(row, item, lift: false);
            return row;
        }

        /// <summary>المرور يبرز الإطار (ويرفع المربّع)، والضغط يصغّره قليلاً، والإفلات يختار</summary>
        private void Interact(Border element, GalleryItem item, bool lift)
        {
            var scale = new ScaleTransform(1, 1);
            var shift = new TranslateTransform();
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            element.RenderTransform = new TransformGroup { Children = { scale, shift } };
            if (!string.IsNullOrWhiteSpace(item.Details)) element.ToolTip = item.Details;
            element.MouseEnter += (_, __) =>
            {
                if (lift) element.SetResourceReference(Border.BorderBrushProperty, "BrandDefault");
                else if (element != _selected) element.SetResourceReference(Border.BackgroundProperty, "TableRowHover");
                if (lift) shift.BeginAnimation(TranslateTransform.YProperty, Motion.To(-2, Motion.Short3, Motion.Emphasized));
            };
            element.MouseLeave += (_, __) =>
            {
                if (lift) element.SetResourceReference(Border.BorderBrushProperty, "OutlineDefault");
                else if (element != _selected) element.Background = Brushes.Transparent;
                shift.BeginAnimation(TranslateTransform.YProperty, Motion.To(0, Motion.Short3, Motion.Emphasized));
                Press(scale, 1);
            };
            element.MouseLeftButtonDown += (_, e) =>
            {
                Press(scale, 0.97);
                if (e.ClickCount == 2) ItemOpen?.Invoke(this, item);
            };
            element.MouseLeftButtonUp += (_, __) =>
            {
                Press(scale, 1);
                Select(element);
                ItemClick?.Invoke(this, item);
            };
        }

        private void Select(Border element)
        {
            if (_selected != null && _selected != element) _selected.SetResourceReference(Border.BackgroundProperty, _selected.Tag as string ?? "SurfaceDefault");
            _selected = element;
            element.SetResourceReference(Border.BackgroundProperty, "StateSelectedBg");
        }

        private static void Press(ScaleTransform scale, double to)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, Motion.To(to, Motion.Short3, Motion.Emphasized));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, Motion.To(to, Motion.Short3, Motion.Emphasized));
        }
    }
}
