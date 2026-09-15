using PrimeERP.Data.Core;

namespace PrimeERP.Data.Migrations
{
    /// <summary>
    /// شكل جذور الأصول في شجرة الحسابات: الصافي أباً لابنَين تجميعيَّين — التكلفة تسكنها الفئات وتحتها
    /// أصولها، والمجمّع تسكنه مرايا الفئات وتحتها مجمّعات أصولها. البذرة لا تمسّ حساباً قائماً، فقواعد
    /// سبقت هذا الشكل تبقى بأسمائها القديمة وبـIsLeaf يمنع سكنى الأبناء.
    /// </summary>
    public static class AssetTreeShapeMigration
    {
        public static void Apply()
        {
            Rename("1101", "صافي الأصول الثابتة");
            Rename("1101001", "الأصول الثابتة");

            // الجذران يقبلان أبناءً: الفئة تحت الأول، ومرآتها تحت الثاني.
            Group("1101001");
            Group("1101002");
        }

        private static void Rename(string code, string name) =>
            DbHelper.Execute("UPDATE Accounts SET Name = @name WHERE Code = @code",
                new System.Collections.Generic.Dictionary<string, object> { ["@name"] = name, ["@code"] = code });

        private static void Group(string code) =>
            DbHelper.Execute("UPDATE Accounts SET IsLeaf = @leaf WHERE Code = @code",
                new System.Collections.Generic.Dictionary<string, object> { ["@leaf"] = false, ["@code"] = code });
    }
}
