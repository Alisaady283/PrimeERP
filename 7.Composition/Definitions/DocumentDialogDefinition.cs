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

        /// <summary>ما تُرجعه القائمة للخانة. فارغ = يُستنتَج من نوع خانة الـDTO (نصّية Code، رقمية Id) —
        /// يُعلَن صراحةً حين تحمل خانةٌ نصّية اسماً لا كوداً، كاسم بنك الشيك.</summary>
        public string PickerValueField { get; init; }
    }

    // مستند رأس+سطور (قيد يومية، وفواتير لاحقاً) — DtoType واحد لكل من الإنشاء والتعديل (يطابق شكل
    // CreateJournalDto: JournalService.Update يأخذ نفس نوع Create لا نوعاً مستقلاً). LinesPropertyName خاصية
    // من نوع List&lt;LineDtoType&gt; على DtoType.
    /// <summary>عمود واحد في جدول الورق — يقرأ خاصية من سطر المستند المخزَّن.</summary>
    public class PrintColumnDefinition
    {
        public required string Key { get; init; }
        public required string Header { get; init; }
        public double Width { get; init; } = 1;
        public bool IsText { get; init; }
        public string Format { get; init; } = "N2";
    }

    /// <summary>سطر واحد في صندوق إجماليات المستند.</summary>
    public class PrintTotalDefinition
    {
        public required string Key { get; init; }
        public required string Label { get; init; }
        /// <summary>صفر لا يُطبع — الخصم والحجز يظهران فقط حين يوجدان فعلاً.</summary>
        public bool HideWhenZero { get; init; }
        public bool IsBold { get; init; }
    }

    /// <summary>حساب صافي السطر حيّاً أثناء الإدخال — المفاتيح فقط، والمعادلة من DocumentTotals نفسها
    /// التي يرحّل بها الحفظ، فلا تُكتب مرتين ولا يختلف ما يراه المستخدم عمّا يُخزَّن.</summary>
    /// <summary>
    /// إجماليات حيّة أسفل السطور: مجموع كل مفتاح بعنوان عموده. MustBalance مفتاحان يجب أن يتساوى
    /// مجموعاهما — يظهر الفرق بلون التحذير ما لم يكن صفراً، فيرى المُدخِل اختلال القيد قبل الحفظ لا بعده.
    /// </summary>
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
        public required Type DtoType { get; init; }
        public required Type LineDtoType { get; init; }
        public required string LinesPropertyName { get; init; }
        public required List<FieldDefinition> HeaderFields { get; init; }
        public required List<LineFieldDefinition> LineFields { get; init; }

        /// <summary>اسم المستند على الورق، لا عنوان نموذج الإدخال.</summary>
        public string PrintTitle { get; init; }

        /// <summary>أعمدة الورق حين تختلف عن حقول الإدخال — القيم المحسوبة (الخصم، الضريبتان، الصافي)
        /// تُخزَّن ولا تُدخَل، فلا تظهر أبداً لو اشتُقّت الأعمدة من LineFields. فارغ = اشتقاقها منها.</summary>
        public List<PrintColumnDefinition> PrintColumns { get; init; }

        /// <summary>إجماليات أسفل المستند: تسمية ← خاصية على الرأس. فارغ = بلا صندوق إجماليات.</summary>
        public List<PrintTotalDefinition> PrintTotals { get; init; }

        /// <summary>فارغ = بلا حساب حيّ.</summary>
        public LineMathDefinition LineMath { get; init; }

        /// <summary>فارغ = بلا شريط إجماليات أسفل السطور.</summary>
        public LineTotalsDefinition LineTotals { get; init; }

        public string DocumentKind { get; init; }
        public List<PullSource> PullSources { get; init; } = new();
        public bool AllowPost { get; init; } = true;
        public StockEffect AffectsStock { get; init; } = StockEffect.None;
    }
}
