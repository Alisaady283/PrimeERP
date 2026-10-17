using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PrimeERP.Platform.Permissions
{
    /// <summary>كل مفتاح صلاحية في النظام</summary>
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

        public static class Manufacturing
        {
            public const string View = "Manufacturing.View", Create = "Manufacturing.Create", Edit = "Manufacturing.Edit", Delete = "Manufacturing.Delete";
        }

        public static class Pos
        {
            public const string View = "Pos.View", Open = "Pos.Open", Close = "Pos.Close", ManualReturn = "Pos.ManualReturn";
        }

        public static class Branches
        {
            public const string View = "Branches.View", Create = "Branches.Create", Edit = "Branches.Edit", Delete = "Branches.Delete";
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

        public static class AllowanceTypes
        {
            public const string View = "AllowanceTypes.View", Create = "AllowanceTypes.Create", Edit = "AllowanceTypes.Edit", Delete = "AllowanceTypes.Delete";
        }

        public static class DeductionTypes
        {
            public const string View = "DeductionTypes.View", Create = "DeductionTypes.Create", Edit = "DeductionTypes.Edit", Delete = "DeductionTypes.Delete";
        }

        public static class LeaveTypes
        {
            public const string View = "LeaveTypes.View", Create = "LeaveTypes.Create", Edit = "LeaveTypes.Edit", Delete = "LeaveTypes.Delete";
        }

        public static class HrLists
        {
            public const string View = "HrLists.View";
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

            public const string OpeningStock = "Inventory.OpeningStock";
            public const string StockOut = "Inventory.StockOut";
            public const string Scrap    = "Inventory.Scrap";

            public const string Transfer = "Inventory.Transfer";
            public const string Export   = "Inventory.Export";
            public const string Print    = "Inventory.Print";
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
            public const string System  = "Settings.System";
            public const string Backup  = "Settings.Backup";
            public const string Restore = "Settings.Restore";

            public const string ClosePeriod  = "Settings.ClosePeriod";
            public const string ReopenPeriod = "Settings.ReopenPeriod";
            public const string CloseYear    = "Settings.CloseYear";
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

        /// <summary>لوحات التحليل — صفحةٌ لكل لوحة في شجرة الصلاحيات، والمفتاح مفتاح الصفحة</summary>
        public static class Analytics
        {
            public const string Executive  = "ExecutiveAnalytics.View";
            public const string Sales      = "SalesAnalytics.View";
            public const string Purchases  = "PurchaseAnalytics.View";
            public const string Inventory  = "InventoryAnalytics.View";
            public const string Liquidity  = "LiquidityAnalytics.View";
            public const string People     = "PeopleAnalytics.View";
            public const string Production = "ProductionAnalytics.View";
        }

        /// <summary>مفتاحٌ انفصل عن آخر: يُمنح لكل دورٍ يحمل الأصل يوم يُضاف، فلا يفقد أحدٌ ما كان يراه</summary>
        public static readonly IReadOnlyDictionary<string, string> SplitFrom = new Dictionary<string, string>
        {
            [Analytics.Executive] = Reports.View, [Analytics.Sales] = Reports.View, [Analytics.Purchases] = Reports.View,
            [Analytics.Inventory] = Reports.View, [Analytics.Liquidity] = Reports.View, [Analytics.People] = Reports.View,
            [Analytics.Production] = Reports.View,
        };

        /// <summary>صفحة مفتاحٍ ليس لمجموعته صفحة: بالمفتاح ثم بالمجموعة</summary>
        public static readonly IReadOnlyDictionary<string, string> Home = new Dictionary<string, string>
        {
            ["Journal"] = "Journals", ["Sales"] = "SalesInvoices", ["Purchases"] = "PurchaseInvoices",
            ["Inventory"] = "StockIn", [Inventory.StockOut] = "StockOut", [Inventory.NegativeStock] = "StockOut",
            [Inventory.Transfer] = "StockTransfer", [Inventory.OpeningStock] = "OpeningBalances", [Inventory.Scrap] = "Scrap",
            ["HR"] = "Payroll", [HR.ColumnSalary] = "Employees", ["Manufacturing"] = "ProductionOrders",
            ["Departments"] = "HrLists", ["JobTitles"] = "HrLists", ["AllowanceTypes"] = "HrLists", ["DeductionTypes"] = "HrLists", ["LeaveTypes"] = "HrLists",
        };

        /// <summary>صفحة المفتاح في الشجرة: صفحته، وإلا صفحته المعلنة، وإلا أول صفحات مجاله</summary>
        public static string PageOfKey(string key)
        {
            var group = key.Split('.')[0];
            lock (Built)
            {
                if (Pages.ContainsKey(group)) return group;
                foreach (var home in new[] { Home.GetValueOrDefault(key), Home.GetValueOrDefault(group) })
                    if (home != null && Pages.ContainsKey(home)) return home;
                return Pages.Where(p => p.Value.Domain == group).OrderBy(p => p.Value.Order).Select(p => p.Key).FirstOrDefault() ?? group;
            }
        }

        private static readonly List<string> Built = new();
        private static readonly Dictionary<string, (string Domain, string Title, string TitleEn, int Order)> Pages = new();

        /// <summary>شاشات وحدة البناء</summary>
        public static readonly string[] BuilderScreens = { "BuilderSections", "BuilderModules", "BuilderColumns", "BuilderActions", "BuilderFilters", "BuilderExport" };

        /// <summary>أفعال الصفحة القياسية</summary>
        public static readonly string[] PageActions = { "View", "Create", "Edit", "Delete", "Print", "Export" };

        /// <summary>صفحةٌ بمفاتيحها: أفعال مجالها القياسية</summary>
        public static void RegisterPage(string page, string domain = null, string title = null, string titleEn = null, int order = int.MaxValue)
        {
            domain = string.IsNullOrWhiteSpace(domain) ? page : domain;
            lock (Built)
            {
                Pages[page] = (domain, title, titleEn, order);

                var actions = PageActions.Where(a => Constants.Value.Contains($"{domain}.{a}")).ToList();
                if (actions.Count == 0) actions = PageActions.Take(4).ToList();

                foreach (var key in actions.Select(a => $"{page}.{a}"))
                    if (!Built.Contains(key)) Built.Add(key);
            }
        }

        public static bool IsPage(string page) { lock (Built) return Pages.ContainsKey(page); }

        /// <summary>مفتاح بوّابة الصفحة: مفتاحها إن سُجّلت، وإلا مجالها</summary>
        public static string GateOf(string page, string domain) => IsPage(page) ? page : domain;

        /// <summary>مجال صفحةٍ مسجّلة غيرها</summary>
        public static string DomainOf(string page)
        {
            lock (Built) return Pages.TryGetValue(page, out var p) && p.Domain != page ? p.Domain : null;
        }

        public static (string Title, string TitleEn)? TitleOf(string page)
        {
            lock (Built) return Pages.TryGetValue(page, out var p) ? (p.Title, p.TitleEn) : null;
        }

        /// <summary>مجالٌ له صفحاتٌ بغير اسمه</summary>
        public static bool IsDomainOnly(string group)
        {
            lock (Built) return !Pages.ContainsKey(group) && Pages.Values.Any(p => p.Domain == group);
        }

        public static IReadOnlyList<string> PageKeys() { lock (Built) return Pages.Keys.ToList(); }

        /// <summary>ترتيب المجموعة كصفحة «الصفحات»: الصفحة بموضعها، والمجال بعد آخر صفحاته</summary>
        public static double OrderOf(string group)
        {
            lock (Built)
                return Pages.TryGetValue(group, out var page) ? page.Order
                    : Pages.Values.Where(p => p.Domain == group).Select(p => p.Order + 0.5).DefaultIfEmpty(double.MaxValue).Max();
        }

        /// <summary>أصل المفتاح الجديد: المنفصل عنه أو مقابله في المجال</summary>
        public static string OriginOf(string key) =>
            SplitFrom.TryGetValue(key, out var origin) ? origin : DomainKeyOf(key);

        /// <summary>مقابل مفتاح الصفحة في مجالها</summary>
        public static string DomainKeyOf(string key)
        {
            var separator = key.IndexOf('.');
            var domain = separator <= 0 ? null : DomainOf(key[..separator]);
            return domain == null ? null : domain + key[separator..];
        }

        /// <summary>ما يمنحه مفتاح الصفحة: مقابله في مجالٍ ليس صفحةً بنفسه، فلا تفتح صفحةٌ أختها</summary>
        public static string ImpliedKeyOf(string key)
        {
            var implied = DomainKeyOf(key);
            return implied == null || IsPage(implied[..implied.IndexOf('.')]) ? null : implied;
        }

        public static List<string> All()
        {
            lock (Built) return Built.Concat(Constants.Value).Distinct().ToList();
        }

        private static readonly System.Lazy<HashSet<string>> Constants = new(() =>
        {
            var keys = new HashSet<string>();
            foreach (var nested in typeof(PermissionKeys).GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
                foreach (var field in nested.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                    if (field.FieldType == typeof(string) && field.IsLiteral)
                        keys.Add((string)field.GetRawConstantValue());
            return keys;
        });
    }
}
