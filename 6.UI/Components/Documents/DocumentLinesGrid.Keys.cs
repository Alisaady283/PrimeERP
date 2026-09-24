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
    /// <summary>شبكة سطور مستند</summary>
    public partial class DocumentLinesGrid
    {
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

                case Key.F2:
                    _editEntryReason = EditEntryReason.F2;
                    break;
            }
        }

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
            var presenter = VisualTree.FindChild<DataGridCellsPresenter>(row);
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
    }
}
