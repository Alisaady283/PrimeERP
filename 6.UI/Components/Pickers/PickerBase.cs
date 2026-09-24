using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PrimeERP.UI.Components.Pickers
{
    /// <summary>الطبقة المعمَّمة فوق PickerBaseControl</summary>
    public class PickerBase<T> : PickerBaseControl where T : class
    {
        public IPickerDataSource<T> DataSource { get; set; }

        public T SelectedItem { get; private set; }

        public new event EventHandler<T> SelectionChanged;

        public PickerBase()
        {
            base.SelectionChanged += (s, item) =>
            {
                SelectedItem = item?.RawData as T;
                SelectionChanged?.Invoke(this, SelectedItem);
            };
        }

        protected override async Task<IEnumerable<PickerResultItem>> SearchAsync(string term)
        {
            if (DataSource == null) return Enumerable.Empty<PickerResultItem>();

            var items = await DataSource.SearchAsync(term, 50);
            var config = DataSource.GetDisplayConfig();
            return items.Select(i => ToResultItem(i, config));
        }

        protected override async Task<PickerResultItem> ResolveExactCodeAsync(string code)
        {
            if (DataSource == null) return null;

            var item = await DataSource.GetByCodeAsync(code);
            return item == null ? null : ToResultItem(item, DataSource.GetDisplayConfig());
        }

        protected void CommitSelection(T item)
        {
            if (item == null)
            {
                SelectResult(null);
                return;
            }

            SelectResult(ToResultItem(item, DataSource?.GetDisplayConfig()));
        }

        protected static PickerResultItem ToResultItem(T item, PickerDisplayConfig config)
        {
            var type = typeof(T);

            string code = config?.CodeField != null
                ? type.GetProperty(config.CodeField)?.GetValue(item)?.ToString()
                : null;

            string name = config?.NameField != null
                ? type.GetProperty(config.NameField)?.GetValue(item)?.ToString()
                : item.ToString();

            string extraInfo = string.IsNullOrEmpty(config?.ExtraInfoTemplate)
                ? null
                : FormatTemplate(config.ExtraInfoTemplate, item);

            int? id = type.GetProperty("Id")?.GetValue(item) is int rawId ? rawId : null;

            return new PickerResultItem
            {
                Id = id,
                Code = code,
                Name = name,
                DisplayText = !string.IsNullOrEmpty(code) ? $"{code} - {name}" : name,
                ExtraInfo = extraInfo,
                RawData = item
            };
        }

        private static string FormatTemplate(string template, T item)
        {
            var type = typeof(T);
            return Regex.Replace(template, @"\{(\w+)(?::([^}]+))?\}", m =>
            {
                var value = type.GetProperty(m.Groups[1].Value)?.GetValue(item);
                if (value == null) return "";

                var format = m.Groups[2].Success ? m.Groups[2].Value : null;
                return value is IFormattable formattable && format != null
                    ? formattable.ToString(format, null)
                    : value.ToString();
            });
        }
    }
}
