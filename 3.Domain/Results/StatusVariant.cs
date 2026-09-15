namespace PrimeERP.Domain.Results
{
    /// <summary>
    /// مفردات حالة الأعمال المقفلة — الستة فقط، لا سابع أبداً. أي حالة أعمال جديدة تُصنَّف لواحدة منها.
    /// الخدمة والمستودع والمتحقّق والكيان يرجعون هذا (أو enum حالة أعمال يُحوَّل إليه في نموذج العرض) ولا
    /// يعرفون لوناً ولا خطاً. التحويل إلى Brush/PrintTheme/ExportTheme يحدث في طبقة العرض وحدها.
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
