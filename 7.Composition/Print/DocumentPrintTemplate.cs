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
    /// <summary>يبني IPrintable لأي مستند من تعريفه نفسه (DocumentDialogDefinition) وكائن بياناته — بلا قالب
    /// مكتوب يدوياً لكل مستند. مستند جديد يُطبَع فور تسجيله، وتعديل شكل الورق يقع هنا مرة واحدة للجميع.</summary>
    public static class DocumentPrintTemplate
    {
        public static IPrintable From(DocumentDialogDefinition definition, string title, object document) =>
            new DefinitionPrintable(definition, title, document);

        private class DefinitionPrintable : IPrintable
        {
            private readonly DocumentDialogDefinition _def;
            private readonly object _doc;
            private readonly string _title;

            public DefinitionPrintable(DocumentDialogDefinition def, string title, object doc)
            {
                _def = def; _title = title; _doc = doc;
            }

            public string DocumentTitle    => _title;
            public string DocumentSubtitle => Read("DocNo") as string ?? Read("VoucherNo") as string ?? Read("InvoiceNo") as string;
            public PrintOrientation Orientation => _def.LineFields.Count > 5 ? PrintOrientation.Landscape : PrintOrientation.Portrait;

            public bool ShowCompanyHeader => true;
            public bool ShowPageNumbers   => true;
            public bool ShowSignatures    => true;
            public List<string> SignatureLabels => new() { "المُعِد", "المراجع", "المستلم" };

            // حقول الرأس تأتي من نفس التعريف الذي بنى الشاشة — فلا يظهر على الورق حقل غاب عن الشاشة أو العكس.
            public Dictionary<string, string> HeaderFields
            {
                get
                {
                    var result = new Dictionary<string, string>();
                    foreach (var headerField in _def.HeaderFields)
                    {
                        if (headerField.Kind == FieldKind.TextArea) continue;

                        var value = ReadDisplay(headerField.Key);
                        if (string.IsNullOrWhiteSpace(value)) continue;

                        result[LocalizationService.Get(headerField.LabelKey)] = value;
                    }
                    return result;
                }
            }

            public Dictionary<string, string> FooterFields
            {
                get
                {
                    var result = new Dictionary<string, string>();
                    var notes = Read("Notes") as string;
                    if (!string.IsNullOrWhiteSpace(notes)) result["ملاحظات"] = notes;
                    return result;
                }
            }

            public List<PrintSection> BuildSections()
            {
                var sections = new List<PrintSection>();

                var party = ReadDisplay("PartyName") ?? ReadDisplay("CustomerName") ?? ReadDisplay("SupplierName");
                if (!string.IsNullOrWhiteSpace(party))
                    sections.Add(new PrintSection
                    {
                        Type = PrintSectionType.Parties,
                        Parties = new List<PrintParty>
                        {
                            new() { Title = "الطرف", Name = party },
                            new() { Title = "المستند", Name = DocumentSubtitle, Details = { ReadDisplay("DocDate") ?? ReadDisplay("VoucherDate") ?? "" } }
                        }
                    });

                var columns = _def.LineFields
                    .Select(lineField => new PrintColumn
                    {
                        Key = lineField.Key, Header = lineField.Header, Width = lineField.Width / 100.0,
                        Align = lineField.Kind == FieldKind.Number ? "Center" : "Right",
                        Format = lineField.Kind == FieldKind.Number ? "N2" : null
                    })
                    .ToList();

                var rows = new List<Dictionary<string, object>>();

                if (Read(_def.LinesPropertyName) is IEnumerable lines)
                {
                    foreach (var line in lines)
                    {
                        var row = new Dictionary<string, object>();
                        foreach (var lineField in _def.LineFields)
                            row[lineField.Key] = LineValue(line, lineField.Key);

                        rows.Add(row);
                    }
                }

                if (rows.Count == 0) return sections;

                sections.Add(new PrintSection
                {
                    Type = PrintSectionType.Table, Columns = columns, Rows = rows,
                    TotalsRow = BuildTotalsRow(columns, rows)
                });

                return sections;
            }

            /// <summary>الكود سطر والاسم سطر تحته.</summary>
            private static object LineValue(object line, string key)
            {
                var type = line.GetType();
                var value = type.GetProperty(key)?.GetValue(line);

                if (!key.EndsWith("Code")) return value;

                var name = type.GetProperty(key[..^4] + "Name")?.GetValue(line) as string;
                var code = value as string;

                if (string.IsNullOrWhiteSpace(name)) return code;
                if (string.IsNullOrWhiteSpace(code)) return name;

                return code + (char)10 + name;
            }

            // الأسعار والنسب لا تُجمَع — جمع سعر الوحدة رقم بلا معنى. تُجمَع الكميات والقيم فقط.
            private static readonly string[] NonAdditive = { "Price", "Cost", "Rate", "Percent", "Discount%" };

            private static Dictionary<string, object> BuildTotalsRow(List<PrintColumn> columns, List<Dictionary<string, object>> rows)
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

                    totals[column.Key] = NonAdditive.Any(column.Key.Contains)
                        ? ""
                        : rows.Sum(r => ToDecimal(r, column.Key));
                }

                return totals;
            }

            private static decimal ToDecimal(Dictionary<string, object> row, string key) =>
                row.TryGetValue(key, out var value) && value != null ? Convert.ToDecimal(value) : 0m;

            private object Read(string property) => _doc?.GetType().GetProperty(property)?.GetValue(_doc);

            // Id لحقل Picker لا يعني شيئاً على الورق — يُستبدَل باسم الطرف/المخزن المرافق متى وُجد.
            private string ReadDisplay(string key)
            {
                var friendly = Read(key.EndsWith("Id") ? key[..^2] + "Name" : key + "Name");
                var value = friendly ?? Read(key);

                return value switch
                {
                    null => null,
                    DateTime date => date.ToString("yyyy-MM-dd"),
                    decimal number => number.ToString("N2", CultureInfo.InvariantCulture),
                    _ => value.ToString()
                };
            }
        }
    }
}
