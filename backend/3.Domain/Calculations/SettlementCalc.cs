using System;
using System.Collections.Generic;

namespace PrimeERP.Domain.Calculations
{
    /// <summary>توزيع السداد على الفواتير</summary>
    public static class SettlementCalc
    {
        /// <summary>الاستحقاق: تاريخ الفاتورة وأيام شروط الدفع للطرف</summary>
        public static DateTime DueDate(DateTime invoiceDate, int paymentTermDays) => invoiceDate.Date.AddDays(Math.Max(paymentTermDays, 0));

        /// <summary>المسمّى أولاً على فاتورته، والباقي على الأقدم فالأحدث؛ ويُنقص المتبقي في المكان</summary>
        public static List<(int InvoiceId, decimal Amount, bool Auto)> Allocate(decimal amount, IEnumerable<(int InvoiceId, decimal Amount)> named,
            IReadOnlyList<int> oldestFirst, Dictionary<int, decimal> remaining)
        {
            var applied = new List<(int, decimal, bool)>();

            void Take(int id, decimal wanted, bool auto)
            {
                var take = Math.Min(Math.Min(wanted, amount), remaining.GetValueOrDefault(id));
                if (take <= 0) return;
                remaining[id] -= take;
                amount -= take;
                applied.Add((id, take, auto));
            }

            foreach (var (id, wanted) in named) Take(id, wanted, auto: false);
            foreach (var id in oldestFirst)
            {
                if (amount <= 0) break;
                Take(id, amount, auto: true);
            }
            return applied;
        }
    }
}
