using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Seeders
{
    /// <summary>يزرع شجرة الحسابات الأساسية عند خلوّها</summary>
    public static class AccountSeeder
    {
        public static void Seed(IAccountRepository accounts, bool english)
        {
            if (accounts.HasAny()) return;

            var defaults = new (string Code, string Name, string NameEn, string Parent, int Level, int Type, bool IsLeaf)[]
            {
                ("1", "أصول", "Assets", null, 1, 1, false),
                ("11", "أصول غير متداولة", "Non-current assets", "1", 2, 1, false),
                ("1101", "صافي الأصول الثابتة", "Net fixed assets", "11", 3, 1, false),
                ("1101001", "الأصول الثابتة", "Fixed assets", "1101", 4, 1, false),
                ("1101002", "مجمّع إهلاك الأصول الثابتة", "Accumulated depreciation of fixed assets", "1101", 4, 1, false),
                ("12", "أصول متداولة", "Current assets", "1", 2, 1, false),
                ("1201", "المخزون", "Inventory", "12", 3, 1, false),
                ("1201001", "مخزون البضائع", "Merchandise inventory", "1201", 4, 1, true),
                ("1202", "ذمم مدينة (العملاء)", "Accounts receivable (customers)", "12", 3, 1, false),
                ("1203", "النقدية وما في حكمها", "Cash and cash equivalents", "12", 3, 1, false),
                ("1203001", "بنوك — ودائع لأجل", "Banks — time deposits", "1203", 4, 1, true),
                ("1203003", "بنوك — حسابات جارية", "Banks — current accounts", "1203", 4, 1, false),
                ("1203004", "شيكات تحت التحصيل", "Cheques under collection", "1203", 4, 1, true),
                ("1203007", "نقدية بالصندوق", "Cash on hand", "1203", 4, 1, false),
                ("1206", "مدينون وأرصدة مدينة أخرى", "Other debtors and debit balances", "12", 3, 1, false),
                ("1206001", "سلف الموظفين", "Employee advances", "1206", 4, 1, false),
                ("1206002", "عهد الموظفين", "Employee custody", "1206", 4, 1, true),
                ("1206003", "مدينون متنوعون", "Sundry debtors", "1206", 4, 1, true),
                ("1206004", "إيرادات مستحقة", "Accrued revenue", "1206", 4, 1, true),
                ("1206005", "تأمينات لدى الغير", "Deposits with others", "1206", 4, 1, true),
                ("1207", "الأرصدة الضريبية المدينة", "Tax receivables", "12", 3, 1, false),
                ("1207001", "ضريبة القيمة المضافة المدينة", "VAT receivable", "1207", 4, 1, true),
                ("1207002", "ضريبة الخصم والإضافة المدينة", "Withholding tax receivable", "1207", 4, 1, true),
                ("2", "خصوم", "Liabilities", null, 1, 2, false),
                ("21", "خصوم غير متداولة", "Non-current liabilities", "2", 2, 2, false),
                ("2101", "قروض طويلة الأجل", "Long-term loans", "21", 3, 2, true),
                ("22", "خصوم متداولة", "Current liabilities", "2", 2, 2, false),
                ("2201", "ذمم دائنة (الموردون)", "Accounts payable (suppliers)", "22", 3, 2, false),
                ("2202", "دائنون وأرصدة دائنة أخرى", "Other creditors and credit balances", "22", 3, 2, false),
                ("2202001", "دائنون متنوعون", "Sundry creditors", "2202", 4, 2, true),
                ("2202002", "مصروفات مستحقة", "Accrued expenses", "2202", 4, 2, true),
                ("2202003", "تأمينات للغير", "Deposits from others", "2202", 4, 2, true),
                ("2202004", "أجور مستحقة", "Accrued wages", "2202", 4, 2, true),
                ("2203", "الضرائب المستحقة", "Taxes payable", "22", 3, 2, false),
                ("2203001", "ضريبة القيمة المضافة المستحقة", "VAT payable", "2203", 4, 2, true),
                ("2203002", "ضريبة الخصم والإضافة المستحقة", "Withholding tax payable", "2203", 4, 2, true),
                ("2203003", "ضريبة المرتبات المستحقة", "Payroll tax payable", "2203", 4, 2, true),
                ("2203004", "ضريبة الدخل المستحقة", "Income tax payable", "2203", 4, 2, true),
                ("3", "حقوق الملكية", "Equity", null, 1, 3, false),
                ("31", "رأس المال", "Capital", "3", 2, 3, false),
                ("3101", "رأس المال المدفوع", "Paid-in capital", "31", 3, 3, true),
                ("32", "الأرباح المحتجزة", "Retained earnings", "3", 2, 3, true),
                ("33", "حسابات افتتاحية وتسويات", "Opening and adjustment accounts", "3", 2, 3, true),
                ("4", "إيرادات", "Revenue", null, 1, 4, false),
                ("41", "إيرادات المبيعات", "Sales revenue", "4", 2, 4, false),
                ("4101", "مبيعات بضائع", "Sales of goods", "41", 3, 4, true),
                ("4102", "مرتجعات المبيعات", "Sales returns", "41", 3, 4, true),
                ("42", "إيرادات أخرى", "Other revenue", "4", 2, 4, false),
                ("4201", "أرباح رأسمالية", "Capital gains", "42", 3, 4, true),
                ("4202", "إيرادات تمويلية", "Finance income", "42", 3, 4, true),
                ("4203", "إيرادات أخرى", "Other income", "42", 3, 4, true),
                ("5", "مصروفات", "Expenses", null, 1, 5, false),
                ("51", "مصروفات تشغيلية", "Operating expenses", "5", 2, 5, false),
                ("5101", "تكلفة البضاعة المباعة", "Cost of goods sold", "51", 3, 5, true),
                ("52", "مصروفات بيعية وتسويقية", "Selling and marketing expenses", "5", 2, 5, true),
                ("53", "مصروفات إدارية عمومية", "General and administrative expenses", "5", 2, 5, false),
                ("5301", "الرواتب والأجور", "Salaries and wages", "53", 3, 5, true),
                ("5302", "بدلات الموظفين", "Employee allowances", "53", 3, 5, true),
                ("54", "مصروف إهلاك الأصول الثابتة", "Depreciation expense", "5", 2, 5, true),
                ("55", "مصروفات تمويلية", "Finance expenses", "5", 2, 5, true),
                ("56", "مصروفات أخرى", "Other expenses", "5", 2, 5, false),
                ("5601", "خسائر رأسمالية", "Capital losses", "56", 3, 5, true),
                ("57", "ضريبة الدخل", "Income tax", "5", 2, 5, true),
            };

            accounts.InsertMany(defaults.Select(account => new Account
            {
                Code = account.Code,
                Name = english ? account.NameEn : account.Name,
                ParentCode = account.Parent,
                Level = account.Level,
                Type = account.Type,
                IsLeaf = account.IsLeaf,
                IsActive = true
            }));
        }
    }
}
