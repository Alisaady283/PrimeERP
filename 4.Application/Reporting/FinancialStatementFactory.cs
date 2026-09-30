using PrimeERP.Platform.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Application.Reporting
{
    /// <summary>القوائم المالية بشكلها الرسمي</summary>
    public static class FinancialStatementFactory
    {
        public class Line
        {
            public string   Statement { get; set; }
            public decimal? Partial   { get; set; }
            public decimal? Total     { get; set; }
            public string   Kind      { get; set; }
        }

        public static readonly Func<object, string> RowKind = row => (row as Line)?.Kind;

        private const string Indent = "      ";

        public static Line Heading(string title) => new() { Statement = title, Kind = "heading" };

        public static Line Grand(string label, decimal amount, int level = 0) =>
            new() { Statement = Repeat(level) + label, Total = amount, Kind = "total" };

        public static IEnumerable<Line> Group(string title, List<Line> items, string totalLabel = null, int level = 1)
        {
            yield return new Line { Statement = Repeat(level) + title, Kind = "heading" };

            foreach (var item in items)
                yield return new Line { Statement = Repeat(level + 1) + item.Statement, Partial = item.Partial };

            yield return new Line
            {
                Statement = Repeat(level + 1) + (totalLabel ?? LocalizationService.Get("Str.Statement.TotalOf", title)),
                Total = items.Sum(i => i.Partial ?? 0),
                Kind = "total"
            };
        }

        public static decimal Sum(List<Line> items) => items.Sum(i => i.Partial ?? 0);


        public static List<Line> Closing(IEnumerable<TrialBalanceLine> balance, AccountType type, bool creditNatured,
            Func<string, bool> matches) =>
            Read(balance, type, matches, l => creditNatured ? l.ClosingCredit - l.ClosingDebit : l.ClosingDebit - l.ClosingCredit);

        public static List<Line> Period(IEnumerable<TrialBalanceLine> balance, AccountType type, bool creditNatured,
            Func<string, bool> matches) =>
            Read(balance, type, matches, l => creditNatured ? l.PeriodCredit - l.PeriodDebit : l.PeriodDebit - l.PeriodCredit);

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

        private static (string Code, string Name) GroupOf(TrialBalanceLine leaf) =>
            string.IsNullOrEmpty(leaf.ParentCode) || leaf.Level <= 2
                ? (leaf.Code, leaf.Name)
                : (leaf.ParentCode, leaf.ParentName ?? leaf.ParentCode);

        public static Func<string, bool> StartsWith(params string[] prefixes) =>
            code => prefixes.Any(p => !string.IsNullOrEmpty(p) && code != null && code.StartsWith(p, StringComparison.Ordinal));

        public static Func<string, bool> StartsWithBut(string prefix, string excluded) =>
            code => code != null && code.StartsWith(prefix, StringComparison.Ordinal) &&
                    (string.IsNullOrWhiteSpace(excluded) || !code.StartsWith(excluded, StringComparison.Ordinal));

        private static string Repeat(int level) => string.Concat(Enumerable.Repeat(Indent, Math.Max(0, level)));
    }
}
