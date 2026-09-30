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
    }
}
