namespace PrimeERP.Composition.Definitions
{
    public enum FilterKind { Combo, Toggle }

    // فلتر إعلاني فوق شبكة MasterList — يكتب في خاصية على TFilter مباشرة عبر Reflection عند التغيير،
    // ثم يُعيد التحميل؛ بلا أي كود خاص بكل وحدة (راجع CrudPageRenderer).
    public class FilterDefinition
    {
        public required string Key { get; init; }
        public required string LabelKey { get; init; }
        public FilterKind Kind { get; init; } = FilterKind.Combo;

        // Kind.Combo مع PickerType="Category" فقط مدعوم حالياً (يخدم أغلب حالات فلترة MasterList).
        public string PickerType { get; init; }
        public string PickerCategoryModuleKey { get; init; }
        public double Width { get; init; } = 180;
    }
}
