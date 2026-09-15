using PrimeERP.Data.Core;

namespace PrimeERP.Data.Migrations
{
    /// <summary>
    /// الأصول التي سبقت عمود «القيمة بعد إعادة التقييم» تحمل فيه صفراً، وهو أساس الإهلاك والدفترية —
    /// فيُملأ بتكلفة الشراء: أصلٌ لم يُعَد تقييمه قيمته تكلفته. لا يمسّ أصلاً أُعيد تقييمه فعلاً.
    /// </summary>
    public static class AssetRevaluedValueMigration
    {
        public static void Apply() =>
            DbHelper.Execute("UPDATE Assets SET RevaluedValue = PurchaseCost WHERE RevaluedValue IS NULL OR RevaluedValue = 0");
    }
}
