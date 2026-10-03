using System;
using System.Collections.Generic;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>وصف حوار المستند وسطوره</summary>
    public class LineFieldDefinition
    {
        public required string Key { get; init; }
        public required string Header { get; init; }
        public FieldKind Kind { get; init; } = FieldKind.Text;
        public double Width { get; init; } = 140;
        public bool IsRequired { get; init; }
        public string PickerType { get; init; }
        public bool PickerLeafOnly { get; init; }

        public string PickerValueField { get; init; }
    }

    /// <summary>عمود واحد في جدول الورق</summary>
    public class PrintColumnDefinition
    {
        public required string Key { get; init; }
        public required string Header { get; init; }
        public double Width { get; init; } = 1;
        public bool IsText { get; init; }
        public string Format { get; init; } = "N2";
    }

    /// <summary>سطر واحد في صندوق إجماليات</summary>
    public class PrintTotalDefinition
    {
        public required string Key { get; init; }
        public required string Label { get; init; }
        public bool HideWhenZero { get; init; }
        public bool IsBold { get; init; }
    }

    /// <summary>حساب صافي السطر حيّاً أثناء</summary>
    public class LineTotalsDefinition
    {
        public required List<string> Keys { get; init; }
        public string[] MustBalance { get; init; }
    }

    public class LineMathDefinition
    {
        public required string QtyKey { get; init; }
        public required string PriceKey { get; init; }
        public string DiscountPercentKey { get; init; }
        public string VatPercentKey { get; init; }
        public string WithholdingPercentKey { get; init; }
        public required string NetKey { get; init; }
    }

    public class DocumentDialogDefinition
    {
        public required string TitleKey { get; init; }
        public required string TitleEditKey { get; init; }
        public required Type ServiceType { get; init; }

        public Func<IServiceProvider, object> ServiceFactory { get; init; }
        public required Type DtoType { get; init; }
        public required Type LineDtoType { get; init; }
        public required string LinesPropertyName { get; init; }
        public required List<FieldDefinition> HeaderFields { get; init; }
        public required List<LineFieldDefinition> LineFields { get; init; }

        public string PrintTitleKey { get; init; }

        public List<PrintColumnDefinition> PrintColumns { get; init; }

        public List<PrintTotalDefinition> PrintTotals { get; init; }

        public LineMathDefinition LineMath { get; init; }

        public LineTotalsDefinition LineTotals { get; init; }

        public string DocumentKind { get; init; }
        public List<PullSource> PullSources { get; init; } = new();
        public bool AllowPost { get; init; } = true;
        public StockEffect AffectsStock { get; init; } = StockEffect.None;
    }
}
