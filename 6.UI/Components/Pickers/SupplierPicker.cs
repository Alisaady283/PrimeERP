using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Localization;

namespace PrimeERP.UI.Components.Pickers
{
    /// <summary>اختيار مورد بجدول بحث</summary>
    public class SupplierPicker : PickerBase<Supplier>
    {
        public List<int> ExcludeIds { get; set; } = new();

        protected override async void OpenSelectionWindow()
        {
            if (DataSource == null) return;

            var items = (await DataSource.SearchAsync("", 500)).Where(s => !ExcludeIds.Contains(s.Id));
            var config = DataSource.GetDisplayConfig();
            var results = items.Select(i => ToResultItem(i, config)).ToList();

            var window = new PickerGridWindow(LocalizationService.Get("Str.Picker.Supplier"), config, results, allowQuickAdd: AllowQuickAdd);
            window.QuickAddRequested += (s, e) => RaiseQuickAddRequested();

            if (window.ShowDialog() == true && window.SelectedResult?.RawData is Supplier selected)
                CommitSelection(selected);
        }
    }
}
