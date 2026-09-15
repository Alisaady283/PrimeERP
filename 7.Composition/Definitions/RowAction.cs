using System;
using PrimeERP.Domain.Results;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>إجراء إضافي على السجل المحدَّد بجانب تعديل/حذف (ترحيل قيد، تحريك شيك…). يُعلَن في تسجيل
    /// الوحدة ويُصيَّر تلقائياً — لا شاشة تبني زراً خاصاً بها في كودها.</summary>
    public class RowAction
    {
        public required string Label { get; init; }
        public string Variant { get; init; } = "secondary";
        public string PermissionKey { get; init; }

        /// <summary>هل ينطبق على هذا السجل تحديداً — سجل مرحَّل لا يُرحَّل ثانية مثلاً.</summary>
        public Func<object, bool> AppliesTo { get; init; }

        /// <summary>إجراءٌ على الكل لا على سجلّ (احتساب إهلاك كل الأصول) — يعمل بلا صفٍّ محدَّد.</summary>
        public bool RequiresSelection { get; init; } = true;

        public required Func<IServiceProvider, object, Result> Execute { get; init; }
    }
}
