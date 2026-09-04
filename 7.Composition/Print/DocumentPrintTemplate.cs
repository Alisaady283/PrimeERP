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
                var totals = new List<PrintTotal>();

                if (Read(_def.LinesPropertyName) is IEnumerable lines)
                {
                    decimal totalQty = 0, totalValue = 0;
                    foreach (var line in lines)
                    {
                        var row = new Dictionary<string, object>();
                        foreach (var lineField in _def.LineFields)
                            row[lineField.Key] = line.GetType().GetProperty(lineField.Key)?.GetValue(line);

                        totalQty   += ToDecimal(row, "Qty");
                        totalValue += ToDecimal(row, "Qty") * (ToDecimal(row, "UnitPrice") + ToDecimal(row, "UnitCost"));
                        rows.Add(row);
                    }

                    if (totalQty > 0)   totals.Add(new PrintTotal { Label = "إجمالي الكمية", Value = totalQty.ToString("N2", CultureInfo.InvariantCulture) });
                    if (totalValue > 0) totals.Add(new PrintTotal { Label = "الإجمالي", Value = totalValue.ToString("N2", CultureInfo.InvariantCulture) });
                }

                if (rows.Count > 0)
                    sections.Add(new PrintSection { Type = PrintSectionType.Table, Columns = columns, Rows = rows, Totals = totals.Count > 0 ? totals : null });

                return sections;
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
