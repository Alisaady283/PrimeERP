using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrimeERP.UI.Components.Pickers
{
    /// <summary>عمود في نافذة اختيار الـ</summary>
    public class PickerColumn
    {
        public string Header { get; set; }
        public string Field  { get; set; }
        public double Width  { get; set; } = 120;
        public string Format { get; set; }
    }

    /// <summary>يصف كيف يُعرض النوع T</summary>
    public class PickerDisplayConfig
    {
        public string CodeField { get; set; }
        public string NameField { get; set; }

        public List<PickerColumn> Columns { get; set; } = new();

        public string ExtraInfoTemplate { get; set; }
    }

    /// <summary>مصدر بيانات لأي Picker</summary>
    public interface IPickerDataSource<T>
    {
        Task<IEnumerable<T>> SearchAsync(string term, int maxResults);
        Task<T> GetByIdAsync(int id);
        Task<T> GetByCodeAsync(string code);
        PickerDisplayConfig GetDisplayConfig();
    }
}
