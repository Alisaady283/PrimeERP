using System;
using System.Windows;
using System.Windows.Controls;

namespace PrimeERP.UI.Components.Layout
{
    public partial class FilterBar : UserControl
    {
        public static readonly DependencyProperty SearchPlaceholderProperty =
            DependencyProperty.Register(nameof(SearchPlaceholder), typeof(string), typeof(FilterBar),
                new PropertyMetadata("", OnSearchPlaceholderChanged));

        public static readonly DependencyProperty FiltersContentProperty =
            DependencyProperty.Register(nameof(FiltersContent), typeof(object), typeof(FilterBar),
                new PropertyMetadata(null, OnFiltersContentChanged));

        public static readonly DependencyProperty ResultCountProperty =
            DependencyProperty.Register(nameof(ResultCount), typeof(int?), typeof(FilterBar),
                new PropertyMetadata(null, OnResultCountChanged));

        public string SearchPlaceholder { get => (string)GetValue(SearchPlaceholderProperty); set => SetValue(SearchPlaceholderProperty, value); }
        public object FiltersContent    { get => GetValue(FiltersContentProperty);              set => SetValue(FiltersContentProperty, value); }
        public int?   ResultCount       { get => (int?)GetValue(ResultCountProperty);           set => SetValue(ResultCountProperty, value); }

        /// <summary>يُطلق بعد فترة التهدئة الخاصة بـ AppSearchBox — استخدمه لتصفية البيانات فعلياً.</summary>
        public event EventHandler<string> Search;

        /// <summary>يُطلق عند الضغط على "مسح الفلتر".</summary>
        public event EventHandler ClearRequested;

        public FilterBar()
        {
            InitializeComponent();
            search.Search += (s, query) => Search?.Invoke(this, query);
        }

        private static void OnSearchPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((FilterBar)d).search.Placeholder = (string)e.NewValue;

        private static void OnFiltersContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((FilterBar)d).filtersPresenter.Content = e.NewValue;

        private static void OnResultCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (FilterBar)d;
            var count = (int?)e.NewValue;
            c.countBadge.Visibility = count.HasValue ? Visibility.Visible : Visibility.Collapsed;
            c.txtCount.Text = count.HasValue ? $"{count.Value} نتيجة" : "";
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            search.Text = "";
            ClearRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
