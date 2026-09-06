using System.Collections.Generic;

namespace PrimeERP.Platform.Settings
{
    /// <summary>كل مفتاح إعداد في النظام — كلاس ثابت متداخل، بلا نصوص حرة في الكود. أكواد الحسابات الخاصة هنا فقط (Accounts.*) — لا تُكتب حرفياً في أي خدمة أخرى.</summary>
    public static class SettingKeys
    {
        public static class Company
        {
            public const string Name             = "Company.Name";
            public const string NameEn            = "Company.NameEn";
            public const string TaxNumber         = "Company.TaxNumber";

            /// <summary>تاريخ بدء العمل بالنظام — منه تُؤرَّخ الأرصدة الافتتاحية، فلا تُكتب بتاريخ حديث
            /// يجعلها حركةَ فترة بدل رصيد ما قبلها.</summary>
            public const string StartDate         = "Company.StartDate";
            public const string CommercialRegNo   = "Company.CommercialRegNo";
            public const string Address           = "Company.Address";
            public const string Phone             = "Company.Phone";
            public const string Email             = "Company.Email";
            public const string LogoPath          = "Company.LogoPath";

            /// <summary>الشعار نفسه Base64 داخل قاعدة البيانات لا مساراً على القرص — ينجو مع النسخة الاحتياطية
            /// وينتقل مع النظام لجهاز آخر، بخلاف ملف خارجي يضيع بصمت وقت الطباعة.</summary>
            public const string LogoData          = "Company.LogoData";
        }

        public static class Financial
        {
            /// <summary>اسم العملة ووحدتها الفرعية كما يُكتبان في التفقيط على المستندات.</summary>
            public const string CurrencyName     = "Financial.CurrencyName";
            public const string CurrencySubUnit  = "Financial.CurrencySubUnit";

            public const string BaseCurrencyId       = "Financial.BaseCurrencyId";
            public const string DecimalPlaces        = "Financial.DecimalPlaces";
            public const string FiscalYearStartMonth = "Financial.FiscalYearStartMonth";
            public const string AllowNegativeStock   = "Financial.AllowNegativeStock";
            public const string DefaultCostMethod    = "Financial.DefaultCostMethod";
            public const string RoundingMethod       = "Financial.RoundingMethod";

            /// <summary>false (الافتراضي): تاريخ بلا فترة مالية مُعرَّفة = مفتوح (يسمح بتشغيل النظام قبل إعداد السنوات). true: يمنع التسجيل بلا فترة معرَّفة.</summary>
            public const string RequireFiscalPeriod  = "Financial.RequireFiscalPeriod";

            /// <summary>false (الافتراضي): يمنع تكرار نفس الحساب في أكثر من سطر بقيد اليومية الواحد. true: يسمح به.</summary>
            public const string AllowDuplicateAccountInEntry = "Financial.AllowDuplicateAccountInEntry";

            /// <summary>true (الافتراضي): تحذير لا منع — عميل جديد باسم مطابق لعميل قائم.</summary>
            public const string WarnOnDuplicateCustomerName = "Financial.WarnOnDuplicateCustomerName";

            /// <summary>true (الافتراضي): تحذير لا منع — عميل جديد برقم هاتف مطابق لعميل قائم.</summary>
            public const string WarnOnDuplicatePhone = "Financial.WarnOnDuplicatePhone";
        }

        /// <summary>أكواد الحسابات الخاصة — تُقرأ من هنا فقط عبر ISettingsService، لا تُكتب حرفياً في أي خدمة. كل عميل شجرة مختلفة.</summary>
        public static class Accounts
        {
            public const string Customers        = "Accounts.Customers";
            public const string Suppliers        = "Accounts.Suppliers";
            public const string Inventory        = "Accounts.Inventory";
            public const string DepreciationExpense     = "Accounts.DepreciationExpense";
            public const string AccumulatedDepreciation = "Accounts.AccumulatedDepreciation";
            public const string Cash             = "Accounts.Cash";

            /// <summary>مفاتيح الأصول المرتبطة بكيانات: قيمتها كود حساب <b>تجميعي</b> يعيش أبناؤه ككيانات
            /// (عميل/مورد/خزينة/بنك). ضبطها على حساب ورقي يُعطّل الربط بصمت — لذلك تُرفَض عند الحفظ.</summary>
            public static readonly string[] LinkedRoots = { Customers, Suppliers, Cash, Bank };
            public const string Bank             = "Accounts.Bank";
            public const string Sales            = "Accounts.Sales";
            public const string SalesReturns     = "Accounts.SalesReturns";
            public const string COGS             = "Accounts.COGS";
            public const string Salaries         = "Accounts.Salaries";
            public const string RetainedEarnings = "Accounts.RetainedEarnings";
            public const string VATInput         = "Accounts.VATInput";
            public const string VATOutput        = "Accounts.VATOutput";

            /// <summary>ضريبة الخصم والإضافة: ما يُحجَز من مستحقّاتنا أصلٌ لدى المصلحة، وما نحجزه من الموردين التزام علينا.</summary>
            public const string WithholdingReceivable = "Accounts.WithholdingReceivable";
            public const string WithholdingPayable    = "Accounts.WithholdingPayable";

            /// <summary>حسابات دورة الشيكات — بالمحفظة (وارد لم يودَع)، تحت التحصيل (مودع بالبنك)، وشيكات الدفع (صادر).</summary>

            /// <summary>false يعطّل الربط التلقائي (حساب↔عميل/مورد) كلياً وبصمت — true (الافتراضي) يفرض نجاح الربط أو Fail صريح، لا سكوت.</summary>
            public const string AutoLinkEnabled  = "Accounts.AutoLinkEnabled";
        }

        public static class Print
        {
            public const string ChequeOffsetX = "Print.ChequeOffsetX";
            public const string ChequeOffsetY = "Print.ChequeOffsetY";
            public const string CopyLabels    = "Print.CopyLabels";
            public const string LinesPerPage  = "Print.LinesPerPage";
            public const string Terms         = "Print.Terms";
        }

        public static class Documents
        {
            public const string JournalPrefix         = "Documents.JournalPrefix";
            public const string SalesInvoicePrefix    = "Documents.SalesInvoicePrefix";
            public const string PurchaseInvoicePrefix = "Documents.PurchaseInvoicePrefix";
            public const string StockVoucherPrefix    = "Documents.StockVoucherPrefix";
            public const string NumberPadding         = "Documents.NumberPadding";
            public const string ResetNumbersYearly    = "Documents.ResetNumbersYearly";

            /// <summary>مفعّل: الفاتورة تمسّ المخزون ومستندات الدورة مخفية. معطّل: الدورة الكاملة والإذن يمسّ المخزون.</summary>
            public const string SimplifiedFlow        = "Documents.SimplifiedFlow";

            /// <summary>
            /// البادئة الفعلية لتسلسل NumberSequenceService بالمفتاح "Customer" — يزرعها NumberSequenceSeeder، لا
            /// EnsureRow التلقائية (التي كانت ستجعل البادئة "Customer" نفسها). بلا شرطة لاحقة — NumberSequenceService.
            /// Format يضيف "-" فاصلة بنفسه دائماً (نفس اصطلاح JournalPrefix="JE" الحالي)؛ قيمة بشرطة هنا كانت
            /// ستنتج "C--2026-00001" (شرطة مزدوجة) بدل "C-2026-00001".
            /// </summary>
            public const string CustomerPrefix = "Documents.CustomerPrefix";
            public const string SupplierPrefix = "Documents.SupplierPrefix";
            public const string ProductPrefix  = "Documents.ProductPrefix";
        }

        public static class UI
        {
            public const string Theme             = "UI.Theme";
            public const string Language          = "UI.Language";
            public const string UseArabicNumerals = "UI.UseArabicNumerals";
            public const string DateFormat        = "UI.DateFormat";
            public const string PageSize          = "UI.PageSize";
            public const string SidebarCollapsed  = "UI.SidebarCollapsed";

            /// <summary>مفتاح حزمة الهوية الحالية (اسم مجلد تحت Resources/Design/Identity — "Default"/"Corporate") — يقرأه IIdentityService.Initialize عند الإقلاع.</summary>
            public const string Identity          = "UI.Identity";

            public const string IdentityBaseline  = "UI.IdentityBaseline";
        }

        public static class Backup
        {
            public const string AutoBackupEnabled       = "Backup.AutoBackupEnabled";
            public const string AutoBackupPath          = "Backup.AutoBackupPath";
            public const string AutoBackupIntervalHours = "Backup.AutoBackupIntervalHours";
            public const string RetentionCount          = "Backup.RetentionCount";
        }

        public static class Security
        {
            public const string PasswordMinLength        = "Security.PasswordMinLength";
            public const string SessionTimeoutMinutes    = "Security.SessionTimeoutMinutes";
            public const string RequirePasswordChange    = "Security.RequirePasswordChange";
        }

        /// <summary>تعريف كامل لمفتاح إعداد — قيمته الافتراضية، نوعه، فئته — يستخدمه SettingSeeder لزرع الجدول.</summary>
        public record Definition(string Key, string DefaultValue, string DataType, string Category, bool IsSystem = false);

        public static List<Definition> All() => new()
        {
            new(Company.Name,           "شركتي",         "string", "Company"),
            new(Company.NameEn,         "My Company",    "string", "Company"),
            new(Company.TaxNumber,      "",               "string", "Company"),
            new(Company.StartDate,      "2026-01-01",     "date",   "Company"),
            new(Company.CommercialRegNo,"",               "string", "Company"),
            new(Company.Address,        "",               "string", "Company"),
            new(Company.Phone,          "",               "string", "Company"),
            new(Company.Email,          "",               "string", "Company"),
            new(Company.LogoPath,       "",               "string", "Company"),
            new(Company.LogoData,       "",               "string", "Company"),

            new(Financial.CurrencyName,    "جنيه", "string", "Financial"),
            new(Financial.CurrencySubUnit, "قرش",  "string", "Financial"),
            new(Financial.BaseCurrencyId,       "1",               "int",    "Financial", IsSystem: true),
            new(Financial.DecimalPlaces,        "2",               "int",    "Financial"),
            new(Financial.FiscalYearStartMonth, "1",               "int",    "Financial", IsSystem: true),
            new(Financial.AllowNegativeStock,   "false",           "bool",   "Financial"),
            new(Financial.DefaultCostMethod,    "WeightedAverage", "string", "Financial"),
            new(Financial.RoundingMethod,       "Nearest",         "string", "Financial"),
            new(Financial.RequireFiscalPeriod,  "false",           "bool",   "Financial"),
            new(Financial.AllowDuplicateAccountInEntry, "false",   "bool",   "Financial"),
            new(Financial.WarnOnDuplicateCustomerName, "true",     "bool",   "Financial"),
            new(Financial.WarnOnDuplicatePhone,        "true",     "bool",   "Financial"),

            new(Accounts.Customers,        "1202", "string", "Accounts", IsSystem: true),
            new(Accounts.Suppliers,        "2101", "string", "Accounts", IsSystem: true),
            new(Accounts.Inventory,        "1201", "string", "Accounts", IsSystem: true),
            new(Accounts.DepreciationExpense,     "", "string", "Accounts", IsSystem: true),
            new(Accounts.AccumulatedDepreciation, "", "string", "Accounts", IsSystem: true),
            new(Accounts.Cash,             "1204", "string", "Accounts", IsSystem: true),
            new(Accounts.Bank,             "1203", "string", "Accounts", IsSystem: true),
            new(Accounts.Sales,            "41",   "string", "Accounts", IsSystem: true),
            new(Accounts.SalesReturns,     "42",   "string", "Accounts", IsSystem: true),
            new(Accounts.COGS,             "51",   "string", "Accounts", IsSystem: true),
            new(Accounts.Salaries,         "52",   "string", "Accounts", IsSystem: true),
            new(Accounts.RetainedEarnings, "32",   "string", "Accounts", IsSystem: true),
            new(Accounts.VATInput,         "",     "string", "Accounts", IsSystem: true),
            new(Accounts.VATOutput,        "",     "string", "Accounts", IsSystem: true),
            new(Accounts.WithholdingReceivable, "", "string", "Accounts", IsSystem: true),
            new(Accounts.WithholdingPayable,    "", "string", "Accounts", IsSystem: true),
            new(Accounts.AutoLinkEnabled,  "true", "bool",   "Accounts"),

            new(Print.ChequeOffsetX, "0",  "string", "Print"),
            new(Print.ChequeOffsetY, "0",  "string", "Print"),
            new(Print.CopyLabels,    "",   "string", "Print"),
            new(Print.LinesPerPage,  "0",  "int",    "Print"),
            new(Print.Terms,         "",   "string", "Print"),
            new(Documents.JournalPrefix,         "JE",   "string", "Documents"),
            new(Documents.SalesInvoicePrefix,    "INV",  "string", "Documents"),
            new(Documents.PurchaseInvoicePrefix, "PINV", "string", "Documents"),
            new(Documents.StockVoucherPrefix,    "SV",   "string", "Documents"),
            new(Documents.NumberPadding,         "5",    "int",    "Documents"),
            new(Documents.ResetNumbersYearly,    "true", "bool",   "Documents"),
            new(Documents.SimplifiedFlow,        "true",  "bool",  "Documents"),
            new(Documents.CustomerPrefix,        "C",    "string", "Documents"),
            new(Documents.SupplierPrefix,        "S",    "string", "Documents"),
            new(Documents.ProductPrefix,         "P",    "string", "Documents"),

            new(UI.Theme,            "Light",       "string", "UI"),
            new(UI.Language,         "Ar",          "string", "UI"),
            new(UI.UseArabicNumerals,"false",       "bool",   "UI"),
            new(UI.DateFormat,       "yyyy-MM-dd",  "string", "UI"),
            new(UI.PageSize,         "25",          "int",    "UI"),
            new(UI.SidebarCollapsed, "false",       "bool",   "UI"),
            new(UI.Identity,         "Signature",   "string", "UI"),
            new(UI.IdentityBaseline, "",            "string", "UI", true),

            new(Backup.AutoBackupEnabled,       "false", "bool",   "Backup"),
            new(Backup.AutoBackupPath,          "",      "string", "Backup"),
            new(Backup.AutoBackupIntervalHours, "24",    "int",    "Backup"),
            new(Backup.RetentionCount,          "10",    "int",    "Backup"),

            new(Security.PasswordMinLength,     "8",     "int",  "Security", IsSystem: true),
            new(Security.SessionTimeoutMinutes, "60",    "int",  "Security"),
            new(Security.RequirePasswordChange, "false", "bool", "Security"),
        };
    }
}
