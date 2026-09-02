using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>يبني شجرة الصلاحيات من PermissionKeys.All() آلياً — موديول ← إجراء. لا قائمة يدوية:
    /// أي مفتاح جديد يظهر تلقائياً في الشاشتين.</summary>
    public static class PermissionTreeFactory
    {
        private static readonly Dictionary<string, string> ModuleNames = new()
        {
            ["Accounts"] = "شجرة الحسابات", ["Journal"] = "قيود اليومية",
            ["Customers"] = "العملاء", ["Suppliers"] = "الموردون", ["Products"] = "الأصناف",
            ["Categories"] = "الفئات", ["Brands"] = "الماركات", ["Units"] = "الوحدات",
            ["Warehouses"] = "المخازن", ["AssetCategories"] = "فئات الأصول",
            ["Departments"] = "الأقسام", ["JobTitles"] = "المسميات الوظيفية",
            ["Assets"] = "الأصول الثابتة", ["Inventory"] = "المخزون",
            ["Sales"] = "المبيعات", ["Purchases"] = "المشتريات", ["HR"] = "الموارد البشرية",
            ["Reports"] = "التقارير", ["Settings"] = "الإعدادات", ["Users"] = "المستخدمون",
        };

        private static readonly Dictionary<string, string> ActionNames = new()
        {
            ["View"] = "عرض", ["Create"] = "إضافة", ["Edit"] = "تعديل", ["Delete"] = "حذف",
            ["Print"] = "طباعة", ["Export"] = "تصدير", ["Post"] = "ترحيل", ["Unpost"] = "إلغاء ترحيل",
            ["Confirm"] = "اعتماد", ["Discount"] = "خصم", ["ChangePrice"] = "تغيير السعر",
            ["Adjust"] = "تسوية", ["Transfer"] = "تحويل", ["NegativeStock"] = "سالب المخزون",
            ["StockIn"] = "إذن إضافة", ["StockOut"] = "إذن صرف", ["PaySalary"] = "صرف رواتب",
            ["System"] = "إعدادات نظام", ["Backup"] = "نسخ احتياطي", ["Restore"] = "استعادة",
            ["ClosePeriod"] = "إقفال فترة", ["ReopenPeriod"] = "إعادة فتح فترة",
            ["CloseYear"] = "إقفال سنة", ["ReopenYear"] = "إعادة فتح سنة",
            ["ManageRoles"] = "إدارة الأدوار",
        };

        /// <summary>عقدة لكل موديول، وتحتها عقدة لكل مفتاح. عقدة الموديول ليست مفتاحاً بذاتها (IsCheckable=false)
        /// لكنها تنشر حالتها لأبنائها عبر AppTreeView.ApplyState.</summary>
        public static List<TreeNodeViewModel> Build()
        {
            var groups = PermissionKeys.All()
                .Distinct()
                .GroupBy(k => k.Split('.')[0])
                .OrderBy(g => g.Key);

            var roots = new List<TreeNodeViewModel>();

            foreach (var group in groups)
            {
                var moduleNode = new TreeNodeViewModel
                {
                    Id = group.Key,
                    Name = ModuleName(group.Key),
                    DisplayText = ModuleName(group.Key),
                    IsCheckable = false,
                    IsExpanded = false
                };

                foreach (var key in group.OrderBy(k => k))
                {
                    moduleNode.AddChild(new TreeNodeViewModel
                    {
                        Id = key,
                        Name = ActionName(key),
                        DisplayText = ActionName(key),
                        IsLeaf = true,
                        Data = key
                    });
                }

                roots.Add(moduleNode);
            }

            return roots;
        }

        /// <summary>كل عقد المفاتيح (الأوراق) في الشجرة — العقد الجذرية تجميعية بلا مفتاح.</summary>
        public static IEnumerable<TreeNodeViewModel> KeyNodes(IEnumerable<TreeNodeViewModel> roots) =>
            roots.SelectMany(r => r.Children);

        private static string ModuleName(string module) =>
            ModuleNames.TryGetValue(module, out var name) ? name : module;

        private static string ActionName(string key)
        {
            var parts = key.Split('.');
            if (parts.Length >= 3 && parts[1] == "Column")
                return "عمود: " + (ActionNames.TryGetValue(parts[2], out var col) ? col : parts[2]);

            var action = parts.Length > 1 ? parts[1] : key;
            return ActionNames.TryGetValue(action, out var name) ? name : action;
        }
    }
}
