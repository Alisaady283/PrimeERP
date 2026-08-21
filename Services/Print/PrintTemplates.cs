using System.Collections.Generic;
using System.Linq;
using PrimeERP.Models;

namespace PrimeERP.Services.Print
{
    /// <summary>
    /// قوالب جاهزة تبني IPrintable من Models حقيقية — PrintService نفسه لا يعرف JournalEntry ولا أي Model.
    /// AccountStatementPrintTemplate/TrialBalancePrintTemplate (F.2.4) الآن في Services/Print/Templates/ —
    /// ملفان منفصلان (لا هنا) لأنهما يُبنيان من DTOs (AccountStatementLine/TrialBalanceLine) لا من Model خام.
    /// ReportPrint لا يزال مؤجَّلاً: يحتاج ReportResult (المرحلة F.5) — لا نموذج بيانات حقيقي له بعد.
    /// </summary>
    public static class PrintTemplates
    {
        public static IPrintable JournalEntryPrint(JournalEntry entry) => new JournalEntryPrintable(entry);

        private class JournalEntryPrintable : IPrintable
        {
            private readonly JournalEntry _entry;

            public JournalEntryPrintable(JournalEntry entry) => _entry = entry;

            public string DocumentTitle    => "قيد يومية";
            public string DocumentSubtitle => _entry.EntryNo;
            public PrintOrientation Orientation => PrintOrientation.Portrait;

            public Dictionary<string, string> HeaderFields => new()
            {
                ["رقم القيد"] = _entry.EntryNo,
                ["التاريخ"]   = _entry.EntryDate,
                ["المصدر"]    = _entry.Source,
                ["الحالة"]    = _entry.IsPosted ? "✔ مرحّل" : "◷ مسودة",
                ["البيان"]    = _entry.Description
            };

            public Dictionary<string, string> FooterFields => new()
            {
                ["أُنشئ بواسطة"] = _entry.CreatedBy
            };

            public bool ShowCompanyHeader => true;
            public bool ShowPageNumbers   => true;
            public bool ShowSignatures    => true;
            public List<string> SignatureLabels => new() { "المُعِد", "المراجع", "المعتمِد" };

            public List<PrintSection> BuildSections()
            {
                var columns = new List<PrintColumn>
                {
                    new() { Key = "AccountCode", Header = "الحساب",  Width = 2, Align = "Right" },
                    new() { Key = "AccountName", Header = "الاسم",   Width = 3, Align = "Right" },
                    new() { Key = "Debit",       Header = "مدين",    Width = 1.5, Align = "Left", Format = "N2" },
                    new() { Key = "Credit",      Header = "دائن",    Width = 1.5, Align = "Left", Format = "N2" },
                    new() { Key = "Notes",       Header = "ملاحظات", Width = 2, Align = "Right" }
                };

                var rows = _entry.Lines.Select(l => new Dictionary<string, object>
                {
                    ["AccountCode"] = l.AccountCode,
                    ["AccountName"] = l.AccountName,
                    ["Debit"]       = l.Debit,
                    ["Credit"]      = l.Credit,
                    ["Notes"]       = l.Notes
                }).ToList();

                var totals = new List<PrintTotal>
                {
                    new() { Label = "إجمالي مدين", Value = _entry.TotalDebit.ToString("N2"),  IsBold = true },
                    new() { Label = "إجمالي دائن", Value = _entry.TotalCredit.ToString("N2"), IsBold = true }
                };

                return new List<PrintSection>
                {
                    new()
                    {
                        Type    = PrintSectionType.Table,
                        Columns = columns,
                        Rows    = rows,
                        Totals  = totals
                    }
                };
            }
        }
    }
}
