using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Localization;

namespace PrimeERP.UI.Components.Pickers
{
    /// <summary>اختيار موظف بجدول بحث</summary>
    public class EmployeePicker : PickerBase<Employee>
    {
        public bool ActiveOnly { get; set; } = true;
        public List<int> ExcludeIds { get; set; } = new();

        protected override async void OpenSelectionWindow()
        {
            if (DataSource == null) return;

            var items = (await DataSource.SearchAsync("", 500)).Where(e => !ExcludeIds.Contains(e.Id));
            if (ActiveOnly)
                items = items.Where(e => e.Status == EmployeeStatus.Active);

            var config = DataSource.GetDisplayConfig();
            var results = items.Select(i => ToResultItem(i, config)).ToList();

            var window = new PickerGridWindow(LocalizationService.Get("Str.Picker.Employee"), config, results, allowQuickAdd: AllowQuickAdd);

            if (window.ShowDialog() == true && window.SelectedResult?.RawData is Employee selected)
                CommitSelection(selected);
        }
    }
}
