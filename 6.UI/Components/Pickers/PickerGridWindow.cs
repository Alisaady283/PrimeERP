using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;
using PrimeERP.Application;
using PrimeERP.Application.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;
using AppGrid = PrimeERP.UI.Components.Display.AppDataGrid;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Feedback;
using PrimeERP.UI.Components.Inputs;

namespace PrimeERP.UI.Components.Pickers
{
    /// <summary>نافذة اختيار بجدول مشتركة لكل</summary>
    public class PickerGridWindow : AppDialogWindow
    {
        public PickerResultItem SelectedResult { get; private set; }

        public event EventHandler QuickAddRequested;

        public Func<object, string> RowHighlight
        {
            get => _grid.RowHighlightSelector;
            set => _grid.RowHighlightSelector = value;
        }

        private readonly List<PickerResultItem> _allItems;
        private readonly Dictionary<object, PickerResultItem> _byRawData = new();
        private readonly AppGrid _grid;
        private readonly TextBlock _txtCount;
        private readonly string _geometryKey;

        public PickerGridWindow(string title, PickerDisplayConfig config, IEnumerable<PickerResultItem> items, bool allowQuickAdd = false)
        {
            Title = title;
            HeaderTitle = title;
            HeaderVariant = StatusVariant.Brand;
            HeaderIcon = (Geometry)FindResource("IconSearch");

            ResizeMode = ResizeMode.CanResize;
            SizeToContent = SizeToContent.Manual;
            Width = 640;
            Height = 520;

            _geometryKey = "PickerGridWindow:" + title;
            PickerWindowGeometry.TryRestore(_geometryKey, this);
            Closing += (s, e) => PickerWindowGeometry.Save(_geometryKey, this);

            _allItems = items?.ToList() ?? new List<PickerResultItem>();
            foreach (var item in _allItems)
                if (item.RawData != null) _byRawData[item.RawData] = item;

            var search = new AppSearchBox { Placeholder = LocalizationService.Get("Str.SearchPlaceholder"), Margin = new Thickness(0, 0, 0, 12) };
            search.Search += (s, term) => Filter(term);

            _grid = new AppGrid
            {
                Height = 340,
                ColumnsSource = (config?.Columns ?? new List<PickerColumn>()).Select(c => new GridColumn
                {
                    Header = c.Header,
                    Binding = c.Field,
                    Width = c.Width,
                    IsStarWidth = false,
                    Format = c.Format
                }).ToList()
            };
            _grid.RowDoubleClick += (s, item) => Choose(item);

            _txtCount = new TextBlock
            {
                FontSize = (double)FindResource("P.Font.Size.200"),
                FontFamily = (FontFamily)FindResource("P.Font.Family.Primary"),
                Foreground = (Brush)FindResource("TextMuted"),
                Margin = new Thickness(2, 8, 0, 0)
            };

            Body = new StackPanel { Children = { search, _grid, _txtCount } };

            var footerButtons = new StackPanel { Orientation = Orientation.Horizontal };

            if (allowQuickAdd)
            {
                var btnAdd = new Btn { Text = "+ جديد", Variant = "secondary", Size = "sm", Margin = new Thickness(0, 0, 8, 0) };
                btnAdd.Click += (s, e) => QuickAddRequested?.Invoke(this, EventArgs.Empty);
                footerButtons.Children.Add(btnAdd);
            }

            var btnCancel = new Btn { Text = LocalizationService.Get("Str.Cancel"), Variant = "secondary", Size = "sm" };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };
            footerButtons.Children.Add(btnCancel);

            Footer = footerButtons;

            RefreshGrid(_allItems);
        }

        public void UpsertAndSelect(PickerResultItem item)
        {
            if (item?.RawData != null)
                _byRawData[item.RawData] = item;

            SelectedResult = item;
            DialogResult = true;
            Close();
        }

        private void Filter(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                RefreshGrid(_allItems);
                return;
            }

            var filtered = _allItems.Where(i => Matches(i, term)).ToList();
            RefreshGrid(filtered);
        }

        private bool Matches(PickerResultItem item, string term) =>
            Contains(item.DisplayText, term) || Contains(item.Code, term) ||
            Contains(item.Name, term) || Contains(item.ExtraInfo, term) ||
            _grid.ColumnsSource.Any(c => Contains(GetFieldValue(item.RawData, c.Binding), term));

        private static bool Contains(string source, string term) =>
            !string.IsNullOrEmpty(source) && source.Contains(term, StringComparison.OrdinalIgnoreCase);

        private static string GetFieldValue(object raw, string field)
        {
            if (raw == null || string.IsNullOrEmpty(field)) return null;
            return raw.GetType().GetProperty(field)?.GetValue(raw)?.ToString();
        }

        private void RefreshGrid(List<PickerResultItem> items)
        {
            _grid.ItemsSource = items.Select(i => i.RawData).ToList();
            _txtCount.Text = $"{items.Count} نتيجة";
        }

        private void Choose(object rawItem)
        {
            if (rawItem != null && _byRawData.TryGetValue(rawItem, out var result))
            {
                SelectedResult = result;
                DialogResult = true;
                Close();
            }
        }

        protected override void OnEnterPressed()
        {
            if (_grid.SelectedItem != null)
                Choose(_grid.SelectedItem);
        }
    }
}
