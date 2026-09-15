using PrimeERP.Data.Core;

namespace PrimeERP.Data.Migrations
{
    /// <summary>
    /// روابط سحبٍ هدفُها مستندٌ لم يعد موجوداً. الفواتير والمرتجعات كانت تسجّل روابطها ولا تُزيلها عند
    /// الحذف، فبقيت تحرس مصدرها: إذن استلامٍ يُرفض حذفه بحجّة أنه سُحب منه، والساحبُ محذوفٌ أصلاً.
    ///
    /// الإزالة أُضيفت في الخدمات الأربع، وهذه تنظّف ما خلّفه غيابها. آمنة للتكرار: لا تحذف إلا يتيماً.
    /// </summary>
    public static class OrphanDocumentLinksMigration
    {
        // اسم الكيان هو اسم جدوله في الأربعة — فالتحقّق من وجود الهدف استعلامٌ مباشر.
        private static readonly string[] Targets =
        {
            "SalesInvoices", "PurchaseInvoices", "SalesReturns", "PurchaseReturns",
        };

        public static void Apply()
        {
            foreach (var target in Targets)
                DbHelper.Execute(
                    $@"DELETE FROM DocumentLinks
                       WHERE TargetType = @target
                         AND NOT EXISTS (SELECT 1 FROM {target} t WHERE t.Id = DocumentLinks.TargetId)",
                    new System.Collections.Generic.Dictionary<string, object> { ["@target"] = target });
        }
    }
}
