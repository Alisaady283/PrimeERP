using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Modules
{
    /// <summary>
    /// القوائم المالية بالشكل المحاسبي: أقسام مصنَّفة، وإجماليات فرعية بينها، لا جدول أرصدة مسطّح.
    /// التصنيف من كود الحساب نفسه — الشجرة مصنَّفة أصلاً (١١ غير متداولة، ١٢ متداولة، ٢١ متداولة،
    /// ٢٢ طويلة الأجل، ٥١ تشغيلية، ٥٢ أخرى) فلا يحتاج التقرير بيانات إضافية.
    /// </summary>
    public static class FinancialStatementFactory
    {
        public class Line
        {
            public string  Code   { get; set; }
            public string  Name   { get; set; }
            public decimal Amount { get; set; }
        }

        /// <summary>عنوان قسم ثم سطوره ثم مجموعه — القسم الفارغ لا يُعرض إطلاقاً.</summary>
        public static IEnumerable<Line> Section(string title, List<Line> lines, string totalLabel = null)
        {
            if (lines.Count == 0) yield break;

            yield return new Line { Name = title };
            foreach (var line in lines) yield return line;
            yield return new Line { Name = totalLabel ?? $"إجمالي {title}", Amount = lines.Sum(l => l.Amount) };
        }

        /// <summary>سطر نتيجة بين الأقسام (مجمل الربح، الربح التشغيلي، صافي الربح).</summary>
        public static Line Result(string label, decimal amount) => new() { Name = label, Amount = amount };

        /// <summary>الحسابات الورقية ذات الحركة التي يبدأ كودها بأحد البادئات.</summary>
        public static List<Line> Under(IEnumerable<TrialBalanceLine> balance, AccountType type, bool creditNatured,
            Func<string, bool> matches)
        {
            return balance
                .Where(l => l.IsLeaf && l.Type == type && matches(l.Code))
                .Select(l => new Line
                {
                    Code = l.Code,
                    Name = l.Name,
                    Amount = creditNatured ? l.ClosingCredit - l.ClosingDebit : l.ClosingDebit - l.ClosingCredit
                })
                .Where(l => l.Amount != 0)
                .ToList();
        }

        /// <summary>مثلها لكن بحركة الفترة لا بالرصيد الختامي — قائمة الدخل فترة لا لحظة.</summary>
        public static List<Line> PeriodUnder(IEnumerable<TrialBalanceLine> balance, AccountType type, bool creditNatured,
            Func<string, bool> matches)
        {
            return balance
                .Where(l => l.IsLeaf && l.Type == type && matches(l.Code))
                .Select(l => new Line
                {
                    Code = l.Code,
                    Name = l.Name,
                    Amount = creditNatured ? l.PeriodCredit - l.PeriodDebit : l.PeriodDebit - l.PeriodCredit
                })
                .Where(l => l.Amount != 0)
                .ToList();
        }

        public static Func<string, bool> StartsWith(params string[] prefixes) =>
            code => prefixes.Any(p => code != null && code.StartsWith(p, StringComparison.Ordinal));

        public static Func<string, bool> StartsWithBut(string prefix, string excluded) =>
            code => code != null && code.StartsWith(prefix, StringComparison.Ordinal) &&
                    (string.IsNullOrWhiteSpace(excluded) || !code.StartsWith(excluded, StringComparison.Ordinal));
    }
}
