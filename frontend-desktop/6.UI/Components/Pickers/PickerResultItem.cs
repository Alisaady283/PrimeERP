namespace PrimeERP.UI.Components.Pickers
{
    /// <summary>تمثيل موحّد وغير معمَّم لأي</summary>
    public class PickerResultItem
    {
        public int?   Id          { get; set; }
        public string Code        { get; set; }
        public string Name        { get; set; }
        public string DisplayText { get; set; }
        public string ExtraInfo   { get; set; }

        public object RawData     { get; set; }
    }
}
