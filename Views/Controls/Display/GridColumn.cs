using System.Windows;

namespace PrimeERP.Views.Controls.Display
{
    /// <summary>محاذاة منطقية لا فيزيائية — Auto/Start يبدأ من جهة اتجاه اللغة (يمين بالعربية، يسار بالإنجليزية)،
    /// End يتبع الاتجاه المعاكس، Center ثابت. القاعدة نفسها في LineAlign (Documents/LineColumn).</summary>
    public enum ColumnAlign { Auto, Start, Center, End }

    public enum FooterAggregate { None, Sum, Count, Average }

    public enum GridSelectionMode { Single, Multiple }

    /// <summary>تعريف عمود AppDataGrid — يُبنى من كود C# لا من XAML، فيسمح بمنطق الصلاحيات والإجماليات موحّداً لكل الشبكات.</summary>
    public class GridColumn
    {
        public string Header { get; set; }
        public string Binding { get; set; }
        public double Width { get; set; } = 120;
        public bool IsStarWidth { get; set; }
        public ColumnAlign Align { get; set; } = ColumnAlign.Auto;
        public string Format { get; set; }
        public string PermissionKey { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsSortable { get; set; } = true;
        public FooterAggregate Footer { get; set; } = FooterAggregate.None;
        public DataTemplate CellTemplate { get; set; }
    }
}
