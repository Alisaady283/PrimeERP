using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Core.Common;
using PrimeERP.Services.Accounting.DTOs;

namespace PrimeERP.Services.Print.Templates
{
    /// <summary>يبني IPrintable لميزان مراجعة من List&lt;TrialBalanceLine&gt; (مصدرها IJournalService.GetTrialBalance) — لا يستدعي IJournalService هنا، نفس نمط AccountStatementPrintTemplate.</summary>
    public static class TrialBalancePrintTemplate
    {
        public static IPrintable Build(List<TrialBalanceLine> lines, DateTime from, DateTime to, bool includesDrafts) =>
            new TrialBalancePrintable(lines, from, to, includesDrafts);

        private class TrialBalancePrintable : IPrintable
        {
            private readonly List<TrialBalanceLine> _lines;
            private readonly DateTime _from;
            private readonly DateTime _to;
            private readonly bool _includesDrafts;

            public TrialBalancePrintable(List<TrialBalanceLine> lines, DateTime from, DateTime to, bool includesDrafts)
            {
                _lines = lines ?? new List<TrialBalanceLine>();
                _from = from;
                _to = to;
                _includesDrafts = includesDrafts;
            }

            public string DocumentTitle    => "ميزان المراجعة";
            public string DocumentSubtitle => $"{_from:yyyy-MM-dd} — {_to:yyyy-MM-dd}";

            /// <summary>إلزامي — ستة أعمدة رقمية (افتتاحي/حركة/ختامي × مدين/دائن) + الكود + الاسم، لا تكفي Portrait.</summary>
            public PrintOrientation Orientation => PrintOrientation.Landscape;

            public Dictionary<string, string> HeaderFields => new()
            {
                ["من"]             = _from.ToString("yyyy-MM-dd"),
                ["إلى"]            = _to.ToString("yyyy-MM-dd"),
                ["يشمل المسودات"] = _includesDrafts ? "نعم" : "لا"
            };

            public Dictionary<string, string> FooterFields => null;

            public bool ShowCompanyHeader => true;
            public bool ShowPageNumbers   => true;
            public bool ShowSignatures    => true;
            public List<string> SignatureLabels => new() { "المحاسب", "المدير المالي" };

            public List<PrintSection> BuildSections()
            {
                var columns = new List<PrintColumn>
                {
                    new() { Key = "Code",          Header = "الكود",        Width = 0.8, Align = "Right" },
                    new() { Key = "Name",          Header = "الحساب",       Width = 2.4, Align = "Right" },
                    new() { Key = "OpeningDebit",  Header = "افتتاحي مدين", Width = 1, Align = "Left", Format = "N2" },
                    new() { Key = "OpeningCredit", Header = "افتتاحي دائن", Width = 1, Align = "Left", Format = "N2" },
                    new() { Key = "PeriodDebit",   Header = "حركة مدين",    Width = 1, Align = "Left", Format = "N2" },
                    new() { Key = "PeriodCredit",  Header = "حركة دائن",    Width = 1, Align = "Left", Format = "N2" },
                    new() { Key = "ClosingDebit",  Header = "ختامي مدين",   Width = 1, Align = "Left", Format = "N2" },
                    new() { Key = "ClosingCredit", Header = "ختامي دائن",   Width = 1, Align = "Left", Format = "N2" }
                };

                // إزاحة اسم الحساب حسب Level — منطق عرض بحت محلي هنا (لا خاصية على Model/DTO، نفس درس فحص
                // تسريب المنطق: التنسيق/الإزاحة تعيش في طبقة العرض، لا في البيانات).
                var rows = _lines.Select(l => new Dictionary<string, object>
                {
                    ["Code"]          = l.Code,
                    ["Name"]          = new string(' ', Math.Max(0, l.Level - 1) * 2) + l.Name,
                    ["OpeningDebit"]  = l.OpeningDebit,
                    ["OpeningCredit"] = l.OpeningCredit,
                    ["PeriodDebit"]   = l.PeriodDebit,
                    ["PeriodCredit"]  = l.PeriodCredit,
                    ["ClosingDebit"]  = l.ClosingDebit,
                    ["ClosingCredit"] = l.ClosingCredit,
                    ["__IsLeaf"]      = l.IsLeaf // مفتاح داخلي لـ RowBold أدناه فقط — ليس عموداً حقيقياً (غير مُدرَج في columns)
                }).ToList();

                var totalOpeningDebit  = _lines.Sum(l => l.OpeningDebit);
                var totalOpeningCredit = _lines.Sum(l => l.OpeningCredit);
                var totalPeriodDebit   = _lines.Sum(l => l.PeriodDebit);
                var totalPeriodCredit  = _lines.Sum(l => l.PeriodCredit);
                var totalClosingDebit  = _lines.Sum(l => l.ClosingDebit);
                var totalClosingCredit = _lines.Sum(l => l.ClosingCredit);

                var sections = new List<PrintSection>
                {
                    new()
                    {
                        Type    = PrintSectionType.Table,
                        Columns = columns,
                        Rows    = rows,
                        // حسابات تجميعية (غير Leaf) تظهر bold — لا تُعيدها GetTrialBalance حالياً (تقتصر على
                        // الأوراق)؛ الشرط هنا صحيح ومُعدّ مسبقاً لليوم الذي تُضاف فيه صفوف تجميعية دون تعديل القالب.
                        RowBold = row => row.TryGetValue("__IsLeaf", out var v) && v is bool isLeaf && !isLeaf,
                        Totals  = new List<PrintTotal>
                        {
                            new() { Label = "افتتاحي مدين", Value = totalOpeningDebit.ToString("N2") },
                            new() { Label = "افتتاحي دائن", Value = totalOpeningCredit.ToString("N2") },
                            new() { Label = "حركة مدين",    Value = totalPeriodDebit.ToString("N2") },
                            new() { Label = "حركة دائن",    Value = totalPeriodCredit.ToString("N2") },
                            new() { Label = "ختامي مدين",   Value = totalClosingDebit.ToString("N2") },
                            new() { Label = "ختامي دائن",   Value = totalClosingCredit.ToString("N2") }
                        }
                    }
                };

                if (totalClosingDebit != totalClosingCredit)
                    sections.Add(new PrintSection
                    {
                        Type    = PrintSectionType.Callout,
                        Variant = StatusVariant.Danger,
                        Text    = $"تحذير: الميزان غير متوازن — الفرق {(totalClosingDebit - totalClosingCredit):N2}"
                    });

                return sections;
            }
        }
    }
}
