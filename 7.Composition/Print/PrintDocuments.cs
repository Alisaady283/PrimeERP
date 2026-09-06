using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PrimeERP.Composition.Definitions;
using PrimeERP.Domain.Contracts;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Composition.Print
{
    /// <summary>عائلتا المستندات: تجاري (جدول سطور) وسردي (نصّ إقراري). الترويسة والتذييل والتوقيعات
    /// موحّدة بحكم البناء لا بحكم النسخ، وكل مستند سطر استدعاء واحد.</summary>
    public static class PrintDocuments
    {
        private static readonly string[] TradeSignatures = { "المُعِد", "المراجع", "المستلم" };

        /// <summary>خيارات الورق المشتركة — تُقرأ من الإعدادات مرة، وتسري على كل العائلات.</summary>
        public class PaperOptions
        {
            public List<string> CopyLabels { get; init; } = new();
            public int LinesPerPage { get; init; }
            public string Terms { get; init; }
            public string BarcodeText { get; init; }
        }

        public static IPrintable Trade(DocumentDialogDefinition definition, string title, object document, PaperOptions paper = null)
        {
            var doc = new DocumentReader(document);
            var sections = new List<PrintSection>();

            var party = doc.Display("PartyName") ?? doc.Display("CustomerName") ?? doc.Display("SupplierName");
            var number = doc.Number();

            if (!string.IsNullOrWhiteSpace(party))
                sections.Add(new PrintSection
                {
                    Type = PrintSectionType.Parties,
                    Parties = new()
                    {
                        new() { Title = "الطرف", Name = party },
                        new() { Title = "المستند", Name = number, Details = { doc.Display("DocDate") ?? doc.Display("InvoiceDate") ?? "" } }
                    }
                });

            var lines = doc.Lines(definition.LinesPropertyName);
            if (lines.Count > 0)
            {
                var split = SplitFields(definition, lines[0]);
                sections.Add(BuildTable(definition, lines, split));
            }

            if (!string.IsNullOrWhiteSpace(paper?.BarcodeText))
                sections.Add(new PrintSection { Type = PrintSectionType.Barcode, Text = paper.BarcodeText });

            if (!string.IsNullOrWhiteSpace(paper?.Terms))
                sections.Add(new PrintSection { Type = PrintSectionType.Terms, Text = paper.Terms });

            return new ComposedPrintable
            {
                Title = title,
                Subtitle = number,
                Orientation = definition.LineFields.Count > 5 ? PrintOrientation.Landscape : PrintOrientation.Portrait,
                Header = HeaderFields(definition, doc),
                Footer = doc.NotesField(),
                Signatures = TradeSignatures.ToList(),
                CopyLabels = paper?.CopyLabels ?? new(),
                LinesPerPage = paper?.LinesPerPage ?? 0,
                Sections = sections
            };
        }

        /// <summary>تقرير: نفس جدول العائلة التجارية بأعمدة التقرير وصفوفه.</summary>
        public static IPrintable Report(Definitions.ReportResult result, PrintOrientation orientation = PrintOrientation.Portrait)
        {
            var columns = result.Columns
                .Select(c => new PrintColumn
                {
                    Key = c.Binding, Header = c.Header, Width = c.Width / 100.0,
                    Align = c.Align == UI.Components.Display.ColumnAlign.Center ? "Center" : null,
                    Format = c.Format
                })
                .ToList();

            var rows = result.Rows.Cast<object>()
                .Select(item => columns.ToDictionary(c => c.Key, c => item.GetType().GetProperty(c.Key)?.GetValue(item)))
                .ToList();

            var section = new PrintSection { Type = PrintSectionType.Table, Columns = columns, Rows = rows };
            if (rows.Count > 0) section.TotalsRow = TotalsRow(columns, rows);

            return new ComposedPrintable
            {
                Title = result.Title,
                Subtitle = result.SubTitle ?? result.GeneratedAt.ToString("yyyy-MM-dd HH:mm"),
                Orientation = orientation,
                Signatures = new(),
                Sections = new() { section }
            };
        }

        public static IPrintable Narrative(NarrativeDocument document)
        {
            var values = document.Values;
            var sections = new List<PrintSection>
            {
                new() { Type = PrintSectionType.Text, Text = Fill(document.Template, values) },
                new()
                {
                    Type = PrintSectionType.AmountInWords,
                    Title = "مبلغاً وقدره",
                    Amount = document.Amount,
                    Currency = document.Currency,
                    SubUnit = document.SubUnit
                }
            };

            if (document.Details is { Count: > 0 })
                sections.Add(new PrintSection { Type = PrintSectionType.KeyValues, KeyValues = new(document.Details) });

            if (document.Table != null) sections.Add(document.Table);

            return new ComposedPrintable
            {
                Title = document.Title,
                Subtitle = document.Number,
                Orientation = PrintOrientation.Portrait,
                Header = document.Header,
                Signatures = document.Signatures,
                Sections = sections
            };
        }

        /// <summary>{Key} يُستبدل من القيم — محرّك واحد لكل النصوص الإنشائية.</summary>
        internal static string Fill(string template, IReadOnlyDictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(template)) return "";

            return values.Aggregate(template, (text, pair) => text.Replace("{" + pair.Key + "}", pair.Value ?? ""));
        }

        private static Dictionary<string, string> HeaderFields(DocumentDialogDefinition definition, DocumentReader doc)
        {
            var result = new Dictionary<string, string>();
            foreach (var field in definition.HeaderFields)
            {
                if (field.Kind == FieldKind.TextArea) continue;

                var value = doc.Display(field.Key);
                if (!string.IsNullOrWhiteSpace(value)) result[LocalizationService.Get(field.LabelKey)] = value;
            }

            return result;
        }

        /// <summary>الكود عمود والاسم عمود — يُحدَّد مرة واحدة من أول سطر.</summary>
        private static HashSet<string> SplitFields(DocumentDialogDefinition definition, object sampleLine)
        {
            var type = sampleLine.GetType();
            return definition.LineFields
                .Select(f => f.Key)
                .Where(k => k.EndsWith("Code") && type.GetProperty(NameKey(k)) != null)
                .ToHashSet();
        }

        private static string NameKey(string key) => key[..^4] + "Name";

        private static PrintSection BuildTable(DocumentDialogDefinition definition, List<object> lines, HashSet<string> split)
        {
            var columns = new List<PrintColumn>();
            foreach (var field in definition.LineFields)
            {
                if (split.Contains(field.Key))
                    columns.Add(new PrintColumn { Key = field.Key, Header = "الكود", Width = 0.9, Align = "Center" });

                columns.Add(new PrintColumn
                {
                    Key = split.Contains(field.Key) ? NameKey(field.Key) : field.Key,
                    Header = field.Header,
                    Width = field.Width / 100.0,
                    Align = field.Kind == FieldKind.Number ? "Center" : "Right",
                    Format = field.Kind == FieldKind.Number ? "N2" : null
                });
            }

            var rows = new List<Dictionary<string, object>>();
            foreach (var line in lines)
            {
                var type = line.GetType();
                var row = new Dictionary<string, object>();

                foreach (var field in definition.LineFields)
                {
                    row[field.Key] = type.GetProperty(field.Key)?.GetValue(line);
                    if (split.Contains(field.Key)) row[NameKey(field.Key)] = type.GetProperty(NameKey(field.Key))?.GetValue(line);
                }

                rows.Add(row);
            }

            return new PrintSection
            {
                Type = PrintSectionType.Table, Columns = columns, Rows = rows,
                TotalsRow = TotalsRow(columns, rows)
            };
        }

        private static Dictionary<string, object> TotalsRow(List<PrintColumn> columns, List<Dictionary<string, object>> rows)
        {
            var totals = new Dictionary<string, object>();
            var labelled = false;

            foreach (var column in columns)
            {
                var isNumeric = rows.Any(r => r.TryGetValue(column.Key, out var v) && v is decimal or int or double);
                if (!isNumeric)
                {
                    totals[column.Key] = labelled ? "" : "الإجمالي";
                    labelled = true;
                    continue;
                }

                totals[column.Key] = PrintTotals.IsAdditive(column.Key)
                    ? rows.Sum(r => ToDecimal(r, column.Key))
                    : "";
            }

            return totals;
        }

        private static decimal ToDecimal(Dictionary<string, object> row, string key) =>
            row.TryGetValue(key, out var value) && value != null ? Convert.ToDecimal(value) : 0m;

        /// <summary>قراءة كائن البيانات بالاسم — الاسم المرافق يُفضَّل على المعرّف.</summary>
        private class DocumentReader
        {
            private readonly object _doc;

            public DocumentReader(object doc) => _doc = doc;

            public object Raw(string property) => _doc?.GetType().GetProperty(property)?.GetValue(_doc);

            public string Number() =>
                Raw("DocNo") as string ?? Raw("VoucherNo") as string ?? Raw("InvoiceNo") as string;

            public List<object> Lines(string property) =>
                (Raw(property) as IEnumerable)?.Cast<object>().ToList() ?? new List<object>();

            public Dictionary<string, string> NotesField()
            {
                var notes = Raw("Notes") as string;
                return string.IsNullOrWhiteSpace(notes) ? null : new Dictionary<string, string> { ["ملاحظات"] = notes };
            }

            public string Display(string key)
            {
                var friendly = Raw(key.EndsWith("Id") ? key[..^2] + "Name" : key + "Name");

                return (friendly ?? Raw(key)) switch
                {
                    null => null,
                    DateTime date => date.ToString("yyyy-MM-dd"),
                    decimal number => number.ToString("N2", CultureInfo.InvariantCulture),
                    var value => value.ToString()
                };
            }
        }

        private class ComposedPrintable : IPrintable
        {
            public string Title { get; init; }
            public string Subtitle { get; init; }
            public Dictionary<string, string> Header { get; init; }
            public Dictionary<string, string> Footer { get; init; }
            public List<string> Signatures { get; init; }
            public List<PrintSection> Sections { get; init; }

            public string DocumentTitle => Title;
            public string DocumentSubtitle => Subtitle;
            public PrintOrientation Orientation { get; init; }

            public Dictionary<string, string> HeaderFields => Header;
            public Dictionary<string, string> FooterFields => Footer;

            public bool ShowCompanyHeader => true;
            public bool ShowPageNumbers => true;
            public bool ShowSignatures => Signatures is { Count: > 0 };
            public List<string> SignatureLabels => Signatures;

            public List<string> CopyLabels { get; init; } = new();
            public int LinesPerPage { get; init; }

            public List<PrintSection> BuildSections() => Sections;
        }
    }

    /// <summary>وصف مستند سردي — سند قبض/صرف، أمر دفع، إشعار.</summary>
    public class NarrativeDocument
    {
        public required string Title { get; init; }
        public string Number { get; init; }
        public required string Template { get; init; }
        public required Dictionary<string, string> Values { get; init; }

        public decimal Amount { get; init; }
        public string Currency { get; init; } = "جنيه";
        public string SubUnit { get; init; } = "قرش";

        public Dictionary<string, string> Header { get; init; }
        public Dictionary<string, string> Details { get; init; }
        public PrintSection Table { get; init; }
        public List<string> Signatures { get; init; } = new();
    }
}
