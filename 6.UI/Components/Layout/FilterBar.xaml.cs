using System;
using System.Windows;
using System.Windows.Controls;
using PrimeERP.Platform.Localization;

namespace PrimeERP.UI.Components.Layout
{
    /// <summary>تخطيط FilterBar</summary>
    public partial class FilterBar : UserControl
    {
        public static readonly DependencyProperty SearchPlaceholderProperty =
            DependencyProperty.Register(nameof(SearchPlaceholder), typeof(string), typeof(FilterBar),
                new PropertyMetadata("", OnSearchPlaceholderChanged));

        public static readonly DependencyProperty ActionsContentProperty =
            DependencyProperty.Register(nameof(ActionsContent), typeof(object), typeof(FilterBar),
                new PropertyMetadata(null, OnActionsContentChanged));

        public static readonly DependencyProperty FiltersContentProperty =
            DependencyProperty.Register(nameof(FiltersContent), typeof(object), typeof(FilterBar),
                new PropertyMetadata(null, OnFiltersContentChanged));

        public static readonly DependencyProperty ResultCountProperty =
            DependencyProperty.Register(nameof(ResultCount), typeof(int?), typeof(FilterBar),
                new PropertyMetadata(null, OnResultCountChanged));

        public string SearchPlaceholder { get => (string)GetValue(SearchPlaceholderProperty); set => SetValue(SearchPlaceholderProperty, value); }
        public object FiltersContent    { get => GetValue(FiltersContentProperty);              set => SetValue(FiltersContentProperty, value); }

        public object ActionsContent    { get => GetValue(ActionsContentProperty);              set => SetValue(ActionsContentProperty, value); }
        public int?   ResultCount       { get => (int?)GetValue(ResultCountProperty);           set => SetValue(ResultCountProperty, value); }

        public event EventHandler<string> Search;

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

        private static void OnActionsContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((FilterBar)d).actionsPresenter.Content = e.NewValue;

        private static void OnResultCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (FilterBar)d;
            var count = (int?)e.NewValue;
            c.countBadge.Visibility = count.HasValue ? Visibility.Visible : Visibility.Collapsed;
            c.txtCount.Text = count.HasValue ? LocalizationService.Get("Str.Results", count.Value) : "";
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            search.Text = "";
            ClearRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
