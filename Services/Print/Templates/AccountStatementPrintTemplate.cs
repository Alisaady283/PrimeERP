using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Services.Accounting.DTOs;

namespace PrimeERP.Services.Print.Templates
{
    /// <summary>
    /// يبني IPrintable لكشف حساب من AccountDto + List&lt;AccountStatementLine&gt; (مصدرهما IAccountService.
    /// GetByCode/GetStatement) — لا يستدعي IAccountService هنا؛ المستدعي (ViewModel أو الخدمة) يجلب البيانات
    /// ويمرّرها، نفس نمط JournalEntryPrint في PrintTemplates.cs.
    /// </summary>
    public static class AccountStatementPrintTemplate
    {
        public static IPrintable Build(AccountDto account, List<AccountStatementLine> lines, DateTime from, DateTime to) =>
            new AccountStatementPrintable(account, lines, from, to);

        private class AccountStatementPrintable : IPrintable
        {
            private readonly AccountDto _account;
            private readonly List<AccountStatementLine> _lines;
            private readonly DateTime _from;
            private readonly DateTime _to;

            public AccountStatementPrintable(AccountDto account, List<AccountStatementLine> lines, DateTime from, DateTime to)
            {
                _account = account;
                _lines = lines ?? new List<AccountStatementLine>();
                _from = from;
                _to = to;
            }

            public string DocumentTitle    => "كشف حساب";
            public string DocumentSubtitle => $"{_account.Code} — {_account.Name}";

            // Portrait ثابتة: 6 أعمدة قريبة من عدد أعمدة JournalEntryPrint (5) الذي يعمل بصورة جيدة في Portrait —
            // لا إشارة موثوقة هنا لـ"لا تكفي" تُحسب برمجياً (تعتمد على طول نصوص البيان الفعلية وقت الطباعة)،
            // فتُركت ثابتة بدل تخمين. تُصبح قابلة للتهيئة لاحقاً لو ظهرت حاجة حقيقية.
            public PrintOrientation Orientation => PrintOrientation.Portrait;

            public Dictionary<string, string> HeaderFields => new()
            {
                ["كود الحساب"] = _account.Code,
                ["الاسم"]      = _account.Name,
                ["النوع"]      = _account.TypeName,
                ["من"]         = _from.ToString("yyyy-MM-dd"),
                ["إلى"]        = _to.ToString("yyyy-MM-dd")
            };

            public Dictionary<string, string> FooterFields => null;

            public bool ShowCompanyHeader => true;
            public bool ShowPageNumbers   => true;
            public bool ShowSignatures    => true;
            public List<string> SignatureLabels => new() { "المحاسب", "المدير المالي" };

            public List<PrintSection> BuildSections()
            {
                var opening = _lines.FirstOrDefault(l => l.SourceType == "Opening");
                var movementLines = _lines.Where(l => l.SourceType != "Opening").ToList();

                var columns = new List<PrintColumn>
                {
                    new() { Key = "Date",          Header = "التاريخ",       Width = 1.2, Align = "Right" },
                    new() { Key = "EntryNo",       Header = "رقم القيد",     Width = 1.2, Align = "Right" },
                    new() { Key = "Description",   Header = "البيان",        Width = 2.6, Align = "Right" },
                    new() { Key = "Debit",         Header = "مدين",          Width = 1,   Align = "Left", Format = "N2" },
                    new() { Key = "Credit",        Header = "دائن",          Width = 1,   Align = "Left", Format = "N2" },
                    new() { Key = "RunningBalance",Header = "الرصيد الجاري", Width = 1.2, Align = "Left", Format = "N2" }
                };

                var rows = movementLines.Select(l => new Dictionary<string, object>
                {
                    ["Date"]           = l.Date,
                    ["EntryNo"]        = l.EntryNo,
                    ["Description"]    = l.Description,
                    ["Debit"]          = l.Debit,
                    ["Credit"]         = l.Credit,
                    ["RunningBalance"] = l.RunningBalance
                }).ToList();

                var totalDebit  = movementLines.Sum(l => l.Debit);
                var totalCredit = movementLines.Sum(l => l.Credit);
                var closing     = _lines.Count > 0 ? _lines[^1].RunningBalance : (opening?.RunningBalance ?? 0m);

                return new List<PrintSection>
                {
                    new()
                    {
                        Type = PrintSectionType.KeyValues,
                        KeyValues = new Dictionary<string, string>
                        {
                            ["الرصيد الافتتاحي"] = (opening?.RunningBalance ?? 0m).ToString("N2")
                        }
                    },
                    new()
                    {
                        Type    = PrintSectionType.Table,
                        Columns = columns,
                        Rows    = rows,
                        Totals  = new List<PrintTotal>
                        {
                            new() { Label = "إجمالي مدين",   Value = totalDebit.ToString("N2") },
                            new() { Label = "إجمالي دائن",   Value = totalCredit.ToString("N2") },
                            new() { Label = "الرصيد الختامي", Value = closing.ToString("N2") }
                        }
                    }
                };
            }
        }
    }
}
