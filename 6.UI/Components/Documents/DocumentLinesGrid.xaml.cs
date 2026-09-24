using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PrimeERP.Domain.Entities;
using PrimeERP.UI.Components.Pickers;
using PrimeERP.UI.Components;

namespace PrimeERP.UI.Components.Documents
{
    /// <summary>شبكة سطور مستند عامة</summary>
    public partial class DocumentLinesGrid : UserControl
    {
        public static readonly DependencyProperty LinesProperty =
            DependencyProperty.Register(nameof(Lines), typeof(ObservableCollection<DocumentLine>), typeof(DocumentLinesGrid),
                new PropertyMetadata(null, OnLinesChanged));

        public static readonly DependencyProperty ColumnsDefinitionProperty =
            DependencyProperty.Register(nameof(ColumnsDefinition), typeof(List<LineColumn>), typeof(DocumentLinesGrid),
                new PropertyMetadata(null, OnColumnsChanged));

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(nameof(Mode), typeof(DocumentLinesMode), typeof(DocumentLinesGrid),
                new PropertyMetadata(DocumentLinesMode.SalesInvoice, OnModeChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(DocumentLinesGrid),
                new PropertyMetadata(false, OnColumnsChanged));

        public static readonly DependencyProperty DefaultLinesCountProperty =
            DependencyProperty.Register(nameof(DefaultLinesCount), typeof(int), typeof(DocumentLinesGrid),
                new PropertyMetadata(0, OnDefaultLinesCountChanged));

        public static readonly DependencyProperty AccountDataSourceProperty =
            DependencyProperty.Register(nameof(AccountDataSource), typeof(IPickerDataSource<Account>), typeof(DocumentLinesGrid));

        public static readonly DependencyProperty CustomerDataSourceProperty =
            DependencyProperty.Register(nameof(CustomerDataSource), typeof(IPickerDataSource<Customer>), typeof(DocumentLinesGrid));

        public static readonly DependencyProperty SupplierDataSourceProperty =
            DependencyProperty.Register(nameof(SupplierDataSource), typeof(IPickerDataSource<Supplier>), typeof(DocumentLinesGrid));

        public static readonly DependencyProperty ProductDataSourceProperty =
            DependencyProperty.Register(nameof(ProductDataSource), typeof(IPickerDataSource<Product>), typeof(DocumentLinesGrid));

        public static readonly DependencyProperty EmployeeDataSourceProperty =
            DependencyProperty.Register(nameof(EmployeeDataSource), typeof(IPickerDataSource<Employee>), typeof(DocumentLinesGrid));

        public ObservableCollection<DocumentLine> Lines             { get => (ObservableCollection<DocumentLine>)GetValue(LinesProperty);             set => SetValue(LinesProperty, value); }
        public List<LineColumn>                   ColumnsDefinition { get => (List<LineColumn>)GetValue(ColumnsDefinitionProperty);                   set => SetValue(ColumnsDefinitionProperty, value); }
        public DocumentLinesMode                  Mode              { get => (DocumentLinesMode)GetValue(ModeProperty);                                set => SetValue(ModeProperty, value); }
        public bool                               IsReadOnly        { get => (bool)GetValue(IsReadOnlyProperty);                                       set => SetValue(IsReadOnlyProperty, value); }
        public int                                DefaultLinesCount { get => (int)GetValue(DefaultLinesCountProperty);                                 set => SetValue(DefaultLinesCountProperty, value); }

        public IPickerDataSource<Account>  AccountDataSource  { get => (IPickerDataSource<Account>)GetValue(AccountDataSourceProperty);   set => SetValue(AccountDataSourceProperty, value); }
        public IPickerDataSource<Customer> CustomerDataSource { get => (IPickerDataSource<Customer>)GetValue(CustomerDataSourceProperty); set => SetValue(CustomerDataSourceProperty, value); }
        public IPickerDataSource<Supplier> SupplierDataSource { get => (IPickerDataSource<Supplier>)GetValue(SupplierDataSourceProperty); set => SetValue(SupplierDataSourceProperty, value); }
        public IPickerDataSource<Product>  ProductDataSource  { get => (IPickerDataSource<Product>)GetValue(ProductDataSourceProperty);   set => SetValue(ProductDataSourceProperty, value); }
        public IPickerDataSource<Employee> EmployeeDataSource { get => (IPickerDataSource<Employee>)GetValue(EmployeeDataSourceProperty); set => SetValue(EmployeeDataSourceProperty, value); }

        public bool         IsValid          { get; private set; } = true;
        public List<string> ValidationErrors { get; private set; } = new();

        public event EventHandler<DocumentLine> LineChanged;
        public event EventHandler                TotalsChanged;
        public event EventHandler                ValidationChanged;

        private bool _isRecalculating;
        private bool _settingLines;
        private ScrollViewer _gridScroll;
        private ScrollViewer _footerScroll;
        private List<int> _editableColumnIndexes = new();

        private bool _lastSelectionWasRowHeader;

        private const int MaxUndoSteps = 20;
        private readonly List<List<DocumentLine>> _undoStack = new();
        private readonly List<List<DocumentLine>> _redoStack = new();

        /// <summary>سبب الدخول لوضع التعديل</summary>
        private enum EditEntryReason { Click, DoubleClick, F2, Navigation, TypedCharacter }
        private EditEntryReason _editEntryReason = EditEntryReason.Click;
        private string _pendingTypedChar = "";

        public DocumentLinesGrid()
        {
            InitializeComponent();
            Loaded += (s, e) => HookScrollSync();
        }

        private static void OnLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (DocumentLinesGrid)d;
            if (c._settingLines) return;
            c._settingLines = true;
            try
            {
                c.EnsureDefaultLines();
                c.grid.ItemsSource = c.Lines;
                c.RecalculateAndValidateAll();
            }
            finally { c._settingLines = false; }
        }

        private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (DocumentLinesGrid)d;
            c.grid.IsReadOnly = c.IsReadOnly;
            c.RebuildColumns();
            c.RecalculateAndValidateAll();
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((DocumentLinesGrid)d).RecalculateAndValidateAll();

        private static void OnDefaultLinesCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((DocumentLinesGrid)d).EnsureDefaultLines();

        private void EnsureDefaultLines()
        {
            if (Lines == null)
                Lines = new ObservableCollection<DocumentLine>();

            while (Lines.Count < DefaultLinesCount)
                Lines.Add(new DocumentLine());
        }

        private void RebuildColumns()
        {
            grid.Columns.Clear();
            _editableColumnIndexes = new List<int>();
            if (ColumnsDefinition == null) return;

            foreach (var col in ColumnsDefinition)
                grid.Columns.Add(BuildColumn(col));

            _editableColumnIndexes = grid.Columns
                .Select((c, idx) => (c, idx))
                .Where(x => !x.c.IsReadOnly)
                .Select(x => x.idx)
                .ToList();

            RebuildFooterColumns();
        }

        private DataGridColumn BuildColumn(LineColumn col)
        {
            bool readOnly = IsReadOnly || col.IsReadOnly ||
                            col.Type == LineColumnType.RowNumber || col.Type == LineColumnType.Computed;

            var column = new DataGridTemplateColumn
            {
                Header = col.Header,
                Width = col.IsStarWidth ? new DataGridLength(col.Width, DataGridLengthUnitType.Star) : new DataGridLength(col.Width),
                SortMemberPath = col.Key,
                CanUserSort = false,
                IsReadOnly = readOnly,
                CellStyle = BuildCellStyle(col),
                CellTemplateSelector = new LineCellTemplateSelector { ColumnType = col.Type, IsEditing = false, Owner = this }
            };

            if (!readOnly)
                column.CellEditingTemplateSelector = new LineCellTemplateSelector { ColumnType = col.Type, IsEditing = true, Owner = this };

            return column;
        }

        private Style BuildCellStyle(LineColumn col)
        {
            var baseStyle = (Style)FindResource("LineCellStyle");
            var style = new Style(typeof(DataGridCell), baseStyle);
            style.Setters.Add(new Setter(DataGridCell.HorizontalContentAlignmentProperty, col.ResolveHorizontalAlignment()));
            return style;
        }

        private void Grid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString(CultureInfo.InvariantCulture);
        }

        private async void TextCell_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not DocumentLine line) return;

            var key = FindAncestor<DataGridCell>(fe)?.Column?.SortMemberPath;
            if (string.IsNullOrEmpty(key)) return;

            var col = ColumnsDefinition?.FirstOrDefault(c => c.Key == key);
            var text = fe switch { TextBox tb => tb.Text, ComboBox cb => cb.Text, _ => null };
            if (text == null) return;

            if (col?.Type == LineColumnType.Picker && col.PickerType != LinePickerType.None)
            {
                await CommitPickerCodeAsync(line, col, text);
                return;
            }

            line[key] = ParseByType(text, col?.Type ?? LineColumnType.Text);
            OnCellEdited(line);
        }

        private void CheckCell_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb || cb.DataContext is not DocumentLine line) return;

            var key = FindAncestor<DataGridCell>(cb)?.Column?.SortMemberPath;
            if (string.IsNullOrEmpty(key)) return;

            line[key] = cb.IsChecked == true;
            OnCellEdited(line);
        }

        private static object ParseByType(string text, LineColumnType type) => type switch
        {
            LineColumnType.Integer =>
                int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0,
            LineColumnType.Decimal or LineColumnType.Money or LineColumnType.Percent =>
                decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m,
            _ => text
        };

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null && current is not T)
                current = VisualTreeHelper.GetParent(current);
            return current as T;
        }

        private (int row, int col) GetCurrentPosition()
        {
            var cell = grid.CurrentCell;
            if (cell.Column != null && cell.Item != null)
                return (grid.Items.IndexOf(cell.Item), grid.Columns.IndexOf(cell.Column));

            var fallbackCol = _editableColumnIndexes.Count > 0 ? _editableColumnIndexes[0] : 0;

            if (grid.SelectedItem != null)
                return (grid.Items.IndexOf(grid.SelectedItem), fallbackCol);

            if (grid.Items.Count > 0)
                return (0, fallbackCol);

            return (-1, -1);
        }

        private LineColumn ColumnsDefinitionAt(int gridColIndex)
        {
            if (ColumnsDefinition == null || gridColIndex < 0 || gridColIndex >= grid.Columns.Count) return null;
            var key = grid.Columns[gridColIndex].SortMemberPath;
            return ColumnsDefinition.FirstOrDefault(c => c.Key == key);
        }

        private static bool IsWritable(LineColumn col) =>
            col != null && !col.IsReadOnly && col.Type != LineColumnType.RowNumber && col.Type != LineColumnType.Computed;

        private List<LineColumn> EditableDataColumns() =>
            ColumnsDefinition?.Where(IsWritable).ToList() ?? new List<LineColumn>();

        private void OnCellEdited(DocumentLine line)
        {
            if (_isRecalculating || ColumnsDefinition == null) return;
            _isRecalculating = true;
            try
            {
                LineComputeEngine.Recalculate(line, ColumnsDefinition);
                LineValidationEngine.Validate(line, ColumnsDefinition, BuildContext());
                line.NotifyErrorsChanged();

                RefreshTotalsFooter();
                RecomputeIsValid();

                LineChanged?.Invoke(this, line);
                TotalsChanged?.Invoke(this, EventArgs.Empty);
            }
            finally { _isRecalculating = false; }
        }

        private void RecalculateAndValidateAll()
        {
            if (Lines == null || ColumnsDefinition == null) return;

            RenumberLines();

            foreach (var line in Lines)
            {
                LineComputeEngine.Recalculate(line, ColumnsDefinition);
                LineValidationEngine.Validate(line, ColumnsDefinition, BuildContext());
                line.NotifyErrorsChanged();
            }

            RefreshTotalsFooter();
            RecomputeIsValid();
            TotalsChanged?.Invoke(this, EventArgs.Empty);
        }

        private DocumentLinesContext BuildContext() => new()
        {
            AllLines = Lines?.ToList() ?? new List<DocumentLine>(),
            DuplicateCheckKey = Mode == DocumentLinesMode.Journal ? null : "ItemCode",
            Mode = Mode
        };

        private void RenumberLines()
        {
            if (Lines == null) return;
            for (int i = 0; i < Lines.Count; i++)
                Lines[i].LineNo = i + 1;
        }

        private void RecomputeIsValid()
        {
            var errors = (Lines ?? Enumerable.Empty<DocumentLine>())
                .Where(l => !l.IsEmpty)
                .SelectMany(l => l.Errors.Values)
                .ToList();

            IsValid = errors.Count == 0;
            ValidationErrors = errors;
            ValidationChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RebuildFooterColumns()
        {
            footerGrid.Columns.Clear();
            if (ColumnsDefinition == null) return;

            foreach (var col in ColumnsDefinition)
            {
                var cellStyle = new Style(typeof(TextBlock));
                cellStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
                cellStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(10, 0, 10, 0)));

                footerGrid.Columns.Add(new DataGridTextColumn
                {
                    Binding = new Binding(col.Key),
                    Width = col.IsStarWidth ? new DataGridLength(col.Width, DataGridLengthUnitType.Star) : new DataGridLength(col.Width),
                    IsReadOnly = true,
                    ElementStyle = cellStyle
                });
            }
        }

        private void RefreshTotalsFooter()
        {
            if (ColumnsDefinition == null) { footerWrap.Visibility = Visibility.Collapsed; return; }

            var footerCols = ColumnsDefinition.Where(c => c.Footer != LineColumnFooter.None).ToList();
            footerWrap.Visibility = footerCols.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (footerCols.Count == 0) return;

            var nonEmpty = (Lines ?? Enumerable.Empty<DocumentLine>()).Where(l => !l.IsEmpty).ToList();
            var row = new ExpandoObject() as IDictionary<string, object>;

            foreach (var col in ColumnsDefinition)
            {
                if (col.Footer == LineColumnFooter.None)
                {
                    row[col.Key] = "";
                    continue;
                }

                row[col.Key] = col.Footer switch
                {
                    LineColumnFooter.Sum   => nonEmpty.Sum(l => ToDecimal(l[col.Key])).ToString("N2", CultureInfo.InvariantCulture),
                    LineColumnFooter.Count => nonEmpty.Count(l => l[col.Key] != null).ToString(CultureInfo.InvariantCulture),
                    _ => ""
                };
            }

            footerGrid.ItemsSource = new List<object> { row };
        }

        private static decimal ToDecimal(object value) => value switch
        {
            decimal d => d,
            int i => i,
            double db => (decimal)db,
            _ => 0m
        };

        private void HookScrollSync()
        {
            _gridScroll = VisualTree.FindChild<ScrollViewer>(grid);
            _footerScroll = VisualTree.FindChild<ScrollViewer>(footerGrid);

            if (_gridScroll != null)
                _gridScroll.ScrollChanged += (s, e) =>
                {
                    if (_footerScroll != null && e.HorizontalChange != 0)
                        _footerScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
                };
        }

    }
}
