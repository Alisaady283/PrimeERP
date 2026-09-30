using PrimeERP.Application.Validation;
using PrimeERP.Domain.Calculations;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Entities;

namespace PrimeERP.UI.Components.Pickers
{
    /// <summary>اختيار عميل بجدول بحث</summary>
    public class CustomerPicker : PickerBase<Customer>
    {
        public List<int> ExcludeIds { get; set; } = new();

        protected override async void OpenSelectionWindow()
        {
            if (DataSource == null) return;

            var items = (await DataSource.SearchAsync("", 500)).Where(c => !ExcludeIds.Contains(c.Id));
            var config = DataSource.GetDisplayConfig();
            var results = items.Select(i => ToResultItem(i, config)).ToList();

            var window = new PickerGridWindow("اختيار عميل", config, results, allowQuickAdd: AllowQuickAdd)
            {
                RowHighlight = raw => raw is Customer c && PartyCalc.IsOverCreditLimit(c.Balance, c.CreditLimit) ? "danger" : null
            };
            window.QuickAddRequested += (s, e) => RaiseQuickAddRequested();

            if (window.ShowDialog() == true && window.SelectedResult?.RawData is Customer selected)
                CommitSelection(selected);
        }
    }
}
