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
using PrimeERP.Platform.Localization;

namespace PrimeERP.UI.Components.Documents
{
    /// <summary>شبكة سطور مستند</summary>
    public partial class DocumentLinesGrid
    {
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

        private void HandleCut()
        {
            HandleCopy();
            SaveUndoSnapshot();
            ClearSelectedCells();
            RecalculateAndValidateAll();
        }

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
                line.Errors[colDef.Key] = LocalizationService.Get("Str.Line.PasteInvalid");
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
    }
}
