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

        public static Line Item(string label, decimal amount, int level = 1) =>
            new() { Statement = Repeat(level) + label, Partial = amount };

        public static decimal Sum(List<Line> items) => items.Sum(i => i.Partial ?? 0);




        public static List<Line> Closing(IEnumerable<TrialBalanceLine> balance, AccountType type, bool creditNatured,
            Func<string, bool> matches) =>
            Grouped(balance.Where(l => type == AccountType.Equity ? l.Level == 2
                    : (type == AccountType.Asset || type == AccountType.Liability) && l.Level > 2 && l.Level < 5),
                type, matches, l => creditNatured ? l.ClosingCredit - l.ClosingDebit : l.ClosingDebit - l.ClosingCredit);

        public static List<Line> Period(IEnumerable<TrialBalanceLine> balance, AccountType type, bool creditNatured,
            Func<string, bool> matches) =>
            Grouped(balance, type, matches, l => creditNatured ? l.PeriodCredit - l.PeriodDebit : l.PeriodDebit - l.PeriodCredit);

        public static List<Line> Grouped(IEnumerable<TrialBalanceLine> balance, AccountType type,
            Func<string, bool> matches, Func<TrialBalanceLine, decimal> amount, int ownLevel = 2) =>
            Groups(balance, l => l.Type == type && matches(l.Code), amount, ownLevel);

        public static List<Line> Groups(IEnumerable<TrialBalanceLine> balance, Func<TrialBalanceLine, bool> where,
            Func<TrialBalanceLine, decimal> amount, int ownLevel = 2)
        {
            return balance
                .Where(l => l.IsLeaf && where(l))
                .GroupBy(l => GroupOf(l, ownLevel))
                .Select(g => new Line { Statement = g.Key.Name, Partial = g.Sum(amount) })
                .OrderBy(l => l.Statement, StringComparer.Ordinal)
                .ToList();
        }

        private static (string Code, string Name) GroupOf(TrialBalanceLine leaf, int ownLevel) =>
            string.IsNullOrEmpty(leaf.ParentCode) || leaf.Level <= ownLevel
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
