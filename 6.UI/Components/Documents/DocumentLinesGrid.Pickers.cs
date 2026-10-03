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
                line.Errors[col.Key] = LocalizationService.Get("Str.Line.CodeNotFound");
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
    }
}
