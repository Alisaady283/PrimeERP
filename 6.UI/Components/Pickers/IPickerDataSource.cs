using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrimeERP.UI.Components.Pickers
{
    /// <summary>عمود في نافذة اختيار الـ Picker (جدول كامل).</summary>
    public class PickerColumn
    {
        public string Header { get; set; }
        public string Field  { get; set; }
        public double Width  { get; set; } = 120;
        public string Format { get; set; }
    }

    /// <summary>يصف كيف يُعرض النوع T داخل الـ Picker — بلا أي معرفة بتفاصيل قاعدة البيانات.</summary>
    public class PickerDisplayConfig
    {
        public string CodeField { get; set; }
        public string NameField { get; set; }

        /// <summary>أعمدة نافذة الاختيار الكاملة (جدول).</summary>
        public List<PickerColumn> Columns { get; set; } = new();

        /// <summary>مثل "الرصيد: {Balance}" — يُعرض بجانب النتيجة في القائمة المصغّرة أثناء الكتابة.</summary>
        public string ExtraInfoTemplate { get; set; }
    }

    /// <summary>
    /// مصدر بيانات لأي Picker — PickerBase&lt;T&gt; لا يستدعي DB مباشرة إطلاقاً، بل يستقبل هذا العقد
    /// من المستهلك (عادة تنفيذ رفيع فوق XxxDb الموجودة أصلاً).
    /// </summary>
    public interface IPickerDataSource<T>
    {
        Task<IEnumerable<T>> SearchAsync(string term, int maxResults);
        Task<T> GetByIdAsync(int id);
        Task<T> GetByCodeAsync(string code);
        PickerDisplayConfig GetDisplayConfig();
    }
}
