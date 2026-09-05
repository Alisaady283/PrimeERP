using System.Collections.Generic;

namespace PrimeERP.Domain.Contracts
{
    public enum BlockKind
    {
        CompanyHeader, DocumentTitle, PartyBlock, FieldsBlock, NarrativeBlock, LinesTable,
        TotalsBlock, AmountInWords, PaymentBlock, ChequeBlock, NotesBlock, TermsBlock,
        SignaturesBlock, StampBlock, PageFooter, Spacer, Divider
    }

    /// <summary>كتلة واحدة في المستند — ترتيبها في القائمة هو التخطيط.</summary>
    public class BlockDefinition
    {
        public required BlockKind Kind { get; init; }

        /// <summary>اسم حقل bool في البيانات يحكم الظهور — فارغ يعني ظهوراً دائماً.</summary>
        public string VisibleWhen { get; init; }

        public object Options { get; init; }

        public double MarginTop { get; init; }
        public double MarginBottom { get; init; } = 8;
        public bool PageBreakBefore { get; init; }
        public bool RepeatOnEveryPage { get; init; }
    }

    /// <summary>مستند بالتكوين: قائمة كتل وقيمها. لا تخطيط مكتوب ولا مُصيِّر خاص لكل نوع.</summary>
    public class DocumentTemplate
    {
        public required string Key { get; init; }
        public required string TitleAr { get; init; }
        public string TitleEn { get; init; }

        public PrintOrientation Orientation { get; init; } = PrintOrientation.Portrait;
        public required List<BlockDefinition> Blocks { get; init; }

        public int CopiesCount { get; init; } = 1;
        public List<string> CopyLabels { get; init; } = new();
        public string PermissionKey { get; init; }
    }
}
