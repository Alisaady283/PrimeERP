using System;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Domain.Calculations
{
    /// <summary>تسعير المخزون بالمتوسط المرجَّح المتحرّك</summary>
    public static class InventoryCosting
    {
        /// <summary>رصيدٌ جارٍ</summary>
        public readonly record struct Balance(decimal Qty, decimal Value)
        {
            public decimal UnitCost => Qty == 0 ? 0 : Value / Qty;
        }

        /// <summary>حركةٌ كما يقرؤها الحساب</summary>
        public readonly record struct Entry(MovementType Type, decimal Qty, decimal UnitCost);

        public readonly record struct BatchStock(string Batch, DateTime? Expiry, decimal Qty);

        public static List<BatchStock> Fefo(IEnumerable<BatchStock> stock, decimal qty, DateTime date)
        {
            var picks = new List<BatchStock>();
            foreach (var batch in stock.Where(b => b.Qty > 0 && (b.Expiry == null || b.Expiry.Value.Date >= date.Date))
                                       .OrderBy(b => b.Expiry ?? DateTime.MaxValue).ThenBy(b => b.Batch))
            {
                if (qty <= 0) break;
                var take = Math.Min(qty, batch.Qty);
                picks.Add(batch with { Qty = take });
                qty -= take;
            }
            return qty > 0 ? null : picks;
        }

        public static bool IsIncoming(Entry entry) =>
            entry.Type != MovementType.Out && entry.Qty > 0;

        public static Balance Apply(Balance current, Entry entry, out decimal movementValue)
        {
            var qty = entry.Qty < 0 ? -entry.Qty : entry.Qty;

            if (IsIncoming(entry))
            {
                movementValue = qty * entry.UnitCost;
                return new Balance(current.Qty + qty, current.Value + movementValue);
            }

            movementValue = qty >= current.Qty ? current.Value : qty * current.UnitCost;
            return new Balance(current.Qty - qty, current.Value - movementValue);
        }

        public static Balance Replay(IEnumerable<Entry> ordered)
        {
            var balance = new Balance(0, 0);
            foreach (var entry in ordered) balance = Apply(balance, entry, out _);
            return balance;
        }

        public static bool TryIssueCost(Balance current, decimal qty, out decimal totalCost)
        {
            totalCost = 0;
            if (qty <= 0) return true;
            if (current.Qty < qty) return false;

            Apply(current, new Entry(MovementType.Out, qty, 0), out totalCost);
            return true;
        }

        public static decimal UnitCostOf(decimal totalValue, decimal qty) => qty == 0 ? 0 : totalValue / qty;

        public static decimal Scale(decimal perBatch, decimal batchQty, decimal orderQty, decimal wastePercent = 0) =>
            batchQty == 0 ? 0 : perBatch * orderQty / batchQty * (1 + wastePercent / 100);

        public static decimal OperationCost(decimal minutes, decimal costPerHour) => minutes / 60 * costPerHour;

        public static string ExpiryState(DateTime? expiry, DateTime today, int alertDays) =>
            expiry == null ? "Valid" : expiry.Value.Date < today.Date ? "Expired" : expiry.Value.Date <= today.Date.AddDays(alertDays) ? "Near" : "Valid";

        public static decimal Produced(decimal materials, decimal operations, decimal byProducts) => materials + operations - byProducts;

        public static List<decimal> Distribute(decimal total, IReadOnlyList<decimal> weights)
        {
            var sum = weights.Sum();
            return weights.Select(w => sum == 0 ? total / Math.Max(weights.Count, 1) : total * w / sum).ToList();
        }

        /// <summary>تكلفة السطر وإلا تكلفة الصنف</summary>
        public static decimal LineCost(decimal given, decimal productCost) => given > 0 ? given : productCost;

        /// <summary>الصرف ينقص والباقي يزيد</summary>
        public static decimal Signed(MovementType type, decimal qty) => type == MovementType.Out ? -qty : qty;
    }
}
