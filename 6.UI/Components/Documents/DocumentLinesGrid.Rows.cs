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
    }
}
