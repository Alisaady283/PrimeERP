namespace PrimeERP.Domain.Results
{
    /// <summary>
    /// مفردات حالة الأعمال المقفلة — الستة فقط، لا سابع أبداً. أي حالة أعمال جديدة (Posted/Draft/Cancelled...)
    /// تُصنَّف لواحدة من هذه الستة (راجع "مفردات الحالة المقفلة" في DESIGN_SYSTEM.md للتصنيف الكامل).
    /// Service/Repository/Validator/Core/Model يرجعون هذا (أو enum حالة أعمال يُحوَّل إليه في الـ ViewModel) —
    /// لا يعرفون لوناً ولا خطاً إطلاقاً. التحويل لـ Brush/PrintTheme/ExportTheme يحدث في طبقة العرض فقط.
    /// </summary>
    public enum StatusVariant
    {
        /// <summary>لا دلالة — NoState/Disabled/Empty.</summary>
        Neutral,

        /// <summary>معلومة — Note/Information/Hint.</summary>
        Info,

        /// <summary>إجراء أساسي — PrimaryAction/Selected.</summary>
        Brand,

        /// <summary>نجاح — Posted/Active/Balanced/Paid/Valid/Completed.</summary>
        Success,

        /// <summary>تحذير — Draft/Pending/PartialPaid/LowStock/Warning.</summary>
        Warning,

        /// <summary>خطأ — Cancelled/Inactive/Unbalanced/OutOfStock/Invalid/Failed/Overdue.</summary>
        Danger
    }
}
