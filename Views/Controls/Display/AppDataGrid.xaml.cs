using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using PrimeERP.Core.Permissions;

namespace PrimeERP.Views.Controls.Display
{
    public partial class AppDataGrid : UserControl
    {
        public static readonly DependencyProperty ColumnsSourceProperty =
            DependencyProperty.Register(nameof(ColumnsSource), typeof(IEnumerable<GridColumn>), typeof(AppDataGrid),
                new PropertyMetadata(null, OnStructuralChanged));

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(AppDataGrid),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(AppDataGrid),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(AppDataGrid),
                new PropertyMetadata(false, OnIsLoadingChanged));

        public static readonly DependencyProperty EmptyMessageProperty =
            DependencyProperty.Register(nameof(EmptyMessage), typeof(string), typeof(AppDataGrid),
                new PropertyMetadata("لا توجد بيانات لعرضها", OnEmptyMessageChanged));

        public static readonly DependencyProperty ShowRowNumbersProperty =
            DependencyProperty.Register(nameof(ShowRowNumbers), typeof(bool), typeof(AppDataGrid),
                new PropertyMetadata(false, OnStructuralChanged));

        public static readonly DependencyProperty AllowExportProperty =
            DependencyProperty.Register(nameof(AllowExport), typeof(bool), typeof(AppDataGrid),
                new PropertyMetadata(false, OnAllowExportChanged));

        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register(nameof(SelectionMode), typeof(GridSelectionMode), typeof(AppDataGrid),
                new PropertyMetadata(GridSelectionMode.Single, OnStructuralChanged));

        public static readonly DependencyProperty ShowRowActionsProperty =
            DependencyProperty.Register(nameof(ShowRowActions), typeof(bool), typeof(AppDataGrid),
                new PropertyMetadata(false, OnStructuralChanged));

        public static readonly DependencyProperty FrozenColumnCountProperty =
            DependencyProperty.Register(nameof(FrozenColumnCount), typeof(int), typeof(AppDataGrid),
                new PropertyMetadata(0, OnFrozenColumnCountChanged));

        public static readonly DependencyProperty PageSizeProperty =
            DependencyProperty.Register(nameof(PageSize), typeof(int), typeof(AppDataGrid),
                new PropertyMetadata(15, OnPageSizeChanged));

        public IEnumerable<GridColumn> ColumnsSource     { get => (IEnumerable<GridColumn>)GetValue(ColumnsSourceProperty); set => SetValue(ColumnsSourceProperty, value); }
        public IEnumerable             ItemsSource       { get => (IEnumerable)GetValue(ItemsSourceProperty);               set => SetValue(ItemsSourceProperty, value); }
        public object                  SelectedItem      { get => GetValue(SelectedItemProperty);                          set => SetValue(SelectedItemProperty, value); }
        public bool                    IsLoading         { get => (bool)GetValue(IsLoadingProperty);                       set => SetValue(IsLoadingProperty, value); }
        public string                  EmptyMessage      { get => (string)GetValue(EmptyMessageProperty);                  set => SetValue(EmptyMessageProperty, value); }
        public bool                    ShowRowNumbers    { get => (bool)GetValue(ShowRowNumbersProperty);                  set => SetValue(ShowRowNumbersProperty, value); }
        public bool                    AllowExport       { get => (bool)GetValue(AllowExportProperty);                     set => SetValue(AllowExportProperty, value); }
        public GridSelectionMode       SelectionMode     { get => (GridSelectionMode)GetValue(SelectionModeProperty);      set => SetValue(SelectionModeProperty, value); }
        public bool                    ShowRowActions    { get => (bool)GetValue(ShowRowActionsProperty);                  set => SetValue(ShowRowActionsProperty, value); }
        public int                     FrozenColumnCount { get => (int)GetValue(FrozenColumnCountProperty);                set => SetValue(FrozenColumnCountProperty, value); }
        public int                     PageSize          { get => (int)GetValue(PageSizeProperty);                        set => SetValue(PageSizeProperty, value); }

        /// <summary>يُستدعى لكل صف ليقرر تلوينه: "danger"/"warning"/null — مفيد لتنبيهات مثل تجاوز حد الائتمان أو نفاد المخزون.</summary>
        public Func<object, string> RowHighlightSelector { get; set; }

        public IReadOnlyList<object> SelectedItems => grid.SelectedItems.Cast<object>().ToList();

        public event EventHandler<object> RowDoubleClick;
        public event EventHandler          ExportRequested;
        public event EventHandler<object>  RowEditRequested;
        public event EventHandler<object>  RowDeleteRequested;
        public event EventHandler          SelectionChanged;

        private List<object> _allItems = new();
        private List<GridColumn> _visibleColumns = new();
        private string _sortMemberPath;
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;
        private bool _suppressSelection;
        private ScrollViewer _gridScroll;
        private ScrollViewer _footerScroll;

        public AppDataGrid()
        {
            InitializeComponent();

            grid.MouseDoubleClick += (s, e) =>
            {
                if (grid.SelectedItem != null)
                    RowDoubleClick?.Invoke(this, grid.SelectedItem);
            };

            grid.SelectionChanged += (s, e) =>
            {
                if (_suppressSelection) return;
                _suppressSelection = true;
                SelectedItem = grid.SelectedItem;
                _suppressSelection = false;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            };

            pagination.PageChanged += (s, e) => RefreshPage();
            pagination.PageSizeChanged += (s, e) => { PageSize = pagination.PageSize; };

            grid.LoadingRow += Grid_LoadingRow;

            Loaded += (s, e) =>
            {
                HookScrollSync();
                Core.AppSession.PermissionsChanged += OnPermissionsChanged;
            };
            Unloaded += (s, e) => Core.AppSession.PermissionsChanged -= OnPermissionsChanged;
        }

        private void OnPermissionsChanged(object sender, EventArgs e) => RebuildColumns();

        private static void OnStructuralChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppDataGrid)d).RebuildColumns();

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppDataGrid)d).LoadItems();

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppDataGrid)d;
            if (c._suppressSelection) return;
            c._suppressSelection = true;
            c.grid.SelectedItem = e.NewValue;
            c._suppressSelection = false;
        }

        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppDataGrid)d;
            c.loadingOverlay.IsBusy = (bool)e.NewValue;
            c.UpdateVisualState();
        }

        private static void OnEmptyMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppDataGrid)d).emptyState.Message = (string)e.NewValue;

        private static void OnAllowExportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppDataGrid)d).toolbar.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;

        private static void OnFrozenColumnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppDataGrid)d).grid.FrozenColumnCount = (int)e.NewValue;

        private static void OnPageSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppDataGrid)d;
            c.pagination.PageSize = (int)e.NewValue;
            c.RefreshPage();
        }

        // ===================== Columns =====================

        private void RebuildColumns()
        {
            grid.Columns.Clear();
            grid.SelectionMode = SelectionMode == GridSelectionMode.Multiple
                ? DataGridSelectionMode.Extended
                : DataGridSelectionMode.Single;

            _visibleColumns = (ColumnsSource ?? Enumerable.Empty<GridColumn>())
                .Where(c => c.IsVisible)
                .Where(c => string.IsNullOrEmpty(c.PermissionKey) || PermissionService.Instance.Can(c.PermissionKey))
                .ToList();

            if (ShowRowNumbers)
                grid.HeadersVisibility = DataGridHeadersVisibility.All;
            else
                grid.HeadersVisibility = DataGridHeadersVisibility.Column;

            if (SelectionMode == GridSelectionMode.Multiple)
                grid.Columns.Add(BuildCheckboxColumn());

            foreach (var col in _visibleColumns)
                grid.Columns.Add(BuildDataColumn(col));

            if (ShowRowActions)
                grid.Columns.Add(BuildActionsColumn());

            grid.FrozenColumnCount = FrozenColumnCount;

            RebuildFooterColumns();
        }

        private DataGridColumn BuildCheckboxColumn()
        {
            var factory = new FrameworkElementFactory(typeof(CheckBox));
            factory.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsSelected")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1),
                Mode = BindingMode.TwoWay
            });
            factory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            factory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);

            return new DataGridTemplateColumn
            {
                Header = "",
                Width = new DataGridLength(38),
                CanUserSort = false,
                CellTemplate = new DataTemplate { VisualTree = factory }
            };
        }

        private DataGridColumn BuildDataColumn(GridColumn col)
        {
            if (col.CellTemplate != null)
            {
                return new DataGridTemplateColumn
                {
                    Header = col.Header,
                    Width = col.IsStarWidth ? new DataGridLength(col.Width, DataGridLengthUnitType.Star) : new DataGridLength(col.Width),
                    CanUserSort = col.IsSortable,
                    SortMemberPath = col.Binding,
                    CellTemplate = col.CellTemplate
                };
            }

            var binding = new Binding(col.Binding);
            if (!string.IsNullOrEmpty(col.Format))
                binding.StringFormat = col.Format;

            var alignment = col.Align switch
            {
                ColumnAlign.Center => HorizontalAlignment.Center,
                ColumnAlign.End    => HorizontalAlignment.Right,
                _                  => HorizontalAlignment.Left // Auto/Start — يُعكس بصرياً تلقائياً حسب FlowDirection
            };

            var cellStyle = new Style(typeof(TextBlock));
            cellStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, alignment));
            cellStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(12, 0, 12, 0)));

            return new DataGridTextColumn
            {
                Header = col.Header,
                Binding = binding,
                Width = col.IsStarWidth ? new DataGridLength(col.Width, DataGridLengthUnitType.Star) : new DataGridLength(col.Width),
                CanUserSort = col.IsSortable,
                SortMemberPath = col.Binding,
                ElementStyle = cellStyle,
                IsReadOnly = true
            };
        }

        private DataGridColumn BuildActionsColumn()
        {
            var panelFactory = new FrameworkElementFactory(typeof(StackPanel));
            panelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            panelFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);

            var editBtn = BuildActionButton("IconEdit", (s, item) => RowEditRequested?.Invoke(this, item));
            var deleteBtn = BuildActionButton("IconDelete", (s, item) => RowDeleteRequested?.Invoke(this, item));

            panelFactory.AppendChild(editBtn);
            panelFactory.AppendChild(deleteBtn);

            return new DataGridTemplateColumn
            {
                Header = "",
                Width = new DataGridLength(70),
                CanUserSort = false,
                CellTemplate = new DataTemplate { VisualTree = panelFactory }
            };
        }

        private FrameworkElementFactory BuildActionButton(string iconKey, Action<object, object> onClick)
        {
            var btnFactory = new FrameworkElementFactory(typeof(Button));
            btnFactory.SetValue(Button.WidthProperty, 26.0);
            btnFactory.SetValue(Button.HeightProperty, 26.0);
            btnFactory.SetValue(Button.MarginProperty, new Thickness(3, 0, 3, 0));
            btnFactory.SetValue(Button.BackgroundProperty, Brushes.Transparent);
            btnFactory.SetValue(Button.BorderThicknessProperty, new Thickness(0));
            btnFactory.SetValue(Button.CursorProperty, System.Windows.Input.Cursors.Hand);

            var pathFactory = new FrameworkElementFactory(typeof(Path));
            pathFactory.SetValue(Path.DataProperty, FindResource(iconKey));
            pathFactory.SetValue(Path.StretchProperty, Stretch.Uniform);
            pathFactory.SetValue(Path.WidthProperty, 13.0);
            pathFactory.SetValue(Path.HeightProperty, 13.0);
            pathFactory.SetValue(Path.StrokeProperty, FindResource("SubTextBrush"));
            pathFactory.SetValue(Path.StrokeThicknessProperty, 1.8);
            btnFactory.AppendChild(pathFactory);

            btnFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) =>
            {
                var item = (s as FrameworkElement)?.DataContext;
                onClick(s, item);
            }));

            return btnFactory;
        }

        // ===================== Data / Paging =====================

        private void LoadItems()
        {
            _allItems = ItemsSource?.Cast<object>().ToList() ?? new List<object>();
            ApplySort();
            pagination.CurrentPage = 1;
            RefreshPage();
        }

        private void RefreshPage()
        {
            pagination.TotalItems = _allItems.Count;

            var page = Math.Max(1, pagination.CurrentPage);
            var pageItems = _allItems
                .Skip((page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            grid.ItemsSource = pageItems;
            RefreshFooterValues();
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            var isEmpty = _allItems.Count == 0;
            emptyState.Visibility = !IsLoading && isEmpty ? Visibility.Visible : Visibility.Collapsed;
            grid.Visibility = !isEmpty || IsLoading ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Grid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (ShowRowNumbers)
            {
                var offset = (pagination.CurrentPage - 1) * pagination.PageSize;
                e.Row.Header = (offset + e.Row.GetIndex() + 1).ToString();
            }

            e.Row.Tag = RowHighlightSelector?.Invoke(e.Row.Item);
        }

        // ===================== Sorting (over the full dataset, not just the page) =====================

        private void grid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var path = e.Column.SortMemberPath;
            if (string.IsNullOrEmpty(path)) return;

            _sortDirection = e.Column.SortDirection != ListSortDirection.Ascending
                ? ListSortDirection.Ascending
                : ListSortDirection.Descending;
            _sortMemberPath = path;

            e.Column.SortDirection = _sortDirection;
            ApplySort();
            RefreshPage();
        }

        private void ApplySort()
        {
            if (string.IsNullOrEmpty(_sortMemberPath) || _allItems.Count == 0) return;

            var prop = _allItems[0]?.GetType().GetProperty(_sortMemberPath, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return;

            _allItems = _sortDirection == ListSortDirection.Ascending
                ? _allItems.OrderBy(i => prop.GetValue(i), Comparer<object>.Create(CompareValues)).ToList()
                : _allItems.OrderByDescending(i => prop.GetValue(i), Comparer<object>.Create(CompareValues)).ToList();
        }

        private static int CompareValues(object a, object b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            if (a is IComparable ca) return ca.CompareTo(b);
            return string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        // ===================== Footer totals =====================

        private void RebuildFooterColumns()
        {
            footerGrid.Columns.Clear();

            var hasFooter = _visibleColumns.Any(c => c.Footer != FooterAggregate.None);
            footerWrap.Visibility = hasFooter ? Visibility.Visible : Visibility.Collapsed;
            if (!hasFooter) return;

            if (SelectionMode == GridSelectionMode.Multiple)
                footerGrid.Columns.Add(new DataGridTextColumn { Width = new DataGridLength(38), IsReadOnly = true });

            foreach (var col in _visibleColumns)
            {
                var alignment = col.Align switch
                {
                    ColumnAlign.Center => HorizontalAlignment.Center,
                    ColumnAlign.End    => HorizontalAlignment.Right,
                    _                  => HorizontalAlignment.Left // Auto/Start — يُعكس بصرياً تلقائياً حسب FlowDirection
                };

                var cellStyle = new Style(typeof(TextBlock));
                cellStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, alignment));
                cellStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(12, 0, 12, 0)));

                footerGrid.Columns.Add(new DataGridTextColumn
                {
                    Binding = new Binding(col.Binding),
                    Width = col.IsStarWidth ? new DataGridLength(col.Width, DataGridLengthUnitType.Star) : new DataGridLength(col.Width),
                    ElementStyle = cellStyle,
                    IsReadOnly = true
                });
            }

            if (ShowRowActions)
                footerGrid.Columns.Add(new DataGridTextColumn { Width = new DataGridLength(70), IsReadOnly = true });
        }

        private void RefreshFooterValues()
        {
            if (footerWrap.Visibility != Visibility.Visible) return;

            var row = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

            foreach (var col in _visibleColumns)
            {
                if (col.Footer == FooterAggregate.None || string.IsNullOrEmpty(col.Binding))
                {
                    row[col.Binding ?? Guid.NewGuid().ToString()] = "";
                    continue;
                }

                var values = _allItems
                    .Select(i => i.GetType().GetProperty(col.Binding, BindingFlags.Public | BindingFlags.Instance)?.GetValue(i))
                    .Where(v => v != null)
                    .Select(v => Convert.ToDecimal(v, CultureInfo.InvariantCulture))
                    .ToList();

                decimal result = col.Footer switch
                {
                    FooterAggregate.Sum     => values.Sum(),
                    FooterAggregate.Count   => values.Count,
                    FooterAggregate.Average => values.Count > 0 ? values.Average() : 0,
                    _ => 0
                };

                var formatted = string.IsNullOrEmpty(col.Format) ? result.ToString(CultureInfo.InvariantCulture) : result.ToString(col.Format, CultureInfo.InvariantCulture);
                row[col.Binding] = formatted;
            }

            footerGrid.ItemsSource = new List<object> { row };
        }

        // ===================== Horizontal scroll sync between grid and footer =====================

        private void HookScrollSync()
        {
            _gridScroll = FindVisualChild<ScrollViewer>(grid);
            _footerScroll = FindVisualChild<ScrollViewer>(footerGrid);

            if (_gridScroll != null)
                _gridScroll.ScrollChanged += (s, e) =>
                {
                    if (_footerScroll != null && e.HorizontalChange != 0)
                        _footerScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
                };
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        // ===================== Toolbar =====================

        private void btnExport_Click(object sender, RoutedEventArgs e) => ExportRequested?.Invoke(this, EventArgs.Empty);
    }
}
