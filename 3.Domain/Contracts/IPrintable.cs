using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Results;

namespace PrimeERP.Domain.Contracts
{
    /// <summary>عقد الطباعة ومفرداته</summary>
    public enum PrintOrientation { Portrait, Landscape }

    public enum PrintSectionType { Title, KeyValues, Table, Text, Spacer, Callout, Parties, AmountInWords, Terms, Barcode }

    public static class PrintTotals
    {
        private static readonly string[] NonAdditive =
            { "Price", "Cost", "Rate", "Percent", "Discount", "Code", "Id", "No", "Number" };

        public static bool IsAdditive(string columnKey) => !NonAdditive.Any(columnKey.Contains);
    }

    public class PrintColumn
    {
        public string Key    { get; set; }
        public string Header { get; set; }
        public double Width  { get; set; } = 1;
        public string Align  { get; set; }

        public string Format { get; set; }
    }

    public class PrintTotal
    {
        public string Label   { get; set; }
        public string Value   { get; set; }
        public bool   IsBold  { get; set; } = true;
    }

    /// <summary>قسم واحد من مستند الطباعة</summary>
    public class PrintSection
    {
        public PrintSectionType Type { get; set; }
        public string Title { get; set; }

        public Dictionary<string, string> KeyValues { get; set; }

        public List<PrintColumn> Columns { get; set; }
        public List<Dictionary<string, object>> Rows { get; set; }
        public List<PrintTotal> Totals { get; set; }

        public Dictionary<string, object> TotalsRow { get; set; }

        public Func<Dictionary<string, object>, bool> RowBold { get; set; }

        public string Text { get; set; }

        public List<string> FillParts { get; set; }

        public List<double> FillShares { get; set; }

        public List<PrintParty> Parties { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string SubUnit { get; set; }

        public StatusVariant? Variant { get; set; }
    }

    /// <summary>صندوق طرف واحد في قسم</summary>
    public class PrintParty
    {
        public string Title { get; set; }
        public string Name  { get; set; }
        public List<string> Details { get; set; } = new();
    }

    /// <summary>أي مستند قابل للطباعة ينفّذ</summary>
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

        List<string> CopyLabels => new();

        int LinesPerPage => 0;

        bool Framed => false;

        bool HalfPage => false;
    }
}
