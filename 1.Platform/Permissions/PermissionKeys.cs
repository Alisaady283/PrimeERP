using System.Collections.Generic;
using System.Reflection;

namespace PrimeERP.Platform.Permissions
{
    /// <summary>كل مفتاح صلاحية في النظام — كلاس ثابت متداخل لكل وحدة، بلا سلاسل نصية مبعثرة في الكود.</summary>
    public static class PermissionKeys
    {
        public static class Accounts
        {
            public const string View          = "Accounts.View";
            public const string Create        = "Accounts.Create";
            public const string Edit          = "Accounts.Edit";
            public const string Delete        = "Accounts.Delete";
            public const string Export        = "Accounts.Export";
            public const string Print         = "Accounts.Print";
            public const string ColumnBalance = "Accounts.Column.Balance";
            public const string ColumnLevel   = "Accounts.Column.Level";
            public const string ColumnType    = "Accounts.Column.Type";
        }

        public static class Journal
        {
            public const string View         = "Journal.View";
            public const string Create       = "Journal.Create";
            public const string Edit         = "Journal.Edit";
            public const string Delete       = "Journal.Delete";
            public const string Post         = "Journal.Post";
            public const string Unpost       = "Journal.Unpost";
            public const string Export       = "Journal.Export";
            public const string Print        = "Journal.Print";
            public const string ColumnSource = "Journal.Column.Source";
        }

        public static class Customers
        {
            public const string View   = "Customers.View";
            public const string Create = "Customers.Create";
            public const string Edit   = "Customers.Edit";
            public const string Delete = "Customers.Delete";
            public const string Export = "Customers.Export";
            public const string Print  = "Customers.Print";
            public const string ColumnCreditLimit = "Customers.Column.CreditLimit";
        }

        public static class Suppliers
        {
            public const string View   = "Suppliers.View";
            public const string Create = "Suppliers.Create";
            public const string Edit   = "Suppliers.Edit";
            public const string Delete = "Suppliers.Delete";
            public const string Export = "Suppliers.Export";
            public const string Print  = "Suppliers.Print";
        }

        public static class Products
        {
            public const string View   = "Products.View";
            public const string Create = "Products.Create";
            public const string Edit   = "Products.Edit";
            public const string Delete = "Products.Delete";
            public const string Export = "Products.Export";
            public const string Print  = "Products.Print";
            public const string ColumnCostPrice = "Products.Column.CostPrice";
        }

        // النمط 2 القائم على ModuleKey (فئات/ماركات/وحدات/مخازن/فئات أصول/أقسام/وظائف) — نفس الأربعة
        // مفاتيح لكل وحدة، لا Export/Print (قوائم بحتة بلا طباعة/تصدير حتى الآن).
        public static class Categories
        {
            public const string View = "Categories.View", Create = "Categories.Create", Edit = "Categories.Edit", Delete = "Categories.Delete";
        }

        public static class Brands
        {
            public const string View = "Brands.View", Create = "Brands.Create", Edit = "Brands.Edit", Delete = "Brands.Delete";
        }

        public static class Units
        {
            public const string View = "Units.View", Create = "Units.Create", Edit = "Units.Edit", Delete = "Units.Delete";
        }

        public static class Warehouses
        {
            public const string View = "Warehouses.View", Create = "Warehouses.Create", Edit = "Warehouses.Edit", Delete = "Warehouses.Delete";
        }

        public static class Treasuries
        {
            public const string View = "Treasuries.View", Create = "Treasuries.Create", Edit = "Treasuries.Edit", Delete = "Treasuries.Delete";
        }

        public static class Receipts
        {
            public const string View = "Receipts.View", Create = "Receipts.Create", Edit = "Receipts.Edit", Delete = "Receipts.Delete", Print = "Receipts.Print";
        }

        public static class Payments
        {
            public const string View = "Payments.View", Create = "Payments.Create", Edit = "Payments.Edit", Delete = "Payments.Delete", Print = "Payments.Print";
        }

        public static class Cheques
        {
            public const string View = "Cheques.View", Create = "Cheques.Create", Edit = "Cheques.Edit", Delete = "Cheques.Delete", Print = "Cheques.Print";
        }

        public static class AssetCategories
        {
            public const string View = "AssetCategories.View", Create = "AssetCategories.Create", Edit = "AssetCategories.Edit", Delete = "AssetCategories.Delete";
        }

        public static class Departments
        {
            public const string View = "Departments.View", Create = "Departments.Create", Edit = "Departments.Edit", Delete = "Departments.Delete";
        }

        public static class JobTitles
        {
            public const string View = "JobTitles.View", Create = "JobTitles.Create", Edit = "JobTitles.Edit", Delete = "JobTitles.Delete";
        }

        public static class Assets
        {
            public const string View   = "Assets.View";
            public const string Create = "Assets.Create";
            public const string Edit   = "Assets.Edit";
            public const string Delete = "Assets.Delete";
            public const string Export = "Assets.Export";
            public const string Print  = "Assets.Print";
        }

        public static class Inventory
        {
            public const string View     = "Inventory.View";
            public const string StockIn  = "Inventory.StockIn";
            public const string StockOut = "Inventory.StockOut";
            public const string Transfer = "Inventory.Transfer";
            public const string Export   = "Inventory.Export";
            public const string Print    = "Inventory.Print";
            // CrudViewModelBase العامة (Add/Edit/Delete على شبكة StockIn/StockOut/Transfer) تحتاج هذه الثلاثة
            // بنفس التسمية القياسية — منفصلة عن StockIn/StockOut أعلاه (تلك لأوامر برمجية محدَّدة لاحقاً).
            public const string Create   = "Inventory.Create";
            public const string Edit     = "Inventory.Edit";
            public const string Delete   = "Inventory.Delete";
            public const string Adjust        = "Inventory.Adjust";
            public const string NegativeStock = "Inventory.NegativeStock";
        }

        public static class Sales
        {
            public const string View    = "Sales.View";
            public const string Create  = "Sales.Create";
            public const string Edit    = "Sales.Edit";
            public const string Delete  = "Sales.Delete";
            public const string Confirm = "Sales.Confirm";
            public const string Post    = "Sales.Post";
            public const string Unpost  = "Sales.Unpost";
            public const string Discount    = "Sales.Discount";
            public const string ChangePrice = "Sales.ChangePrice";
            public const string Export  = "Sales.Export";
            public const string Print   = "Sales.Print";
        }

        public static class Purchases
        {
            public const string View    = "Purchases.View";
            public const string Create  = "Purchases.Create";
            public const string Edit    = "Purchases.Edit";
            public const string Delete  = "Purchases.Delete";
            public const string Confirm = "Purchases.Confirm";
            public const string Post    = "Purchases.Post";
            public const string Unpost  = "Purchases.Unpost";
            public const string Export  = "Purchases.Export";
            public const string Print   = "Purchases.Print";
        }

        public static class HR
        {
            public const string View      = "HR.View";
            public const string Create    = "HR.Create";
            public const string Edit      = "HR.Edit";
            public const string Delete    = "HR.Delete";
            public const string PaySalary = "HR.PaySalary";
            public const string Export    = "HR.Export";
            public const string Print     = "HR.Print";
            public const string ColumnSalary = "HR.Column.Salary";
        }

        public static class Reports
        {
            public const string View   = "Reports.View";
            public const string Export = "Reports.Export";
            public const string Print  = "Reports.Print";
        }

        public static class Settings
        {
            public const string View    = "Settings.View";
            public const string Edit    = "Settings.Edit";
            /// <summary>تعديل إعداد IsSystem=true — أعلى من Edit العادية.</summary>
            public const string System  = "Settings.System";
            public const string Backup  = "Settings.Backup";
            public const string Restore = "Settings.Restore";

            public const string ClosePeriod  = "Settings.ClosePeriod";
            public const string ReopenPeriod = "Settings.ReopenPeriod";
            public const string CloseYear    = "Settings.CloseYear";
            /// <summary>الأخطر في النظام — تحذف قيد الإقفال وتعيد فتح سنة كاملة. صلاحية منفصلة ومقيَّدة عمداً.</summary>
            public const string ReopenYear   = "Settings.ReopenYear";
        }

        public static class Users
        {
            public const string View        = "Users.View";
            public const string Create      = "Users.Create";
            public const string Edit        = "Users.Edit";
            public const string Delete      = "Users.Delete";
            public const string ManageRoles = "Users.ManageRoles";
        }

        /// <summary>يجمع كل مفاتيح الصلاحيات المعرّفة عبر كل الوحدات — يُستخدم لزرع جدول Permissions تلقائياً.</summary>
        /// <summary>
        /// مفاتيح الوحدات المبنيّة — تُسجَّل عند الإقلاع فتظهر في شجرة الصلاحيات مع المكتوبة بلا تمييز.
        /// شجرة الشاشة تُبنى من All() آلياً، فلا تعديل في شاشة الصلاحيات نفسها.
        /// </summary>
        private static readonly List<string> Built = new();

        /// <summary>يُستدعى مرّة عند الإقلاع لكل وحدة مبنيّة: View/Create/Edit/Delete.</summary>
        public static void RegisterBuilt(string moduleKey)
        {
            foreach (var action in new[] { "View", "Create", "Edit", "Delete" })
            {
                var key = $"{moduleKey}.{action}";
                if (!Built.Contains(key)) Built.Add(key);
            }
        }

        public static List<string> All()
        {
            var keys = new List<string>(Built);

            foreach (var nested in typeof(PermissionKeys).GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
            {
                foreach (var field in nested.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                {
                    if (field.FieldType == typeof(string) && field.IsLiteral)
                        keys.Add((string)field.GetRawConstantValue());
                }
            }

            return keys;
        }
    }
}
