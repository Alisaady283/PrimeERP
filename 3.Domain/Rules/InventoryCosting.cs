using System.Collections.Generic;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Domain.Rules
{
    /// <summary>
    /// تسعير المخزون بالمتوسط المرجَّح المتحرّك — قاعدة نقيّة بلا قاعدة بيانات ولا إعدادات. تستوردها
    /// خدمةُ المخزون فتستهلكها الفواتير، ويستوردها تقريرُ حركة الأصناف فيعرض الرصيد سطراً سطراً —
    /// فالرقم المُرحَّل في القيد هو نفسه المعروض في التقرير، لا حسابان قد يفترقان.
    ///
    /// الرصيد يُشتقّ من سجلّ الحركات ولا يُخزَّن: إعادة قراءتها بالترتيب تعطي الحالة نفسها دائماً،
    /// وحذفُ مستندٍ يُصحّحها تلقائياً — بخلاف عمودٍ محفوظ يحتاج فكّاً دقيقاً عند كل حذف.
    ///
    /// والمتوسط يُلغي سؤال «من أي دفعةٍ عاد المرتجع؟» الذي لا جواب له في الوارد أوّلاً.
    /// </summary>
    public static class InventoryCosting
    {
        /// <summary>رصيدٌ جارٍ: كميةٌ وقيمة، وسعر الوحدة مشتقٌّ منهما لا مخزَّن.</summary>
        public readonly record struct Balance(decimal Qty, decimal Value)
        {
            /// <summary>القيمة على الكمية — صفرٌ عند نفاد الرصيد، فلا قسمة على صفر.</summary>
            public decimal UnitCost => Qty == 0 ? 0 : Value / Qty;
        }

        /// <summary>حركةٌ كما يقرؤها الحساب — نوعها وكميتها وتكلفة وحدتها، بلا شيء آخر.</summary>
        public readonly record struct Entry(MovementType Type, decimal Qty, decimal UnitCost);

        /// <summary>هل الحركة تزيد الرصيد. التسوية بإشارة كميتها: موجبةً وارد، وسالبةً منصرف.</summary>
        public static bool IsIncoming(Entry entry) =>
            entry.Type != MovementType.Out && entry.Qty > 0;

        /// <summary>
        /// الرصيد بعد حركةٍ واحدة، وقيمتها. الوارد يضيف كميته بقيمتها فيتحرّك المتوسط، والمنصرف يخرج
        /// **بمتوسط اللحظة** لا بسعره المكتوب — فقيمة ما بقي تبقى متّسقة مع كميته.
        /// </summary>
        public static Balance Apply(Balance current, Entry entry, out decimal movementValue)
        {
            var qty = entry.Qty < 0 ? -entry.Qty : entry.Qty;

            if (IsIncoming(entry))
            {
                movementValue = qty * entry.UnitCost;
                return new Balance(current.Qty + qty, current.Value + movementValue);
            }

            // المنصرف بمتوسط اللحظة. صرفُ كل الرصيد يأخذ قيمته كاملةً فلا يبقى كسرُ تقريب.
            movementValue = qty >= current.Qty ? current.Value : qty * current.UnitCost;
            return new Balance(current.Qty - qty, current.Value - movementValue);
        }

        /// <summary>الرصيد بعد إعادة قراءة الحركات بترتيب ورودها.</summary>
        public static Balance Replay(IEnumerable<Entry> ordered)
        {
            var balance = new Balance(0, 0);
            foreach (var entry in ordered) balance = Apply(balance, entry, out _);
            return balance;
        }

        /// <summary>
        /// تكلفة صرف كميةٍ من رصيدٍ قائم — إجمالاً لا وحدةً. تُرجع false لو تجاوزت الكميةُ الرصيد،
        /// ولا تُنتج رقماً حينها.
        /// </summary>
        public static bool TryIssueCost(Balance current, decimal qty, out decimal totalCost)
        {
            totalCost = 0;
            if (qty <= 0) return true;
            if (current.Qty < qty) return false;

            Apply(current, new Entry(MovementType.Out, qty, 0), out totalCost);
            return true;
        }

        /// <summary>متوسط تكلفة الوحدة في حركةٍ بعينها — قيمتها مقسومةً على كميتها.</summary>
        public static decimal UnitCostOf(decimal totalValue, decimal qty) => qty == 0 ? 0 : totalValue / qty;
    }
}
