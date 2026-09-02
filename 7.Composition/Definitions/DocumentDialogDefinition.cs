using System;
using System.Collections.Generic;

namespace PrimeERP.Composition.Definitions
{
    // عمود واحد في شبكة السطور المتكرّرة (قيد يومية اليوم، فاتورة لاحقاً) — نفس FieldKind المستخدَم في
    // الحقول المسطّحة، بلا LabelKey (العنوان يظهر مرة واحدة أعلى العمود، لا لكل صف).
    public class LineFieldDefinition
    {
        public required string Key { get; init; }
        public required string Header { get; init; }
        public FieldKind Kind { get; init; } = FieldKind.Text;
        public double Width { get; init; } = 140;
        public bool IsRequired { get; init; }
        public string PickerType { get; init; }
        public bool PickerLeafOnly { get; init; }
    }

    // مستند رأس+سطور (قيد يومية، وفواتير لاحقاً) — DtoType واحد لكل من الإنشاء والتعديل (يطابق شكل
    // CreateJournalDto: JournalService.Update يأخذ نفس نوع Create لا نوعاً مستقلاً). LinesPropertyName خاصية
    // من نوع List&lt;LineDtoType&gt; على DtoType.
    public class DocumentDialogDefinition
    {
        public required string TitleKey { get; init; }
        public required string TitleEditKey { get; init; }
        public required Type ServiceType { get; init; }
        public required Type DtoType { get; init; }
        public required Type LineDtoType { get; init; }
        public required string LinesPropertyName { get; init; }
        public required List<FieldDefinition> HeaderFields { get; init; }
        public required List<LineFieldDefinition> LineFields { get; init; }

        public string DocumentKind { get; init; }
        public List<PullSource> PullSources { get; init; } = new();
        public bool AllowPost { get; init; } = true;
        public StockEffect AffectsStock { get; init; } = StockEffect.None;
    }
}
