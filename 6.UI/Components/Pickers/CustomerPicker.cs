using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Entities;

namespace PrimeERP.UI.Components.Pickers
{
    /// <summary>اختيار عميل بجدول بحث — تحذير بصري (تلوين الصف) لو تجاوز حد الائتمان.</summary>
    public class CustomerPicker : PickerBase<Customer>
    {
        /// <summary>معرّفات تُستبعد من القائمة (مثلاً عملاء مختارين بالفعل في سطور أخرى من نفس المستند).</summary>
        public List<int> ExcludeIds { get; set; } = new();

        protected override async void OpenSelectionWindow()
        {
            if (DataSource == null) return;

            var items = (await DataSource.SearchAsync("", 500)).Where(c => !ExcludeIds.Contains(c.Id));
            var config = DataSource.GetDisplayConfig();
            var results = items.Select(i => ToResultItem(i, config)).ToList();

            // TEMPORARY — "تجاوز حد الائتمان" قرار أعمال تحسبه الخدمة في CustomerDto.IsOverCreditLimit، لكن
            // DataSource هنا يُرجع الكيان لا الـDTO فتُعاد المقارنة محلياً. يزول بجعل المصدر يُرجع الـDTO.
            var window = new PickerGridWindow("اختيار عميل", config, results, allowQuickAdd: AllowQuickAdd)
            {
                RowHighlight = raw => raw is Customer c && c.CreditLimit > 0 && c.Balance > c.CreditLimit ? "danger" : null
            };
            window.QuickAddRequested += (s, e) => RaiseQuickAddRequested();

            if (window.ShowDialog() == true && window.SelectedResult?.RawData is Customer selected)
                CommitSelection(selected);
        }
    }
}
