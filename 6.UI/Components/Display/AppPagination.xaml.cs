using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Display
{
    public partial class AppPagination : UserControl
    {
        public static readonly DependencyProperty TotalItemsProperty =
            DependencyProperty.Register(nameof(TotalItems), typeof(int), typeof(AppPagination),
                new PropertyMetadata(0, OnDataChanged));

        public static readonly DependencyProperty PageSizeProperty =
            DependencyProperty.Register(nameof(PageSize), typeof(int), typeof(AppPagination),
                new FrameworkPropertyMetadata(15, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDataChanged));

        public static readonly DependencyProperty CurrentPageProperty =
            DependencyProperty.Register(nameof(CurrentPage), typeof(int), typeof(AppPagination),
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDataChanged));

        public static readonly DependencyProperty PageSizesProperty =
            DependencyProperty.Register(nameof(PageSizes), typeof(int[]), typeof(AppPagination),
                new PropertyMetadata(new[] { 10, 15, 25, 50, 100 }, OnPageSizesChanged));

        public int   TotalItems  { get => (int)GetValue(TotalItemsProperty);  set => SetValue(TotalItemsProperty, value); }
        public int   PageSize    { get => (int)GetValue(PageSizeProperty);    set => SetValue(PageSizeProperty, value); }
        public int   CurrentPage { get => (int)GetValue(CurrentPageProperty); set => SetValue(CurrentPageProperty, value); }
        public int[] PageSizes   { get => (int[])GetValue(PageSizesProperty); set => SetValue(PageSizesProperty, value); }

        public int TotalPages => TotalItems == 0 ? 1 : (int)Math.Ceiling((double)TotalItems / Math.Max(1, PageSize));

        public event EventHandler<int> PageChanged;
        public event EventHandler<int> PageSizeChanged;

        private bool _isInitialized;
        private bool _suppressEvents;

        public AppPagination()
        {
            InitializeComponent();
            PopulatePageSizes();
            _isInitialized = true;
            Render();
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppPagination)d).Render();

        private static void OnPageSizesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppPagination)d).PopulatePageSizes();

        private void PopulatePageSizes()
        {
            if (cmbPageSize == null) return;

            _suppressEvents = true;
            cmbPageSize.Items.Clear();
            foreach (var size in PageSizes ?? new[] { 15 })
                cmbPageSize.Items.Add(size);
            cmbPageSize.SelectedItem = PageSize;
            _suppressEvents = false;
        }

        private void Render()
        {
            if (!_isInitialized || pnlPages == null) return;

            pnlPages.Children.Clear();

            int from = TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
            int to   = Math.Min(CurrentPage * PageSize, TotalItems);

            txtInfo.Text = $"عرض {from} إلى {to} من {TotalItems} عنصر";

            AddNavButton("IconChevronsRight", () => GoTo(1), CurrentPage == 1);
            AddNavButton("IconChevronRight", () => GoTo(CurrentPage - 1), CurrentPage == 1);

            int start = Math.Max(1, CurrentPage - 2);
            int end   = Math.Min(TotalPages, start + 4);
            if (end - start < 4) start = Math.Max(1, end - 4);

            var size = (double)FindResource("C.Pagination.Button.Size");
            var gap  = (double)FindResource("C.Pagination.Button.Gap");
            var fontSize = (double)FindResource("C.Pagination.Button.FontSize");

            for (int i = start; i <= end; i++)
            {
                int page = i;
                bool active = i == CurrentPage;

                var btn = new Button
                {
                    Content = i.ToString(),
                    Width = size, Height = size,
                    Margin = new Thickness(gap / 2, 0, gap / 2, 0),
                    FontSize = fontSize,
                    FontFamily = (FontFamily)FindResource("P.Font.Family.Numeric"),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    BorderThickness = new Thickness(1),
                    BorderBrush = active ? (Brush)FindResource("BrandDefault") : (Brush)FindResource("OutlineDefault"),
                    Background = active ? (Brush)FindResource("BrandDefault") : (Brush)FindResource("SurfaceDefault"),
                    Foreground = active ? (Brush)FindResource("TextOnBrand") : (Brush)FindResource("TextPrimary"),
                    FontWeight = active ? FontWeights.SemiBold : FontWeights.Medium
                };
                btn.Click += (s, e) => GoTo(page);
                pnlPages.Children.Add(btn);
            }

            AddNavButton("IconChevronLeft", () => GoTo(CurrentPage + 1), CurrentPage == TotalPages);
            AddNavButton("IconChevronsLeft", () => GoTo(TotalPages), CurrentPage == TotalPages);
        }

        // أيقونة حقيقية لا رمز نصي («‹›») — الرموز النصية تُرسَم بوزن الخط لا بوزن الأيقونات، فتبدو رفيعة وغريبة.
        private void AddNavButton(string iconKey, Action action, bool disabled)
        {
            var size = (double)FindResource("C.Pagination.Button.Size");
            var gap  = (double)FindResource("C.Pagination.Button.Gap");

            var btn = new Button
            {
                Content = new AppIcon
                {
                    Key = iconKey,
                    Size = "Sm",
                    Brush = disabled ? (Brush)FindResource("TextMuted") : (Brush)FindResource("TextPrimary")
                },
                Width = size, Height = size,
                Margin = new Thickness(gap / 2, 0, gap / 2, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = (Brush)FindResource("SurfaceDefault"),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)FindResource("OutlineDefault"),
                IsEnabled = !disabled,
                Opacity = disabled ? 0.4 : 1.0
            };
            btn.Click += (s, e) => action();
            pnlPages.Children.Add(btn);
        }

        private void GoTo(int page)
        {
            if (page < 1 || page > TotalPages || page == CurrentPage) { Render(); return; }
            CurrentPage = page;
            PageChanged?.Invoke(this, CurrentPage);
        }

        private void cmbPageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressEvents || cmbPageSize.SelectedItem is not int size) return;
            PageSize = size;
            CurrentPage = 1;
            PageSizeChanged?.Invoke(this, PageSize);
        }
    }
}
