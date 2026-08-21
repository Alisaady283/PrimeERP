using System;
using System.Collections.Generic;
using PrimeERP.Domain.Results;

namespace PrimeERP.Domain.Contracts
{
    public enum PrintOrientation { Portrait, Landscape }

    public enum PrintSectionType { Title, KeyValues, Table, Text, Spacer, Callout }

    public class PrintColumn
    {
        public string Key    { get; set; }
        public string Header { get; set; }
        public double Width  { get; set; } = 1;
        public string Align  { get; set; } = "Right";

        /// <summary>تنسيق .NET قياسي (مثال: "N2", "yyyy-MM-dd") يُطبَّق عبر IFormattable — فارغ يعني نص كما هو.</summary>
        public string Format { get; set; }
    }

    public class PrintTotal
    {
        public string Label   { get; set; }
        public string Value   { get; set; }
        public bool   IsBold  { get; set; } = true;
    }

    /// <summary>قسم واحد من مستند الطباعة — عنوان/بيانات مفتاح-قيمة/جدول/نص حر/فاصل.</summary>
    public class PrintSection
    {
        public PrintSectionType Type { get; set; }
        public string Title { get; set; }

        public Dictionary<string, string> KeyValues { get; set; }

        public List<PrintColumn> Columns { get; set; }
        public List<Dictionary<string, object>> Rows { get; set; }
        public List<PrintTotal> Totals { get; set; }

        /// <summary>لتمييز صفوف معيّنة (مثال: حسابات تجميعية غير Leaf في ميزان المراجعة) — نفس نمط AppDataGrid.RowHighlightSelector.</summary>
        public Func<Dictionary<string, object>, bool> RowBold { get; set; }

        public string Text { get; set; }

        /// <summary>لـ PrintSectionType.Callout فقط — يلوّن الصندوق عبر PrintTheme (Soft/SoftText/Solid لنفس المتغيّر).</summary>
        public StatusVariant? Variant { get; set; }
    }

    /// <summary>
    /// أي مستند قابل للطباعة ينفّذ هذا العقد فقط — PrintService لا يعرف شيئاً عن Account/JournalEntry/فاتورة،
    /// يبني المستند من BuildSections() حصراً (نفس نمط IPrintable الأصلي في الوصف الأول لهذه الخدمة).
    /// </summary>
    public interface IPrintable
    {
        string DocumentTitle { get; }
        string DocumentSubtitle { get; }
        PrintOrientation Orientation { get; }

        Dictionary<string, string> HeaderFields { get; }
        List<PrintSection> BuildSections();
        Dictionary<string, string> FooterFields { get; }

        bool ShowCompanyHeader { get; }
        bool ShowPageNumbers { get; }
        bool ShowSignatures { get; }
        List<string> SignatureLabels { get; }
    }
}
