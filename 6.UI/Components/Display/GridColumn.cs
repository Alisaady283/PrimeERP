using System.Windows;

namespace PrimeERP.UI.Components.Display
{
    /// <summary>محاذاة منطقية لا فيزيائية — Auto/Start يبدأ من جهة اتجاه اللغة (يمين بالعربية، يسار بالإنجليزية)،
    /// End يتبع الاتجاه المعاكس، Center ثابت. القاعدة نفسها في LineAlign (Documents/LineColumn).</summary>
    public enum ColumnAlign { Auto, Start, Center, End }

    public enum FooterAggregate { None, Sum, Count, Average }

    public enum GridSelectionMode { Single, Multiple }

    /// <summary>تعريف عمود AppDataGrid — يُبنى من كود C# لا من XAML، فيسمح بمنطق الصلاحيات والإجماليات موحّداً لكل الشبكات.</summary>
    /// <remarks>سجلٌّ لا صنف: وصفُ البناء يعلو عليه بـ<c>with</c>، فيأخذ العمود عنوانه وعرضه من صفّه
    /// ويحتفظ بما لا يصفه الوصف — قالب الخلية والمحاذاة والعرض النجمي.</remarks>
    public record GridColumn
    {
        public string Header { get; set; }

        /// <summary>عنوان مجموعة يعلو عدة أعمدة متجاورة (مثل: الأرصدة الافتتاحية فوق مدين ودائن).
        /// فارغ = العمود بلا مجموعة، فيمتدّ عنوانه على صفَّي الرأس.</summary>
        public string Group { get; set; }
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
