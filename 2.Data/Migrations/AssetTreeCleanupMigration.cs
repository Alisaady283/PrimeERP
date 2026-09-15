using PrimeERP.Data.Core;

namespace PrimeERP.Data.Migrations
{
    /// <summary>
    /// تنظيف بقايا بنيةٍ سابقة للأصول: حسابات فئاتٍ وأصولٍ معطَّلة تحت جذور الأصول، بلا قيدٍ واحد وبلا
    /// أبناء — تبقى صفوفاً في الجدول فتشغل أكوادها ويبدأ ترقيم الفئة الجديدة بعدها.
    ///
    /// الحذف صلبٌ هنا عن قصد: المعطَّل بلا قيود لا قيمة محاسبية له، والإبقاء عليه يُفسد الترقيم وحده.
    /// وحدود الأمان صريحة: تحت جذور الأصول فقط · معطَّل فقط · بلا أسطر قيود · بلا أبناء.
    /// </summary>
    public static class AssetTreeCleanupMigration
    {
        public static void Apply()
        {
            // الأعمق أولاً: حذف الأب قبل ابنه يترك يتيماً، والشرط «بلا أبناء» يمنعه فيتعطّل التنظيف.
            for (var pass = 0; pass < 4; pass++)
                DbHelper.Execute(@"
                    DELETE FROM Accounts
                    WHERE IsActive = 0
                      AND (Code LIKE '1101%' AND Code NOT IN ('1101', '1101001', '1101002'))
                      AND NOT EXISTS (SELECT 1 FROM JournalEntryLines l WHERE l.AccountCode = Accounts.Code)
                      AND NOT EXISTS (SELECT 1 FROM Accounts c WHERE c.ParentCode = Accounts.Code)");

            // وصفوف الفئات المعطَّلة التي لم يبقَ لحسابها وجود — سجلٌّ بلا حساب لا يصلح أباً لأصل.
            DbHelper.Execute(@"
                DELETE FROM Categories
                WHERE ModuleKey = 'AssetCategories' AND IsActive = 0
                  AND (AccountCode IS NULL OR AccountCode = ''
                       OR NOT EXISTS (SELECT 1 FROM Accounts a WHERE a.Code = Categories.AccountCode))
                  AND NOT EXISTS (SELECT 1 FROM Assets s WHERE s.CategoryId = Categories.Id AND s.IsDeleted = 0)");
        }
    }
}
