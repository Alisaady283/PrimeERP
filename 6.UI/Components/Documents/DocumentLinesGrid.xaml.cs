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

namespace PrimeERP.UI.Components.Documents
{
    /// <summary>
    /// شبكة سطور مستند عامة (قيد/فاتورة مبيعات/فاتورة مشتريات/إذن مخزن) — أعمدتها تُبنى من List&lt;LineColumn&gt;
    /// عبر DataTemplateSelector على قوالب ثابتة معرّفة في XAML (لا FrameworkElementFactory). كل تعديل خلية
    /// يعيد الحساب (LineComputeEngine) والتحقق (LineValidationEngine) فوراً، ويحدّث صف إجماليات متزامن أفقياً
    /// مع الجدول الأساسي. لا تعرف هذه القطعة قاعدة البيانات إطلاقاً — مصادر الـ Pickers تُمرَّر من المستهلك.
    /// </summary>
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

        /// <summary>true إذا كان آخر تفاعل نقر كان على رأس الصف (تحديد صف كامل) لا خلية — يُصفّر عند أي نقر خلية.</summary>
        private bool _lastSelectionWasRowHeader;

        private const int MaxUndoSteps = 20;
        private readonly List<List<DocumentLine>> _undoStack = new();
        private readonly List<List<DocumentLine>> _redoStack = new();

        /// <summary>سبب الدخول لوضع التعديل — يحدّد سلوك المؤشر/التحديد في PreparingCellForEdit (أسلوب Excel).</summary>
        private enum EditEntryReason { Click, DoubleClick, F2, Navigation, TypedCharacter }
        private EditEntryReason _editEntryReason = EditEntryReason.Click;
        private string _pendingTypedChar = "";

        public DocumentLinesGrid()
        {
            InitializeComponent();
            Loaded += (s, e) => HookScrollSync();
        }

        // ===================== DP callbacks =====================

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

        // ===================== Columns =====================

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

        /// <summary>يستنسخ LineCellStyle المشترك (يحتفظ بإطار الخطأ الأحمر) ويضيف محاذاة هذا العمود تحديداً
        /// عبر DataGridCell.HorizontalContentAlignment — تقرأها القوالب لاحقاً بربط RelativeSource.</summary>
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

        // ===================== Cell edit commit (event-handler based — لا ConvertBack) =====================

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

        // ===================== Pickers مدمجة (F4 / زر … / كتابة كود مباشرة) =====================

        private void PickerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not DocumentLine line) return;
            var key = FindAncestor<DataGridCell>(fe)?.Column?.SortMemberPath;
            var col = ColumnsDefinition?.FirstOrDefault(c => c.Key == key);
            if (col == null) return;
            OpenPickerForCell(line, col);
        }

        private void OpenPickerForCurrentCell()
        {
            var (row, col) = GetCurrentPosition();
            if (row < 0 || ColumnsDefinition == null || Lines == null || col < 0 || col >= grid.Columns.Count) return;

            var key = grid.Columns[col].SortMemberPath;
            var lineCol = ColumnsDefinition.FirstOrDefault(c => c.Key == key);
            if (lineCol == null || lineCol.Type != LineColumnType.Picker || lineCol.PickerType == LinePickerType.None) return;

            OpenPickerForCell(Lines[row], lineCol);
        }

        private void OpenPickerForCell(DocumentLine line, LineColumn col)
        {
            if (col.PickerType == LinePickerType.None) return;

            var host = CreatePickerHost(col.PickerType, GetExcludedIds(line));
            if (host == null) return;

            host.SelectionChanged += (s, item) =>
            {
                if (item != null) ApplyPickerSelection(line, col, item);
            };
            host.OpenPicker();
        }

        private async Task CommitPickerCodeAsync(DocumentLine line, LineColumn col, string text)
        {
            var code = text?.Trim() ?? "";
            line[col.Key] = code;

            if (string.IsNullOrEmpty(code))
            {
                line.ItemId = null;
                OnCellEdited(line);
                return;
            }

            var host = CreatePickerHost(col.PickerType, new List<int>());
            var match = host == null ? null : await host.ResolveCodeAsync(code);

            if (match != null)
            {
                ApplyPickerSelection(line, col, match);
            }
            else
            {
                line.ItemId = null;
                OnCellEdited(line);
                line.Errors[col.Key] = "كود غير موجود";
                line.NotifyErrorsChanged();
            }
        }

        private void ApplyPickerSelection(DocumentLine line, LineColumn col, PickerResultItem match)
        {
            line[col.Key] = match.Code ?? "";
            line.ItemId = match.Id;
            ApplyFillsFrom(line, col, match.RawData);
            OnCellEdited(line);
        }

        private static void ApplyFillsFrom(DocumentLine line, LineColumn col, object rawData)
        {
            if (rawData == null || col.FillsFrom == null) return;
            var type = rawData.GetType();

            foreach (var kv in col.FillsFrom)
            {
                var prop = type.GetProperty(kv.Value);
                if (prop == null) continue;
                line[kv.Key] = prop.GetValue(rawData);
            }
        }

        private PickerBaseControl CreatePickerHost(LinePickerType type, List<int> excludeIds)
        {
            excludeIds ??= new List<int>();
            return type switch
            {
                LinePickerType.Account  => new AccountPicker  { DataSource = AccountDataSource,  ExcludeIds = excludeIds },
                LinePickerType.Customer => new CustomerPicker { DataSource = CustomerDataSource, ExcludeIds = excludeIds },
                LinePickerType.Supplier => new SupplierPicker { DataSource = SupplierDataSource, ExcludeIds = excludeIds },
                LinePickerType.Product  => new ProductPicker  { DataSource = ProductDataSource,  ExcludeIds = excludeIds },
                LinePickerType.Employee => new EmployeePicker { DataSource = EmployeeDataSource, ExcludeIds = excludeIds },
                _ => null
            };
        }

        private List<int> GetExcludedIds(DocumentLine currentLine) =>
            (Lines ?? Enumerable.Empty<DocumentLine>())
                .Where(l => l != currentLine && l.ItemId.HasValue)
                .Select(l => l.ItemId!.Value)
                .ToList();

        // ===================== التنقل بالكيبورد + التحديد/النسخ/اللصق بأسلوب Excel =====================

        /// <summary>
        /// يلتقط ما إذا كان النقر على رأس الصف (تحديد صف كامل) قبل أن تعالج DataGrid التحديد فعلياً،
        /// ويحدّد سبب دخول التعديل (نقرة مزدوجة أم نقرة على خلية نشطة بالفعل) لاستخدامه في PreparingCellForEdit.
        /// </summary>
        private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            _lastSelectionWasRowHeader = FindAncestor<DataGridRowHeader>(source) != null;

            if (!_lastSelectionWasRowHeader && FindAncestor<DataGridCell>(source) != null)
                _editEntryReason = e.ClickCount >= 2 ? EditEntryReason.DoubleClick : EditEntryReason.Click;
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            var inTextEditor = e.OriginalSource is TextBox or ComboBox;

            switch (e.Key)
            {
                case Key.Tab:
                    MoveToAdjacentEditableCell(shift ? -1 : 1);
                    e.Handled = true;
                    break;

                case Key.Enter when !ctrl:
                    HandleEnterKey();
                    e.Handled = true;
                    break;

                case Key.Up when !ctrl:
                    MoveVertical(-1);
                    e.Handled = true;
                    break;

                case Key.Down when !ctrl:
                    MoveVertical(1);
                    e.Handled = true;
                    break;

                case Key.D when ctrl:
                    DuplicateCurrentRow();
                    e.Handled = true;
                    break;

                case Key.Delete when ctrl:
                    DeleteCurrentRow();
                    e.Handled = true;
                    break;

                // Delete بلا Ctrl: يفرّغ التحديد الحالي (خلية/نطاق/صف عبر رأسه) بلا حذف السطر نفسه.
                // لا يعترض أثناء التحرير الفعلي داخل TextBox (يترك سلوك الحذف الطبيعي للنص).
                case Key.Delete when !ctrl && !inTextEditor:
                    SaveUndoSnapshot();
                    ClearSelectedCells();
                    RecalculateAndValidateAll();
                    e.Handled = true;
                    break;

                case Key.C when ctrl && !inTextEditor:
                    HandleCopy();
                    e.Handled = true;
                    break;

                case Key.X when ctrl && !inTextEditor:
                    HandleCut();
                    e.Handled = true;
                    break;

                case Key.V when ctrl && !inTextEditor:
                    HandlePaste();
                    e.Handled = true;
                    break;

                case Key.A when ctrl && !inTextEditor:
                    grid.SelectAllCells();
                    e.Handled = true;
                    break;

                case Key.Z when ctrl:
                    Undo();
                    e.Handled = true;
                    break;

                case Key.Y when ctrl:
                    Redo();
                    e.Handled = true;
                    break;

                case Key.F4:
                    OpenPickerForCurrentCell();
                    e.Handled = true;
                    break;

                // F2 يُترك بلا e.Handled=true حتى يبدأ DataGrid التعديل بسلوكه الافتراضي —
                // فقط نسجّل السبب هنا قبل أن يصل ذلك لـ PreparingCellForEdit (Preview تنازلي فيسبق المعالجة الداخلية).
                case Key.F2:
                    _editEntryReason = EditEntryReason.F2;
                    break;
            }
        }

        /// <summary>
        /// كتابة حرف مباشرة على خلية محددة (غير في وضع تعديل) تبدأ التعديل وتستبدل القيمة القديمة بالحرف —
        /// السلوك الوحيد المسموح له بالمسح، بخلاف كل مسارات الدخول الأخرى التي تحافظ على القيمة الحالية.
        /// </summary>
        private void Grid_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text) || char.IsControl(e.Text[0])) return;
            if (Keyboard.FocusedElement is TextBox or ComboBox) return; // بالفعل في وضع تعديل — اترك السلوك الطبيعي

            var (row, col) = GetCurrentPosition();
            if (row < 0 || col < 0 || col >= grid.Columns.Count || grid.Columns[col].IsReadOnly) return;

            _editEntryReason = EditEntryReason.TypedCharacter;
            _pendingTypedChar = e.Text;
            grid.BeginEdit();
            e.Handled = true;
        }

        /// <summary>
        /// نقطة التحكم الوحيدة بحالة المؤشر/التحديد عند دخول وضع التعديل — لا تُفرّغ Text إطلاقاً (يأتي من الـ Binding
        /// أصلاً)، فقط تضبط السلوك حسب _editEntryReason: F2/Navigation = تحديد الكل، نقر = المؤشر في النهاية،
        /// حرف مكتوب = استبدال القيمة بالحرف (الحالة الوحيدة المسموح لها بالمسح).
        /// </summary>
        private void Grid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (FindEditableInput(e.EditingElement) is not TextBox tb) return;

            switch (_editEntryReason)
            {
                case EditEntryReason.TypedCharacter:
                    tb.Text = _pendingTypedChar;
                    tb.CaretIndex = tb.Text.Length;
                    break;

                case EditEntryReason.DoubleClick:
                case EditEntryReason.Click:
                    tb.CaretIndex = tb.Text.Length;
                    break;

                case EditEntryReason.Navigation:
                case EditEntryReason.F2:
                default:
                    tb.SelectAll();
                    break;
            }

            _editEntryReason = EditEntryReason.Click;
            _pendingTypedChar = "";
        }

        /// <summary>
        /// يعتمد على CurrentCell.Item (لا SelectedItem — SelectionUnit="FullRow" لا يعني وجود تحديد فعلي
        /// إن لم يضغط المستخدم صفاً/خلية بعد)، مع رجوع احتياطي إلى SelectedItem ثم أول سطر متاح، حتى تعمل
        /// أزرار "حذف السطر"/"نسخ" الخارجية حتى لو لم يُفعَّل CurrentCell صراحةً بعد.
        /// </summary>
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

        private void MoveToAdjacentEditableCell(int direction)
        {
            if (_editableColumnIndexes.Count == 0) return;
            var (rowIndex, colIndex) = GetCurrentPosition();
            if (rowIndex < 0) return;

            var pos = _editableColumnIndexes.IndexOf(colIndex);
            if (pos < 0) pos = direction > 0 ? -1 : _editableColumnIndexes.Count;
            pos += direction;

            var newRow = rowIndex;
            if (pos < 0) { pos = _editableColumnIndexes.Count - 1; newRow = Math.Max(0, rowIndex - 1); }
            else if (pos >= _editableColumnIndexes.Count) { pos = 0; newRow = rowIndex + 1; }

            if (newRow >= grid.Items.Count)
            {
                AddRowAndFocus(_editableColumnIndexes[pos]);
                return;
            }

            SetCurrentCellAndEdit(newRow, _editableColumnIndexes[pos]);
        }

        private void HandleEnterKey()
        {
            var (row, col) = GetCurrentPosition();
            if (row < 0 || _editableColumnIndexes.Count == 0) return;

            var pos = _editableColumnIndexes.IndexOf(col);
            if (pos < 0) { MoveToAdjacentEditableCell(1); return; }

            if (pos < _editableColumnIndexes.Count - 1)
                SetCurrentCellAndEdit(row, _editableColumnIndexes[pos + 1]);
            else if (row < grid.Items.Count - 1)
                SetCurrentCellAndEdit(row + 1, _editableColumnIndexes[0]);
            else
                AddRowAndFocus(_editableColumnIndexes[0]);
        }

        private void MoveVertical(int direction)
        {
            var (row, col) = GetCurrentPosition();
            if (row < 0 || col < 0) return;

            var newRow = row + direction;
            if (newRow < 0 || newRow >= grid.Items.Count) return;

            SetCurrentCellAndEdit(newRow, col);
        }

        private void AddRowAndFocus(int colIndex)
        {
            if (Lines == null) return;
            grid.CommitEdit(DataGridEditingUnit.Cell, true);

            var newLine = new DocumentLine();
            Lines.Add(newLine);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var row = grid.Items.IndexOf(newLine);
                if (row >= 0) SetCurrentCellAndEdit(row, colIndex);
            }), DispatcherPriority.Background);
        }

        private void SetCurrentCellAndEdit(int row, int col)
        {
            if (row < 0 || row >= grid.Items.Count || col < 0 || col >= grid.Columns.Count) return;

            grid.CommitEdit(DataGridEditingUnit.Cell, true);

            var item = grid.Items[row];
            var column = grid.Columns[col];

            grid.SelectedItem = item;
            grid.CurrentCell = new DataGridCellInfo(item, column);
            grid.ScrollIntoView(item, column);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!column.IsReadOnly)
                {
                    _editEntryReason = EditEntryReason.Navigation;
                    grid.BeginEdit();
                    FocusEditingElement();
                }
            }), DispatcherPriority.Background);
        }

        /// <summary>
        /// ينقل تركيز لوحة المفاتيح فقط — حالة المؤشر/التحديد سبق ضبطها في Grid_PreparingCellForEdit
        /// (مصدر واحد لمنطق التحديد بدل تكراره هنا).
        /// </summary>
        private void FocusEditingElement()
        {
            var cell = GetCurrentCellElement();
            if (cell == null) return;

            var input = FindEditableInput(cell);
            input?.Focus();
        }

        private DataGridCell GetCurrentCellElement()
        {
            var cellInfo = grid.CurrentCell;
            if (cellInfo.Column == null) return null;

            if (grid.ItemContainerGenerator.ContainerFromItem(cellInfo.Item) is not DataGridRow row) return null;
            var presenter = FindVisualChild<DataGridCellsPresenter>(row);
            if (presenter == null) return null;

            var colIndex = grid.Columns.IndexOf(cellInfo.Column);
            return presenter.ItemContainerGenerator.ContainerFromIndex(colIndex) as DataGridCell;
        }

        private static FrameworkElement FindEditableInput(DependencyObject root)
        {
            if (root is TextBox or ComboBox or CheckBox) return (FrameworkElement)root;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var found = FindEditableInput(VisualTreeHelper.GetChild(root, i));
                if (found != null) return found;
            }
            return null;
        }

        // ===================== Ctrl+D / Ctrl+Delete =====================

        private void DuplicateCurrentRow()
        {
            var (row, col) = GetCurrentPosition();
            if (row < 0 || Lines == null) return;
            grid.CommitEdit(DataGridEditingUnit.Row, true);
            SaveUndoSnapshot();

            var clone = Lines[row].Clone();
            Lines.Insert(row + 1, clone);
            RecalculateAndValidateAll();

            var focusCol = col < 0 ? (_editableColumnIndexes.FirstOrDefault()) : col;
            Dispatcher.BeginInvoke(new Action(() => SetCurrentCellAndEdit(row + 1, focusCol)), DispatcherPriority.Background);
        }

        private void DeleteCurrentRow()
        {
            var (row, col) = GetCurrentPosition();
            if (row < 0 || Lines == null || Lines.Count == 0) return;
            grid.CommitEdit(DataGridEditingUnit.Row, true);
            SaveUndoSnapshot();

            Lines.RemoveAt(row);
            if (Lines.Count == 0) Lines.Add(new DocumentLine());
            RecalculateAndValidateAll();

            var newRow = Math.Min(row, Lines.Count - 1);
            var focusCol = col < 0 ? _editableColumnIndexes.FirstOrDefault() : col;
            Dispatcher.BeginInvoke(new Action(() => SetCurrentCellAndEdit(newRow, focusCol)), DispatcherPriority.Background);
        }

        // ===================== شريط الأزرار =====================

        private void btnAddRow_Click(object sender, RoutedEventArgs e)
        {
            if (Lines == null) return;
            SaveUndoSnapshot();
            Lines.Add(new DocumentLine());
            RecalculateAndValidateAll();
        }

        private void btnDeleteRow_Click(object sender, RoutedEventArgs e) => DeleteCurrentRow();

        private void btnDeleteAllRows_Click(object sender, RoutedEventArgs e)
        {
            if (Lines == null) return;
            SaveUndoSnapshot();
            Lines.Clear();
            Lines.Add(new DocumentLine());
            RecalculateAndValidateAll();
        }

        private void btnCopyRows_Click(object sender, RoutedEventArgs e) => HandleCopy();

        private void btnPasteRows_Click(object sender, RoutedEventArgs e) => HandlePaste();

        // ===================== تحديد الخلايا (أسلوب Excel) =====================

        /// <summary>يحوّل grid.SelectedCells لمواضع (صف, عمود) فعلية مرتّبة بالموضع لا بترتيب التحديد.</summary>
        private List<(int row, int col)> GetSelectedPositions()
        {
            return grid.SelectedCells
                .Where(c => c.Column != null && c.Item != null)
                .Select(c => (row: grid.Items.IndexOf(c.Item), col: grid.Columns.IndexOf(c.Column)))
                .Where(p => p.row >= 0 && p.col >= 0)
                .Distinct()
                .OrderBy(p => p.row).ThenBy(p => p.col)
                .ToList();
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

        // ===================== نسخ (Ctrl+C) — يعتمد على شكل التحديد =====================

        private void HandleCopy()
        {
            if (ColumnsDefinition == null || Lines == null) return;

            var positions = GetSelectedPositions();
            if (positions.Count == 0) return;

            if (_lastSelectionWasRowHeader)
            {
                var rows = positions.Select(p => p.row).Distinct().OrderBy(r => r).ToList();
                CopyRows(rows);
                return;
            }

            if (positions.Count == 1)
            {
                var (row, col) = positions[0];
                var colDef = ColumnsDefinitionAt(col);
                if (colDef == null || row < 0 || row >= Lines.Count) return;
                Clipboard.SetText(FormatForCopy(Lines[row][colDef.Key]));
                return;
            }

            CopyRange(positions);
        }

        private void CopyRows(List<int> rows)
        {
            var dataCols = EditableDataColumns();
            var sb = new StringBuilder();

            foreach (var r in rows)
            {
                if (r < 0 || r >= Lines.Count) continue;
                sb.AppendLine(string.Join("\t", dataCols.Select(c => FormatForCopy(Lines[r][c.Key]))));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void CopyRange(List<(int row, int col)> positions)
        {
            var rows = positions.Select(p => p.row).Distinct().OrderBy(r => r).ToList();
            var cols = positions.Select(p => p.col).Distinct().OrderBy(c => c).ToList();

            var sb = new StringBuilder();
            foreach (var r in rows)
            {
                var line = r >= 0 && r < Lines.Count ? Lines[r] : null;
                var cells = cols.Select(c =>
                {
                    var colDef = ColumnsDefinitionAt(c);
                    return colDef == null || line == null ? "" : FormatForCopy(line[colDef.Key]);
                });
                sb.AppendLine(string.Join("\t", cells));
            }

            Clipboard.SetText(sb.ToString());
        }

        private static string FormatForCopy(object value) => value switch
        {
            null => "",
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString()
        };

        // ===================== قص (Ctrl+X) =====================

        private void HandleCut()
        {
            HandleCopy();
            SaveUndoSnapshot();
            ClearSelectedCells();
            RecalculateAndValidateAll();
        }

        // ===================== إفراغ التحديد (Delete) — لا يحذف السطر، يفرّغ الأعمدة القابلة للتعديل فقط =====================

        private void ClearSelectedCells()
        {
            if (Lines == null) return;
            var positions = GetSelectedPositions();
            if (positions.Count == 0) return;

            if (_lastSelectionWasRowHeader)
            {
                var rows = positions.Select(p => p.row).Distinct();
                var editableCols = EditableDataColumns();
                foreach (var r in rows)
                {
                    if (r < 0 || r >= Lines.Count) continue;
                    foreach (var col in editableCols)
                        Lines[r][col.Key] = null;
                }
                return;
            }

            foreach (var (row, col) in positions)
            {
                var colDef = ColumnsDefinitionAt(col);
                if (!IsWritable(colDef) || row < 0 || row >= Lines.Count) continue;
                Lines[row][colDef.Key] = null;
            }
        }

        // ===================== لصق (Ctrl+V) — يعتمد على شكل الحافظة وشكل التحديد =====================

        private void HandlePaste()
        {
            if (ColumnsDefinition == null || Lines == null || !Clipboard.ContainsText()) return;

            var raw = Clipboard.GetText().Replace("\r\n", "\n").Replace("\r", "\n");
            if (raw.EndsWith("\n")) raw = raw[..^1];
            if (raw.Length == 0) return;

            var block = raw.Split('\n').Select(r => r.Split('\t')).ToArray();
            var isSingleValue = block.Length == 1 && block[0].Length == 1;

            var positions = GetSelectedPositions();
            if (positions.Count == 0)
            {
                var (curRow, curCol) = GetCurrentPosition();
                if (curRow < 0) return;
                positions = new List<(int, int)> { (curRow, curCol) };
            }

            SaveUndoSnapshot();

            if (isSingleValue)
            {
                var value = block[0][0];
                foreach (var (row, col) in positions)
                    PasteValueAt(row, col, value);
            }
            else if (positions.Count == 1)
            {
                PasteBlockExpanding(positions[0].row, positions[0].col, block);
            }
            else
            {
                PasteBlockClipped(positions, block);
            }

            // التحقق يعمل على كل الأسطر (حتى الملصوقة) — الخاطئة تُميّز بإطار أحمر ولا تُرفض
            RecalculateAndValidateAll();
        }

        private void PasteBlockExpanding(int startRow, int startCol, string[][] block)
        {
            for (int r = 0; r < block.Length; r++)
            {
                var targetRow = startRow + r;
                while (targetRow >= Lines.Count) Lines.Add(new DocumentLine());

                for (int c = 0; c < block[r].Length; c++)
                    PasteValueAt(targetRow, startCol + c, block[r][c]);
            }
        }

        private void PasteBlockClipped(List<(int row, int col)> positions, string[][] block)
        {
            var rows = positions.Select(p => p.row).Distinct().OrderBy(r => r).ToList();
            var cols = positions.Select(p => p.col).Distinct().OrderBy(c => c).ToList();

            for (int r = 0; r < rows.Count && r < block.Length; r++)
            {
                var srcRow = block[r];
                for (int c = 0; c < cols.Count && c < srcRow.Length; c++)
                    PasteValueAt(rows[r], cols[c], srcRow[c]);
            }
        }

        /// <summary>
        /// يحوّل النص لنوع العمود؛ الفاشل يُترك كما هو (لا يُكتب فوق القيمة الحالية) ويُميَّز الخلية بخطأ صريح
        /// بدل تخزين قيمة غير صالحة قد تكسر الحساب لاحقاً بصمت.
        /// </summary>
        private void PasteValueAt(int row, int col, string text)
        {
            var colDef = ColumnsDefinitionAt(col);
            if (!IsWritable(colDef) || row < 0 || row >= Lines.Count) return;

            var line = Lines[row];
            if (TryParseByType(text, colDef.Type, out var parsed))
            {
                line[colDef.Key] = parsed;
            }
            else
            {
                line.Errors[colDef.Key] = "قيمة غير صالحة للصق";
                line.NotifyErrorsChanged();
            }
        }

        private static bool TryParseByType(string text, LineColumnType type, out object result)
        {
            if (string.IsNullOrWhiteSpace(text)) { result = null; return true; }

            switch (type)
            {
                case LineColumnType.Integer:
                    if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var i)) { result = i; return true; }
                    result = null; return false;

                case LineColumnType.Decimal:
                case LineColumnType.Money:
                case LineColumnType.Percent:
                    if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) { result = d; return true; }
                    result = null; return false;

                default:
                    result = text;
                    return true;
            }
        }

        // ===================== تراجع/إعادة بسيط (Ctrl+Z / Ctrl+Y) — آخر 20 عملية بنيوية =====================

        private void SaveUndoSnapshot()
        {
            if (Lines == null) return;
            _undoStack.Add(Lines.Select(l => l.Clone()).ToList());
            if (_undoStack.Count > MaxUndoSteps) _undoStack.RemoveAt(0);
            _redoStack.Clear();
        }

        private void Undo()
        {
            if (_undoStack.Count == 0 || Lines == null) return;

            var previous = _undoStack[^1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _redoStack.Add(Lines.Select(l => l.Clone()).ToList());

            RestoreSnapshot(previous);
        }

        private void Redo()
        {
            if (_redoStack.Count == 0 || Lines == null) return;

            var next = _redoStack[^1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            _undoStack.Add(Lines.Select(l => l.Clone()).ToList());

            RestoreSnapshot(next);
        }

        private void RestoreSnapshot(List<DocumentLine> snapshot)
        {
            Lines.Clear();
            foreach (var line in snapshot) Lines.Add(line);
            RecalculateAndValidateAll();
        }

        // ===================== Recalculate / Validate =====================

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

        /// <summary>يُعاد ترقيم LineNo حسب الموضع الفعلي بعد أي إضافة/حذف/تكرار/لصق — عمود "#" في العرض
        /// يعتمد على DataGridRow.Header (Grid_LoadingRow) لكن LineNo نفسه قيمة حقيقية على DocumentLine لأي استخدام آخر (نسخ/تصدير).</summary>
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

        // ===================== Footer totals =====================

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
    }
}
