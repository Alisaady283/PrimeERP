using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Domain.Calculations
{
    /// <summary>قواعد المشاريع النقيّة: تكلفة الساعات، والمستخلص وضمانه، والإنجاز المرجَّح، والربحية</summary>
    public static class ProjectCalc
    {
        /// <summary>تكلفة الساعات — سعر الساعة المكتوب وإلا الافتراضي</summary>
        public static decimal LaborCost(decimal hours, decimal hourlyCost, decimal defaultHourlyCost) =>
            Math.Round(hours * (hourlyCost > 0 ? hourlyCost : defaultHourlyCost), 2);

        /// <summary>قيمة المستخلص: المكتوبة، وإلا نسبته من قيمة العقد</summary>
        public static decimal BillAmount(decimal amount, decimal percent, decimal contractValue) =>
            amount > 0 ? amount : Math.Round(contractValue * percent / 100m, 2);

        /// <summary>ضمان الأعمال المحجوز من المستخلص</summary>
        public static decimal Retention(decimal amount, decimal retentionPercent) => Math.Round(amount * retentionPercent / 100m, 2);

        /// <summary>نسبة الإنجاز مرجَّحةً بأوزان المهام — بلا أوزانٍ متوسطٌ بسيط</summary>
        public static decimal Progress(IEnumerable<(decimal Weight, decimal Progress)> tasks)
        {
            var list = tasks.ToList();
            if (list.Count == 0) return 0;
            var weight = list.Sum(t => t.Weight > 0 ? t.Weight : 1);
            return Math.Round(list.Sum(t => (t.Weight > 0 ? t.Weight : 1) * Math.Clamp(t.Progress, 0, 100)) / weight, 1);
        }

        /// <summary>ما استُهلك من الميزانية نسبةً</summary>
        public static decimal BudgetUsed(decimal cost, decimal budget) => budget <= 0 ? 0 : Math.Round(cost / budget * 100m, 1);

        /// <summary>الربح وهامشه من المستخلص والتكلفة</summary>
        public static (decimal Profit, decimal Margin) Profit(decimal billed, decimal cost) =>
            (billed - cost, billed == 0 ? 0 : Math.Round((billed - cost) / billed * 100m, 1));
    }
}
