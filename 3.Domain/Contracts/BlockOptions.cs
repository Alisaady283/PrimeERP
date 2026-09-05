using System.Collections.Generic;

namespace PrimeERP.Domain.Contracts
{
    public enum LogoPosition { None, Left, Right, Center }

    public class CompanyHeaderOptions
    {
        public LogoPosition LogoPosition { get; init; } = LogoPosition.Left;
        public double LogoMaxWidth { get; init; } = 132;
        public double LogoMaxHeight { get; init; } = 68;

        /// <summary>يمنع التفاف اسم الشركة رأسياً حين يضيق عموده.</summary>
        public double CompanyMinWidth { get; init; } = 302;

        public List<string> ShowFields { get; init; } = new();
    }

    public class DocumentTitleOptions
    {
        public string Alignment { get; init; } = "Center";
        public bool ShowNumber { get; init; } = true;
        public bool ShowDate { get; init; } = true;
    }

    public class PartyBlockOptions
    {
        public string Label { get; init; } = "الطرف";
        public string NameField { get; init; } = "PartyName";
        public List<PrintField> Fields { get; init; } = new();
        public bool ShowBorder { get; init; } = true;
    }

    public class PrintField
    {
        public required string Key { get; init; }
        public required string Label { get; init; }
        public string Format { get; init; }
    }

    public class FieldsBlockOptions
    {
        public int Columns { get; init; } = 2;
        public List<PrintField> Fields { get; init; } = new();
        public bool ShowBorders { get; init; } = true;
    }

    public class NarrativeBlockOptions
    {
        public required string Template { get; init; }
        public bool UnderlineValues { get; init; } = true;
    }

    public class LinesTableOptions
    {
        public List<PrintColumn> Columns { get; init; } = new();

        /// <summary>اسم خاصية السطور على كائن البيانات.</summary>
        public required string LinesField { get; init; }

        public bool ShowRowNumbers { get; init; } = true;
        public bool RepeatHeaderOnNewPage { get; init; } = true;

        /// <summary>أعمدة لا تُجمَع (أسعار ونسب) — الباقي الرقمي يُجمَع في صف الإجمالي.</summary>
        public List<string> NonAdditive { get; init; } = new() { "Price", "Cost", "Rate", "Percent" };
    }

    public class TotalRow
    {
        public required string Label { get; init; }
        public required string Field { get; init; }
        public string Format { get; init; } = "N2";
        public bool IsBold { get; init; }
        public bool IsLarge { get; init; }
        public bool HideIfZero { get; init; } = true;
    }

    public class TotalsBlockOptions
    {
        public double Width { get; init; } = 265;
        public List<TotalRow> Rows { get; init; } = new();
    }

    public class AmountInWordsOptions
    {
        public required string SourceField { get; init; }
        public string CurrencyName { get; init; } = "جنيه";
        public string SubUnitName { get; init; } = "قرش";
        public string Style { get; init; } = "Box";
    }

    public class PaymentBlockOptions
    {
        public List<PrintField> Fields { get; init; } = new();
    }

    public class SignatureSlot
    {
        public required string Label { get; init; }
        public bool ShowLine { get; init; } = true;
    }

    public class SignaturesBlockOptions
    {
        public List<SignatureSlot> Slots { get; init; } = new();
    }

    public class NotesBlockOptions
    {
        public string Field { get; init; } = "Notes";
        public string Label { get; init; } = "ملاحظات";
    }

    public class TermsBlockOptions
    {
        public string Text { get; init; }
        public string Label { get; init; } = "الشروط والأحكام";
    }

    public class PageFooterOptions
    {
        public bool ShowPageNumbers { get; init; } = true;
        public bool ShowPrintDate { get; init; } = true;
        public bool ShowPrintedBy { get; init; } = true;
        public string CustomText { get; init; }
    }
}
