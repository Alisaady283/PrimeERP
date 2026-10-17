using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Domain.Calculations
{
    /// <summary>مبالغ سطر الميزان</summary>
    public readonly record struct TrialAmounts(decimal OpeningDebit, decimal OpeningCredit, decimal PeriodDebit,
        decimal PeriodCredit, decimal ClosingDebit, decimal ClosingCredit)
    {
        public bool IsZero => OpeningDebit == 0 && OpeningCredit == 0 && PeriodDebit == 0 && PeriodCredit == 0
                              && ClosingDebit == 0 && ClosingCredit == 0;
    }

    /// <summary>مقاييس القوائم المالية</summary>
    public static class StatementCalc
    {
        public static decimal GrossProfit(decimal sales, decimal cogs) => sales - cogs;

        public static decimal OperatingProfit(decimal grossProfit, decimal operating) => grossProfit - operating;

        public static decimal NetIncome(decimal operatingProfit, decimal otherIncome, decimal otherExpense) =>
            operatingProfit + otherIncome - otherExpense;

        public static decimal ProfitBeforeTax(decimal grossProfit, decimal otherRevenue, decimal operatingExpenses) =>
            grossProfit + otherRevenue - operatingExpenses;

        public static decimal AfterTax(decimal profitBeforeTax, decimal incomeTax) =>
            profitBeforeTax - incomeTax;

        /// <summary>فرق الميزانية، وصفرٌ حين تتوازن</summary>
        public static decimal BalanceGap(decimal assets, decimal liabilities, decimal equity) => assets - liabilities - equity;

        /// <summary>فرق النقدية عن رصيدها</summary>
        public static decimal CashGap(decimal opening, decimal change, decimal closing) =>
            Math.Round(opening + change, 2) - Math.Round(closing, 2);

        /// <summary>سطر الميزان من مجاميعه</summary>
        public static TrialAmounts Trial(decimal openingDebit, decimal openingCredit, decimal periodDebit, decimal periodCredit)
        {
            var opening = openingDebit - openingCredit;
            var closing = opening + periodDebit - periodCredit;
            return new TrialAmounts(Math.Max(opening, 0), Math.Max(-opening, 0), periodDebit, periodCredit, Math.Max(closing, 0), Math.Max(-closing, 0));
        }

        /// <summary>الرصيد بعد كل سطر</summary>
        public static List<decimal> Running<T>(decimal opening, IEnumerable<T> lines, Func<T, decimal> change)
        {
            var balances = new List<decimal>();
            foreach (var line in lines) balances.Add(opening += change(line));
            return balances;
        }

        /// <summary>رصيد كل عقدةٍ بما تحتها</summary>
        public static Dictionary<TKey, decimal> Rollup<T, TKey>(IEnumerable<T> nodes, Func<T, TKey> key, Func<T, TKey> parent,
            Func<T, decimal> own)
        {
            var list = nodes.ToList();
            var children = list.ToLookup(parent);
            var totals = new Dictionary<TKey, decimal>();

            decimal Total(T node) => totals.TryGetValue(key(node), out var done)
                ? done
                : totals[key(node)] = own(node) + children[key(node)].Sum(Total);

            foreach (var node in list) Total(node);
            return totals;
        }
    }
}
