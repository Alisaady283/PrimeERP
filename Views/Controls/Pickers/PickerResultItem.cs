namespace PrimeERP.Views.Controls.Pickers
{
    /// <summary>تمثيل موحّد وغير معمَّم لأي نتيجة بحث Picker — PickerBaseControl يعمل بهذا النوع فقط، والطبقة المعمَّمة (PickerBase&lt;T&gt;) تحوّل T إليه.</summary>
    public class PickerResultItem
    {
        public int?   Id          { get; set; }
        public string Code        { get; set; }
        public string Name        { get; set; }
        public string DisplayText { get; set; }
        public string ExtraInfo   { get; set; }

        /// <summary>الكائن الأصلي (Account/Customer/Product/...).</summary>
        public object RawData     { get; set; }
    }
}
