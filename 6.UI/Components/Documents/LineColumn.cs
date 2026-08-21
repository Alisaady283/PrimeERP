using System.Collections.Generic;
using System.Windows;

namespace PrimeERP.UI.Components.Documents
{
    public enum LineColumnType
    {
        RowNumber, Text, Integer, Decimal, Money, Percent, Picker, Combo, Checkbox, Computed
    }

    public enum LinePickerType
    {
        None, Account, Customer, Supplier, Product, Employee
    }

    public enum LineColumnFooter
    {
        None, Sum, Count
    }

    /// <summary>
    /// محاذاة منطقية لا فيزيائية — Auto يتبع نوع العمود (أرقام/تواريخ/أكواد/حالات = وسط، نصوص = بداية اتجاه اللغة)،
    /// Start/End يتبعان اتجاه اللغة أيضاً (لا يمين/يسار ثابتين). القاعدة نفسها مطبّقة في ColumnAlign (Display/GridColumn).
    /// </summary>
    public enum LineAlign
    {
        Auto, Start, Center, End
    }

    /// <summary>تعريف عمود في DocumentLinesGrid — يُبنى من كود C# (LineColumnPresets)، لا XAML.</summary>
    public class LineColumn
    {
        public string          Key         { get; set; }
        public string          Header      { get; set; }
        public double          Width       { get; set; } = 100;
        public bool             IsStarWidth { get; set; }
        public LineColumnType   Type        { get; set; } = LineColumnType.Text;
        public bool             IsReadOnly  { get; set; }
        public bool             IsRequired  { get; set; }
        public string           Format      { get; set; }
        public LineAlign        Align       { get; set; } = LineAlign.Auto;
        public LinePickerType   PickerType  { get; set; } = LinePickerType.None;

        /// <summary>مفتاح صيغة مسجّلة في LineComputeEngine — يُعاد حسابها تلقائياً عند أي تغيير في السطر (عمود Type=Computed فقط).</summary>
        public string ComputeExpression { get; set; }

        public LineColumnFooter Footer       { get; set; } = LineColumnFooter.None;
        public string           PermissionKey{ get; set; }
        public int?              MaxLength    { get; set; }

        /// <summary>عند اختيار عنصر من الـ picker: مفتاح عمود آخر في نفس السطر ← اسم الخاصية على الكائن المختار التي تُنسخ منها القيمة.</summary>
        public Dictionary<string, string> FillsFrom { get; set; } = new();

        /// <summary>
        /// المحاذاة الفعلية لهذا العمود — Auto يُحسم حسب Type (نص = بداية اتجاه اللغة، غير ذلك = وسط).
        /// النتيجة Left/Right فيزيائية لكنها "منطقية" فعلياً لأن WPF يعكسها تلقائياً حسب FlowDirection المحيط.
        /// </summary>
        public HorizontalAlignment ResolveHorizontalAlignment()
        {
            var effective = Align == LineAlign.Auto ? AutoAlignFor(Type) : Align;
            return effective switch
            {
                LineAlign.Center => HorizontalAlignment.Center,
                LineAlign.End    => HorizontalAlignment.Right,
                _                => HorizontalAlignment.Left // Start — يُعكس بصرياً تلقائياً في RTL
            };
        }

        private static LineAlign AutoAlignFor(LineColumnType type) => type switch
        {
            LineColumnType.Text or LineColumnType.Combo => LineAlign.Start,
            _ => LineAlign.Center // RowNumber, Integer, Decimal, Money, Percent, Picker (كود), Checkbox, Computed
        };
    }
}
