using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrimeERP.Core;
using PrimeERP.Models;
using PrimeERP.Views.Controls.Pickers;

namespace PrimeERP.Views.Dev
{
    /// <summary>مصادر بيانات وهمية لكل Picker — تُستخدم في الـ Gallery فقط لإثبات أن PickerBase&lt;T&gt; لا يستدعي DB إطلاقاً.</summary>
    public static class MockPickerDataSources
    {
        public static IPickerDataSource<Account>  Accounts  { get; } = new MockAccountDataSource();
        public static IPickerDataSource<Customer> Customers { get; } = new MockCustomerDataSource();
        public static IPickerDataSource<Supplier> Suppliers { get; } = new MockSupplierDataSource();
        public static IPickerDataSource<Product>  Products  { get; } = new MockProductDataSource();
        public static IPickerDataSource<Employee> Employees { get; } = new MockEmployeeDataSource();
    }

    // ===================== Accounts (20، شجرة حقيقية) =====================

    internal class MockAccountDataSource : IPickerDataSource<Account>
    {
        private readonly List<Account> _accounts = Build();

        public Task<IEnumerable<Account>> SearchAsync(string term, int maxResults)
        {
            IEnumerable<Account> query = _accounts;
            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(a => Has(a.Name, term) || Has(a.Code, term));
            return Task.FromResult(query.Take(maxResults));
        }

        public Task<Account> GetByIdAsync(int id) => Task.FromResult(_accounts.FirstOrDefault(a => a.Id == id));
        public Task<Account> GetByCodeAsync(string code) => Task.FromResult(_accounts.FirstOrDefault(a => a.Code == code));

        public PickerDisplayConfig GetDisplayConfig() => new()
        {
            CodeField = nameof(Account.Code),
            NameField = nameof(Account.Name),
            ExtraInfoTemplate = "الرصيد: {Balance:N2}"
        };

        private static bool Has(string source, string term) =>
            !string.IsNullOrEmpty(source) && source.Contains(term, StringComparison.OrdinalIgnoreCase);

        private static Account A(int id, string code, string name, string parent, bool isLeaf, int type, decimal balance) => new()
        {
            Id = id, Code = code, Name = name, ParentCode = parent, IsLeaf = isLeaf, Type = type, Balance = balance, IsActive = true
        };

        private static List<Account> Build() => new()
        {
            A(1,  "1000", "أصول",                     null,   false, 1, 0),
            A(2,  "1100", "أصول متداولة",             "1000", false, 1, 0),
            A(3,  "1110", "البنوك",                    "1100", true,  1, 128900.00m),
            A(4,  "1120", "الصناديق",                  "1100", true,  1, 6200.75m),
            A(5,  "1130", "ذمم مدينة (العملاء)",       "1100", true,  1, 45200.50m),
            A(6,  "1200", "أصول ثابتة",                "1000", false, 1, 0),
            A(7,  "1210", "أراضٍ ومبانٍ",               "1200", true,  1, 950000.00m),
            A(8,  "1220", "سيارات ومعدات",             "1200", true,  1, 210000.00m),
            A(9,  "2000", "خصوم",                      null,   false, 2, 0),
            A(10, "2100", "خصوم متداولة",              "2000", false, 2, 0),
            A(11, "2110", "ذمم دائنة (الموردون)",      "2100", true,  2, 31000.00m),
            A(12, "2120", "مصروفات مستحقة",            "2100", true,  2, 8400.00m),
            A(13, "2200", "خصوم طويلة الأجل",          "2000", false, 2, 0),
            A(14, "2210", "قروض طويلة الأجل",          "2200", true,  2, 300000.00m),
            A(15, "3000", "حقوق الملكية",              null,   false, 3, 0),
            A(16, "3100", "رأس المال",                 "3000", true,  3, 500000.00m),
            A(17, "4000", "إيرادات",                   null,   false, 4, 0),
            A(18, "4100", "إيرادات المبيعات",          "4000", true,  4, 210500.00m),
            A(19, "5000", "مصروفات",                   null,   false, 5, 0),
            A(20, "5100", "مصروفات تشغيلية",           "5000", true,  5, 18750.25m),
        };
    }

    // ===================== Customers (15) =====================

    internal class MockCustomerDataSource : IPickerDataSource<Customer>
    {
        private readonly List<Customer> _customers = Build();

        public Task<IEnumerable<Customer>> SearchAsync(string term, int maxResults)
        {
            IEnumerable<Customer> query = _customers;
            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(c => Has(c.Name, term) || Has(c.Code, term) || Has(c.Phone, term));
            return Task.FromResult(query.Take(maxResults));
        }

        public Task<Customer> GetByIdAsync(int id) => Task.FromResult(_customers.FirstOrDefault(c => c.Id == id));
        public Task<Customer> GetByCodeAsync(string code) => Task.FromResult(_customers.FirstOrDefault(c => c.Code == code));

        public PickerDisplayConfig GetDisplayConfig() => new()
        {
            CodeField = nameof(Customer.Code),
            NameField = nameof(Customer.Name),
            ExtraInfoTemplate = "الرصيد: {Balance:N2}",
            Columns = new List<PickerColumn>
            {
                new() { Header = "الكود",         Field = nameof(Customer.Code),        Width = 80 },
                new() { Header = "الاسم",         Field = nameof(Customer.Name),        Width = 190 },
                new() { Header = "التليفون",      Field = nameof(Customer.Phone),       Width = 110 },
                new() { Header = "الرصيد",        Field = nameof(Customer.Balance),     Width = 100, Format = "N2" },
                new() { Header = "حد الائتمان",   Field = nameof(Customer.CreditLimit), Width = 110, Format = "N2" },
            }
        };

        private static bool Has(string source, string term) =>
            !string.IsNullOrEmpty(source) && source.Contains(term, StringComparison.OrdinalIgnoreCase);

        private static Customer C(int id, string code, string name, string phone, decimal balance, decimal creditLimit) => new()
        {
            Id = id, Code = code, Name = name, Phone = phone, Balance = balance, CreditLimit = creditLimit, IsActive = true
        };

        private static List<Customer> Build() => new()
        {
            C(1,  "C001", "شركة النور للتجارة",       "01012345678", 12500,  20000),
            C(2,  "C002", "مؤسسة الأمل",               "01098765432", 34200,  30000), // متجاوز
            C(3,  "C003", "أحمد محمود",                "01123456789", 0,      5000),
            C(4,  "C004", "شركة السلام للمقاولات",     "01234567890", 8900,   15000),
            C(5,  "C005", "محلات الفجر",                "01512345678", 21000,  20000), // متجاوز
            C(6,  "C006", "مصنع النيل للبلاستيك",       "01098123456", 55000,  100000),
            C(7,  "C007", "سارة عبد الرحمن",            "01234988765", 1200,   3000),
            C(8,  "C008", "شركة الدلتا للاستيراد",     "01111222333", 78000,  75000), // متجاوز
            C(9,  "C009", "مؤسسة الرجاء التجارية",     "01555666777", 4300,   10000),
            C(10, "C010", "كريم السيد",                 "01066677788", 0,      2000),
            C(11, "C011", "شركة الشروق الصناعية",       "01288899900", 19800,  25000),
            C(12, "C012", "محمد إبراهيم",               "01399988877", 600,    5000),
            C(13, "C013", "مجموعة الوادي التجارية",     "01477788899", 42000,  40000), // متجاوز
            C(14, "C014", "منى حسن",                    "01566655544", 2100,   6000),
            C(15, "C015", "شركة المستقبل للتوريدات",   "01633322211", 15600,  20000),
        };
    }

    // ===================== Suppliers (10) =====================

    internal class MockSupplierDataSource : IPickerDataSource<Supplier>
    {
        private readonly List<Supplier> _suppliers = Build();

        public Task<IEnumerable<Supplier>> SearchAsync(string term, int maxResults)
        {
            IEnumerable<Supplier> query = _suppliers;
            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(s => Has(s.Name, term) || Has(s.Code, term) || Has(s.Phone, term));
            return Task.FromResult(query.Take(maxResults));
        }

        public Task<Supplier> GetByIdAsync(int id) => Task.FromResult(_suppliers.FirstOrDefault(s => s.Id == id));
        public Task<Supplier> GetByCodeAsync(string code) => Task.FromResult(_suppliers.FirstOrDefault(s => s.Code == code));

        public PickerDisplayConfig GetDisplayConfig() => new()
        {
            CodeField = nameof(Supplier.Code),
            NameField = nameof(Supplier.Name),
            ExtraInfoTemplate = "الرصيد: {Balance:N2}",
            Columns = new List<PickerColumn>
            {
                new() { Header = "الكود",    Field = nameof(Supplier.Code),    Width = 80 },
                new() { Header = "الاسم",    Field = nameof(Supplier.Name),    Width = 200 },
                new() { Header = "التليفون", Field = nameof(Supplier.Phone),   Width = 120 },
                new() { Header = "الرصيد",   Field = nameof(Supplier.Balance), Width = 110, Format = "N2" },
            }
        };

        private static bool Has(string source, string term) =>
            !string.IsNullOrEmpty(source) && source.Contains(term, StringComparison.OrdinalIgnoreCase);

        private static Supplier S(int id, string code, string name, string phone, decimal balance) => new()
        {
            Id = id, Code = code, Name = name, Phone = phone, Balance = balance, IsActive = true
        };

        private static List<Supplier> Build() => new()
        {
            S(1, "S001", "مصنع الأهرام للأدوات",     "01011122233", 15400),
            S(2, "S002", "شركة النصر للاستيراد",      "01022233344", 8900),
            S(3, "S003", "مؤسسة الفا للتوريدات",      "01033344455", 22000),
            S(4, "S004", "شركة بيتا الصناعية",        "01044455566", 5600),
            S(5, "S005", "مصنع جاما للبلاستيك",       "01055566677", 31000),
            S(6, "S006", "شركة دلتا للمعادن",         "01066677788", 0),
            S(7, "S007", "مؤسسة سيجما التجارية",      "01077788899", 12300),
            S(8, "S008", "شركة أوميجا للاستيراد",     "01088899900", 4200),
            S(9, "S009", "مصنع زيتا للأخشاب",         "01099900011", 9800),
            S(10,"S010", "شركة إبسيلون للتغليف",      "01100011122", 17600),
        };
    }

    // ===================== Products (20) =====================

    internal class MockProductDataSource : IPickerDataSource<Product>
    {
        private readonly List<Product> _products = Build();

        public Task<IEnumerable<Product>> SearchAsync(string term, int maxResults)
        {
            IEnumerable<Product> query = _products;
            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(p => Has(p.Name, term) || Has(p.Code, term) || Has(p.Barcode, term));
            return Task.FromResult(query.Take(maxResults));
        }

        public Task<Product> GetByIdAsync(int id) => Task.FromResult(_products.FirstOrDefault(p => p.Id == id));
        public Task<Product> GetByCodeAsync(string code) => Task.FromResult(_products.FirstOrDefault(p => p.Code == code));

        public PickerDisplayConfig GetDisplayConfig() => new()
        {
            CodeField = nameof(Product.Code),
            NameField = nameof(Product.Name),
            ExtraInfoTemplate = "الرصيد: {CurrentStock:N2} — السعر: {SalePrice:N2}",
            Columns = new List<PickerColumn>
            {
                new() { Header = "الكود",    Field = nameof(Product.Code),         Width = 80 },
                new() { Header = "الباركود", Field = nameof(Product.Barcode),      Width = 110 },
                new() { Header = "الاسم",    Field = nameof(Product.Name),         Width = 180 },
                new() { Header = "الرصيد",   Field = nameof(Product.CurrentStock), Width = 90, Format = "N2" },
                new() { Header = "السعر",    Field = nameof(Product.SalePrice),    Width = 90, Format = "N2" },
            }
        };

        private static bool Has(string source, string term) =>
            !string.IsNullOrEmpty(source) && source.Contains(term, StringComparison.OrdinalIgnoreCase);

        private static Product P(int id, string code, string barcode, string name, decimal salePrice, decimal stock,
            string unitName = "قطعة", decimal taxRate = 14) => new()
        {
            Id = id, Code = code, Barcode = barcode, Name = name, SalePrice = salePrice, CostPrice = salePrice * 0.7m,
            CurrentStock = stock, UnitName = unitName, TaxRate = taxRate, IsActive = true
        };

        private static List<Product> Build() => new()
        {
            P(1,  "P001", "622001", "لابتوب Dell Inspiron",      22000, 5),
            P(2,  "P002", "622002", "شاشة سامسونج 24 بوصة",      4200,  12),
            P(3,  "P003", "622003", "طابعة HP LaserJet",         5600,  0), // نفد
            P(4,  "P004", "622004", "ماوس لاسلكي",                 250, 40),
            P(5,  "P005", "622005", "كيبورد ميكانيكي",            850,  18),
            P(6,  "P006", "622006", "سماعة بلوتوث",               600,  0), // نفد
            P(7,  "P007", "622007", "هارد خارجي 1TB",            1800, 25),
            P(8,  "P008", "622008", "فلاشة USB 64GB",              180, 60, "علبة"),
            P(9,  "P009", "622009", "راوتر واي فاي",              950,  10),
            P(10, "P010", "622010", "كابل HDMI",                    90, 100, "متر"),
            P(11, "P011", "622011", "حقيبة لابتوب",                 320, 15),
            P(12, "P012", "622012", "شاحن سريع 65W",               480,  0), // نفد
            P(13, "P013", "622013", "كاميرا ويب HD",               700,  8),
            P(14, "P014", "622014", "مايك احترافي",                1100, 6),
            P(15, "P015", "622015", "ستاند لابتوب",                 260, 22),
            P(16, "P016", "622016", "لوحة وصل USB-C",              340, 30),
            P(17, "P017", "622017", "بطارية محمولة 20000mAh",      550, 14),
            P(18, "P018", "622018", "سماعة رأس سلكية",             300, 20),
            P(19, "P019", "622019", "ماوس باد كبير",                90, 50, "قطعة", 0),
            P(20, "P020", "622020", "منظم كابلات",                  70, 35),
        };
    }

    // ===================== Employees (10) =====================

    internal class MockEmployeeDataSource : IPickerDataSource<Employee>
    {
        private readonly List<Employee> _employees = Build();

        public Task<IEnumerable<Employee>> SearchAsync(string term, int maxResults)
        {
            IEnumerable<Employee> query = _employees;
            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(e => Has(e.Name, term) || Has(e.Code, term));
            return Task.FromResult(query.Take(maxResults));
        }

        public Task<Employee> GetByIdAsync(int id) => Task.FromResult(_employees.FirstOrDefault(e => e.Id == id));
        public Task<Employee> GetByCodeAsync(string code) => Task.FromResult(_employees.FirstOrDefault(e => e.Code == code));

        public PickerDisplayConfig GetDisplayConfig() => new()
        {
            CodeField = nameof(Employee.Code),
            NameField = nameof(Employee.Name),
            ExtraInfoTemplate = "{DepartmentName} — {JobTitleName}",
            Columns = new List<PickerColumn>
            {
                new() { Header = "الكود",    Field = nameof(Employee.Code),           Width = 80 },
                new() { Header = "الاسم",    Field = nameof(Employee.Name),           Width = 170 },
                new() { Header = "القسم",    Field = nameof(Employee.DepartmentName), Width = 130 },
                new() { Header = "الوظيفة",  Field = nameof(Employee.JobTitleName),   Width = 130 },
            }
        };

        private static bool Has(string source, string term) =>
            !string.IsNullOrEmpty(source) && source.Contains(term, StringComparison.OrdinalIgnoreCase);

        private static Employee E(int id, string code, string name, string dept, string job, EmployeeStatus status) => new()
        {
            Id = id, Code = code, Name = name, DepartmentName = dept, JobTitleName = job, Status = status,
            HireDate = DateTime.Today.AddYears(-2)
        };

        private static List<Employee> Build() => new()
        {
            E(1,  "E001", "محمد أحمد علي",       "المبيعات",     "مندوب مبيعات",  EmployeeStatus.Active),
            E(2,  "E002", "فاطمة السيد",          "المحاسبة",     "محاسب أول",     EmployeeStatus.Active),
            E(3,  "E003", "خالد إبراهيم",         "المخازن",      "أمين مخزن",     EmployeeStatus.Active),
            E(4,  "E004", "نورهان محمود",         "الموارد البشرية","أخصائي موارد بشرية", EmployeeStatus.Active),
            E(5,  "E005", "عمر حسن",              "المبيعات",     "مدير مبيعات",   EmployeeStatus.Active),
            E(6,  "E006", "ياسمين طارق",          "المحاسبة",     "محاسب",         EmployeeStatus.OnLeave),
            E(7,  "E007", "أحمد فتحي",            "الصيانة",      "فني صيانة",     EmployeeStatus.Active),
            E(8,  "E008", "منة الله سعيد",        "خدمة العملاء", "ممثل خدمة عملاء", EmployeeStatus.Active),
            E(9,  "E009", "طارق عبد العزيز",      "المبيعات",     "مندوب مبيعات",  EmployeeStatus.Inactive),
            E(10, "E010", "رنا وليد",             "الإدارة",      "مساعد إداري",   EmployeeStatus.Active),
        };
    }
}
