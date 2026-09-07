using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.Modules
{
    /// <summary>
    /// القوائم المالية بشكلها الرسمي: البيان، جزئي، كلي. البنود تُكتب في «جزئي» والمجاميع في «كلي»،
    /// والتدرّج بالإزاحة — فتُقرأ كقائمة محاسبية لا كجدول أرصدة. قطعة واحدة تستوردها قائمة الدخل
    /// والمركز المالي والتدفقات، فلا يبقى لكلٍّ تخطيطه.
    /// </summary>
    public static class FinancialStatementFactory
    {
        public class Line
        {
            public string   Statement { get; set; }
            public decimal? Partial   { get; set; }
            public decimal? Total     { get; set; }
        }

        private const string Indent = "      ";

        /// <summary>عنوان قسم بلا مبلغ.</summary>
        public static Line Heading(string title) => new() { Statement = title };

        /// <summary>مجموع في عمود «كلي».</summary>
        public static Line Grand(string label, decimal amount, int level = 0) =>
            new() { Statement = Repeat(level) + label, Total = amount };

        /// <summary>
        /// قسم كامل: عنوانه، ثم بنوده في «جزئي»، ثم مجموعه في «كلي». يُعرض دائماً ولو بصفر — القائمة
        /// تُعرَّف ببنيتها لا بأرصدتها، وحذف قسم لأنه صفر يخفي عن القارئ أنه صفر أصلاً.
        /// </summary>
        public static IEnumerable<Line> Group(string title, List<Line> items, string totalLabel = null, int level = 1)
        {
            yield return new Line { Statement = Repeat(level) + title };

            foreach (var item in items)
                yield return new Line { Statement = Repeat(level + 1) + item.Statement, Partial = item.Partial };

            yield return new Line
            {
                Statement = Repeat(level + 1) + (totalLabel ?? $"إجمالي {title}"),
                Total = items.Sum(i => i.Partial ?? 0)
            };
        }

        public static decimal Sum(List<Line> items) => items.Sum(i => i.Partial ?? 0);

        // ===== قراءة الأرصدة =====

        /// <summary>الحسابات الورقية ذات الرصيد الختامي — لقوائم اللحظة (المركز المالي).</summary>
        public static List<Line> Closing(IEnumerable<TrialBalanceLine> balance, AccountType type, bool creditNatured,
            Func<string, bool> matches) =>
            Read(balance, type, matches, l => creditNatured ? l.ClosingCredit - l.ClosingDebit : l.ClosingDebit - l.ClosingCredit);

        /// <summary>الحسابات الورقية ذات حركة الفترة — لقوائم الفترة (الدخل والتدفقات).</summary>
        public static List<Line> Period(IEnumerable<TrialBalanceLine> balance, AccountType type, bool creditNatured,
            Func<string, bool> matches) =>
            Read(balance, type, matches, l => creditNatured ? l.PeriodCredit - l.PeriodDebit : l.PeriodDebit - l.PeriodCredit);

        /// <summary>
        /// القراءة عند مستوى التجميع لا عند الورقة: الشجرة ثلاث طبقات — رئيسي (١..٥)، وتجميعي بينهما،
        /// وتفصيلي هو الورقة. القائمة تعرض «ذمم مدينة» سطراً واحداً لا اسم كل عميل، فتُجمَع أرصدة
        /// الأوراق عند أبيها التجميعي. وحسابٌ ورقةٌ بذاته (كالأرباح المحتجزة) يظهر باسمه.
        /// </summary>
        private static List<Line> Read(IEnumerable<TrialBalanceLine> balance, AccountType type,
            Func<string, bool> matches, Func<TrialBalanceLine, decimal> amount)
        {
            return balance
                .Where(l => l.IsLeaf && l.Type == type && matches(l.Code))
                .GroupBy(GroupOf)
                .Select(g => new Line { Statement = g.Key.Name, Partial = g.Sum(amount) })
                .OrderBy(l => l.Statement, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>أب الورقة التجميعي، أو الورقة نفسها إن كانت رئيسية أو بلا أب.</summary>
        private static (string Code, string Name) GroupOf(TrialBalanceLine leaf) =>
            string.IsNullOrEmpty(leaf.ParentCode) || leaf.Level <= 2
                ? (leaf.Code, leaf.Name)
                : (leaf.ParentCode, leaf.ParentName ?? leaf.ParentCode);

        public static Func<string, bool> StartsWith(params string[] prefixes) =>
            code => prefixes.Any(p => !string.IsNullOrEmpty(p) && code != null && code.StartsWith(p, StringComparison.Ordinal));

        public static Func<string, bool> StartsWithBut(string prefix, string excluded) =>
            code => code != null && code.StartsWith(prefix, StringComparison.Ordinal) &&
                    (string.IsNullOrWhiteSpace(excluded) || !code.StartsWith(excluded, StringComparison.Ordinal));

        // ===== الأعمدة =====

        public static List<GridColumn> Columns() => new()
        {
            new() { Header = "البيان", Binding = nameof(Line.Statement), Width = 320, IsStarWidth = true },
            new() { Header = "جزئي",   Binding = nameof(Line.Partial), Width = 150, Align = ColumnAlign.Center, Format = "N2" },
            new() { Header = "كلي",    Binding = nameof(Line.Total),   Width = 150, Align = ColumnAlign.Center, Format = "N2" },
        };

        private static string Repeat(int level) => string.Concat(Enumerable.Repeat(Indent, Math.Max(0, level)));
    }
}
