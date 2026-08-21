using System.Collections.Generic;
using System.Linq;
using PrimeERP.Models;

namespace PrimeERP.Views.Controls.Pickers
{
    /// <summary>
    /// اختيار صنف بجدول بحث — تمييز أحمر للأصناف بدون رصيد. WarehouseId هنا مجرّد سياق تعريفي؛
    /// الفلترة الفعلية لرصيد مخزن محدد مسؤولية IPickerDataSource&lt;Product&gt; المُمرَّر (يملأ Product.CurrentStock).
    /// </summary>
    public class ProductPicker : PickerBase<Product>
    {
        public int? WarehouseId { get; set; }
        public List<int> ExcludeIds { get; set; } = new();

        protected override async void OpenSelectionWindow()
        {
            if (DataSource == null) return;

            var items = (await DataSource.SearchAsync("", 500)).Where(p => !ExcludeIds.Contains(p.Id));
            var config = DataSource.GetDisplayConfig();
            var results = items.Select(i => ToResultItem(i, config)).ToList();

            var window = new PickerGridWindow("اختيار صنف", config, results, allowQuickAdd: AllowQuickAdd)
            {
                RowHighlight = raw => raw is Product { CurrentStock: <= 0 } ? "danger" : null
            };
            window.QuickAddRequested += (s, e) => RaiseQuickAddRequested();

            if (window.ShowDialog() == true && window.SelectedResult?.RawData is Product selected)
                CommitSelection(selected);
        }
    }
}
