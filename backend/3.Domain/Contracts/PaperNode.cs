using System.Collections.Generic;

namespace PrimeERP.Domain.Contracts
{
    /// <summary>شجرة الورق: نصٌّ وصورةٌ وجدول</summary>
    public enum PaperTextRole { Name, Detail }

    /// <summary>قطعة ورق كبنية مجرّدة</summary>
    public abstract class PaperNode { }

    public sealed class PaperText : PaperNode
    {
        public string Text { get; init; }
        public PaperTextRole Role { get; init; }
    }

    public sealed class PaperImage : PaperNode
    {
        public string Data { get; init; }
        public double MaxWidth { get; init; }
        public double MaxHeight { get; init; }
    }

    /// <summary>ترتيب عمودي</summary>
    public sealed class PaperStack : PaperNode
    {
        public List<PaperNode> Children { get; init; } = new();
    }

    /// <summary>ترتيب أفقي</summary>
    public sealed class PaperRow : PaperNode
    {
        public List<PaperNode> Children { get; init; } = new();
    }

    /// <summary>خطّ فاصل بلون الهوية</summary>
    public sealed class PaperRule : PaperNode
    {
        public double Thickness { get; init; }
        public double GapAbove { get; init; }
        public double GapBelow { get; init; }
    }
}
