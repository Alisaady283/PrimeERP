namespace PrimeERP.Composition.Definitions
{
    /// <summary>وصف فلتر الصفحة</summary>
    public enum FilterKind { Combo, Toggle }

    public class FilterDefinition
    {
        public required string Key { get; init; }
        public required string LabelKey { get; init; }
        public FilterKind Kind { get; init; } = FilterKind.Combo;

        public string PickerType { get; init; }
        public string PickerCategoryModuleKey { get; init; }

        public string PickerFilterField { get; init; }
        public double Width { get; init; } = 180;
    }
}
