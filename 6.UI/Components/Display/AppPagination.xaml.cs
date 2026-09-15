using System;
using System.Linq;
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
                new FrameworkPropertyMetadata(15, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPageSizeChanged));

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

        /// <summary>الرقم المعروض هو الحجم المستعمل — القائمة تتبع الخاصية ولا تتخلّف عنها.</summary>
        private static void OnPageSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pagination = (AppPagination)d;
            pagination.PopulatePageSizes();
            pagination.Render();
        }

        private void PopulatePageSizes()
        {
            if (cmbPageSize == null) return;

            _suppressEvents = true;
            cmbPageSize.Items.Clear();
            foreach (var size in (PageSizes ?? new[] { 15 }).Append(PageSize).Distinct().OrderBy(size => size))
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
                    // النمط الضمني للأزرار يفرض حشو 14,8 — داخل زر 32px يبتلع الرقم فيظهر شريطاً رفيعاً
                    // مقصوصاً بدل "1". الزر هنا مربّع صغير، محتواه يتوسّطه بلا حشو.
                    Padding = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontFamily = (FontFamily)FindResource("P.Font.Family.Numeric"),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    BorderThickness = new Thickness(1),
                    FontWeight = active ? FontWeights.SemiBold : FontWeights.Medium
                };

                // مرجع حيّ لا لقطة: FindResource تُثبِّت الفرشاة وقت الإنشاء فلا تتبع تبديل الوضع، وكانت
                // أزرار الترقيم تبقى بألوان الوضع السابق بعد التحويل للداكن.
                Bind(btn, Control.BorderBrushProperty, active ? "BrandDefault" : "OutlineDefault");
                Bind(btn, Control.BackgroundProperty, active ? "BrandDefault" : "SurfaceRaised");
                Bind(btn, Control.ForegroundProperty, active ? "TextOnBrand" : "TextPrimary");

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

            var icon = new AppIcon { Key = iconKey, Size = "Sm" };
            icon.SetResourceReference(AppIcon.BrushProperty, disabled ? "TextMuted" : "TextPrimary");

            var btn = new Button
            {
                Content = icon,
                Width = size, Height = size,
                Margin = new Thickness(gap / 2, 0, gap / 2, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                BorderThickness = new Thickness(1),
                IsEnabled = !disabled,
                Opacity = disabled ? 0.4 : 1.0
            };

            Bind(btn, Control.BackgroundProperty, "SurfaceRaised");
            Bind(btn, Control.BorderBrushProperty, "OutlineDefault");

            btn.Click += (s, e) => action();
            pnlPages.Children.Add(btn);
        }

        private static void Bind(FrameworkElement element, DependencyProperty property, string resourceKey) =>
            element.SetResourceReference(property, resourceKey);

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
