using System.Collections.Generic;

namespace PrimeERP.Platform.Settings
{
    /// <summary>كل مفتاح إعداد في النظام</summary>
    public static class SettingKeys
    {
        public static class Company
        {
            public const string Name             = "Company.Name";
            public const string NameEn            = "Company.NameEn";
            public const string TaxNumber         = "Company.TaxNumber";

            public const string StartDate         = "Company.StartDate";
            public const string CommercialRegNo   = "Company.CommercialRegNo";
            public const string Address           = "Company.Address";
            public const string Phone             = "Company.Phone";
            public const string Email             = "Company.Email";
            public const string LogoPath          = "Company.LogoPath";

            public const string LogoData          = "Company.LogoData";
        }

        public static class Financial
        {
            public const string CurrencyName     = "Financial.CurrencyName";
            public const string CurrencySubUnit  = "Financial.CurrencySubUnit";

            public const string BaseCurrencyId       = "Financial.BaseCurrencyId";
            public const string DecimalPlaces        = "Financial.DecimalPlaces";
            public const string FiscalYearStartMonth = "Financial.FiscalYearStartMonth";
            public const string AllowNegativeStock   = "Financial.AllowNegativeStock";
            public const string DefaultCostMethod    = "Financial.DefaultCostMethod";
            public const string RoundingMethod       = "Financial.RoundingMethod";

            public const string RequireFiscalPeriod  = "Financial.RequireFiscalPeriod";

            public const string AllowDuplicateAccountInEntry = "Financial.AllowDuplicateAccountInEntry";

            public const string WarnOnDuplicateCustomerName = "Financial.WarnOnDuplicateCustomerName";

            public const string WarnOnDuplicatePhone = "Financial.WarnOnDuplicatePhone";
        }

        /// <summary>أكواد الحسابات</summary>
        public static class Accounts
        {
            public const string Customers        = "Accounts.Customers";
            public const string Suppliers        = "Accounts.Suppliers";
            public const string Inventory        = "Accounts.Inventory";

            public const string OpeningAdjustments = "Accounts.OpeningAdjustments";
            public const string DepreciationExpense     = "Accounts.DepreciationExpense";
            public const string AccumulatedDepreciation = "Accounts.AccumulatedDepreciation";

            public const string FixedAssets   = "Accounts.FixedAssets";

            public const string CapitalGains  = "Accounts.CapitalGains";

            public const string CapitalLosses = "Accounts.CapitalLosses";
            public const string FinanceIncome = "Accounts.FinanceIncome";
            public const string ChequesUnderCollection = "Accounts.ChequesUnderCollection";
            public const string ChequesPayable = "Accounts.ChequesPayable";
            public const string FinanceExpense = "Accounts.FinanceExpense";
            public const string Cash             = "Accounts.Cash";

            public const string EmployeeAdvances = "Accounts.EmployeeAdvances";

            public static readonly string[] LinkedRoots = { Customers, Suppliers, Cash, Bank, FixedAssets, AccumulatedDepreciation, EmployeeAdvances };
            public const string Bank             = "Accounts.Bank";
            public const string Sales            = "Accounts.Sales";
            public const string SalesReturns     = "Accounts.SalesReturns";
            public const string COGS             = "Accounts.COGS";

            public const string SalaryExpense      = "Accounts.SalaryExpense";
            public const string AllowanceExpense   = "Accounts.AllowanceExpense";
            public const string SalariesPayable    = "Accounts.SalariesPayable";
            public const string InsurancePayable   = "Accounts.InsurancePayable";
            public const string TaxPayable         = "Accounts.TaxPayable";
            public const string RetainedEarnings = "Accounts.RetainedEarnings";
            public const string VATInput         = "Accounts.VATInput";
            public const string VATOutput        = "Accounts.VATOutput";

            public const string WithholdingReceivable = "Accounts.WithholdingReceivable";
            public const string WithholdingPayable    = "Accounts.WithholdingPayable";


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

            public const string SimplifiedFlow        = "Documents.SimplifiedFlow";

            public const string CustomerPrefix = "Documents.CustomerPrefix";
            public const string SupplierPrefix = "Documents.SupplierPrefix";
            public const string ProductPrefix  = "Documents.ProductPrefix";
        }

        public static class UI
        {
            public const string Language          = "UI.Language";
            public const string UseArabicNumerals = "UI.UseArabicNumerals";
            public const string DateFormat        = "UI.DateFormat";
            public const string PageSize          = "UI.PageSize";
            public const string SidebarCollapsed  = "UI.SidebarCollapsed";

            public const string Manifest          = "UI.Manifest";


        }

        public static class Backup
        {
            public const string AutoBackupEnabled       = "Backup.AutoBackupEnabled";
            public const string AutoBackupPath          = "Backup.AutoBackupPath";
            public const string AutoBackupIntervalHours = "Backup.AutoBackupIntervalHours";
            public const string RetentionCount          = "Backup.RetentionCount";
        }

        public static class License
        {
            public const string Serial   = "License.Serial";
            public const string Customer = "License.Customer";
        }

        public static class Developer
        {
            public const string ServerUrl  = "Developer.ServerUrl";
            public const string AdminToken = "Developer.AdminToken";
        }

        public static class HR
        {
            public const string WorkStart       = "HR.WorkStart";
            public const string WorkEnd         = "HR.WorkEnd";
            public const string DailyWageDays   = "HR.DailyWageDays";
            public const string OvertimeRate    = "HR.OvertimeRate";
            public const string OvertimeMinimum = "HR.OvertimeMinimum";
            public const string LateRate        = "HR.LateRate";
            public const string LateMinimum     = "HR.LateMinimum";
            public const string PayrollStartDay = "HR.PayrollStartDay";
            public const string PayrollTreasury = "HR.PayrollTreasury";
        }

        public static class Edition
        {
            public const string AdvancesPosted = "System.AdvancesPosted";
            public const string PayrollPays    = "System.PayrollPays";
            public const string Version        = "System.Version";
            public const string Arabic         = "System.Arabic";
            public const string English        = "System.English";
        }

        public static class Security
        {
            public const string PasswordMinLength        = "Security.PasswordMinLength";
            public const string SessionTimeoutMinutes    = "Security.SessionTimeoutMinutes";
            public const string RequirePasswordChange    = "Security.RequirePasswordChange";
        }

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

            new(Accounts.Customers,        "1202",     "string", "Accounts", IsSystem: true),
            new(Accounts.Suppliers,        "2201",     "string", "Accounts", IsSystem: true),
            new(Accounts.Inventory,        "1201001",  "string", "Accounts", IsSystem: true),
            new(Accounts.OpeningAdjustments, "33",      "string", "Accounts", IsSystem: true),
            new(Accounts.DepreciationExpense, "54",     "string", "Accounts", IsSystem: true),
            new(Accounts.AccumulatedDepreciation, "1101002", "string", "Accounts", IsSystem: true),
            new(Accounts.FixedAssets,       "1101001",  "string", "Accounts", IsSystem: true),
            new(Accounts.CapitalGains,       "4201",     "string", "Accounts", IsSystem: true),
            new(Accounts.CapitalLosses,      "5601",     "string", "Accounts", IsSystem: true),
            new(Accounts.FinanceIncome,      "4202",     "string", "Accounts", IsSystem: true),
            new(Accounts.ChequesUnderCollection, "1203004", "string", "Accounts", IsSystem: true),
            new(Accounts.ChequesPayable,     "",         "string", "Accounts", IsSystem: true),
            new(Accounts.FinanceExpense,     "55",       "string", "Accounts", IsSystem: true),
            new(Accounts.Cash,               "1203007",  "string", "Accounts", IsSystem: true),
            new(Accounts.EmployeeAdvances,   "1206001",  "string", "Accounts", IsSystem: true),
            new(Accounts.SalaryExpense,      "53",       "string", "Accounts", IsSystem: true),
            new(Accounts.AllowanceExpense,   "53",       "string", "Accounts", IsSystem: true),
            new(Accounts.SalariesPayable,    "2202004",  "string", "Accounts", IsSystem: true),
            new(Accounts.InsurancePayable,   "2202003",  "string", "Accounts", IsSystem: true),
            new(Accounts.TaxPayable,         "2203003",  "string", "Accounts", IsSystem: true),
            new(Accounts.Bank,               "1203003",  "string", "Accounts", IsSystem: true),
            new(Accounts.Sales,              "4101",     "string", "Accounts", IsSystem: true),
            new(Accounts.SalesReturns,       "4102",     "string", "Accounts", IsSystem: true),
            new(Accounts.COGS,               "5101",     "string", "Accounts", IsSystem: true),
            new(Accounts.RetainedEarnings, "32",   "string", "Accounts", IsSystem: true),
            new(Accounts.VATInput,         "1207001", "string", "Accounts", IsSystem: true),
            new(Accounts.VATOutput,        "2203001", "string", "Accounts", IsSystem: true),
            new(Accounts.WithholdingReceivable, "1207002", "string", "Accounts", IsSystem: true),
            new(Accounts.WithholdingPayable,    "2203002", "string", "Accounts", IsSystem: true),
            new(Accounts.AutoLinkEnabled,  "true", "bool",   "Accounts"),

            new(Print.ChequeOffsetX, "0",  "string", "Print"),
            new(Print.ChequeOffsetY, "0",  "string", "Print"),
            new(Print.CopyLabels,    "",   "string", "Print"),
            new(Print.LinesPerPage,  "0",  "int",    "Print"),
            new(Print.Terms,         "",   "string", "Print"),
            new(Documents.JournalPrefix,         "JE",   "string", "Documents"),
            new(Documents.SalesInvoicePrefix,    "INV",  "string", "Documents"),
            new(Documents.PurchaseInvoicePrefix, "PI",   "string", "Documents"),
            new(Documents.SimplifiedFlow,        "true",  "bool",  "Documents"),
            new(Documents.CustomerPrefix,        "C",    "string", "Documents"),
            new(Documents.SupplierPrefix,        "S",    "string", "Documents"),
            new(Documents.ProductPrefix,         "P",    "string", "Documents"),

            new(UI.Language,         "Ar",          "string", "UI"),
            new(UI.UseArabicNumerals,"false",       "bool",   "UI"),
            new(UI.DateFormat,       "yyyy-MM-dd",  "string", "UI"),
            new(UI.PageSize,         "25",          "int",    "UI"),
            new(UI.SidebarCollapsed, "false",       "bool",   "UI"),

            new(Backup.AutoBackupEnabled,       "false", "bool",   "Backup"),
            new(Backup.AutoBackupPath,          "",      "string", "Backup"),
            new(Backup.AutoBackupIntervalHours, "24",    "int",    "Backup"),
            new(Backup.RetentionCount,          "10",    "int",    "Backup"),

            new(License.Serial,   "", "string", "License", IsSystem: true),
            new(License.Customer, "", "string", "License", IsSystem: true),

            new(Developer.ServerUrl,  "https://primelogic-eg.com/erp", "string", "Developer", IsSystem: true),
            new(Developer.AdminToken, "",                              "string", "Developer", IsSystem: true),

            new(Security.PasswordMinLength,     "8",     "int",  "Security", IsSystem: true),
            new(Security.SessionTimeoutMinutes, "60",    "int",  "Security"),
            new(Security.RequirePasswordChange, "false", "bool", "Security"),

            new(Edition.AdvancesPosted, "true", "bool", "System"),
            new(Edition.PayrollPays,    "true", "bool", "System"),
            new(Edition.Version,        "",     "string", "System", IsSystem: true),
            new(Edition.Arabic,         "true", "bool",   "System"),
            new(Edition.English,        "true", "bool",   "System"),

            new(HR.WorkStart,       "09:00", "string",  "HR"),
            new(HR.WorkEnd,         "17:00", "string",  "HR"),
            new(HR.DailyWageDays,   "30",    "int",     "HR"),
            new(HR.OvertimeRate,    "1.5",   "decimal", "HR"),
            new(HR.OvertimeMinimum, "01:00", "string",  "HR"),
            new(HR.LateRate,        "1",     "decimal", "HR"),
            new(HR.LateMinimum,     "00:15", "string",  "HR"),
            new(HR.PayrollStartDay, "1",     "int",     "HR"),
            new(HR.PayrollTreasury, "",      "treasury", "HR"),
        };
    }
}
