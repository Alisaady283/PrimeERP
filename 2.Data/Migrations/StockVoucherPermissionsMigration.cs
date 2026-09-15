using PrimeERP.Data.Core;

namespace PrimeERP.Data.Migrations
{
    /// <summary>
    /// أذون الدورة الأربعة (استلام · صرف · صرف مرتجع · استلام مرتجع) كانت تسأل عن مفاتيح غير مُعرَّفة
    /// في PermissionKeys، فتُرفض عند كل دور. المفاتيح أُضيفت، وهذه تمنحها لمن كان يملكها ضمناً:
    /// كل دورٍ يملك Inventory.Create كان مقصوداً به تشغيل هذه الأذون، فلا يُترك مكسوراً بانتظار منحٍ يدوي.
    ///
    /// دور مدير النظام يتولّاه PermissionDb عند الإقلاع (يمنحه كل مفتاح ناقص)، فلا يُعالَج هنا.
    /// </summary>
    public static class StockVoucherPermissionsMigration
    {
        private static readonly string[] VoucherKeys =
        {
            "Inventory.GoodsReceipt", "Inventory.GoodsIssue",
            "Inventory.DeliveryNote", "Inventory.SalesReceipt",
        };

        public static void Apply()
        {
            foreach (var key in VoucherKeys)
                DbHelper.Execute(
                    @"INSERT INTO RolePermissions (RoleId, PermissionKey)
                      SELECT r.RoleId, @key FROM RolePermissions r
                      WHERE r.PermissionKey = 'Inventory.Create'
                        AND NOT EXISTS (SELECT 1 FROM RolePermissions x
                                        WHERE x.RoleId = r.RoleId AND x.PermissionKey = @key)",
                    new System.Collections.Generic.Dictionary<string, object> { ["@key"] = key });
        }
    }
}
