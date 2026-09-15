using PrimeERP.Domain.Contracts;
using System;
using System.Text.RegularExpressions;

namespace PrimeERP.Domain.Rules
{
    /// <summary>قواعد تحقق محاسبية جاهزة — قطعة واحدة يعاد استخدامها في التحقق من الحسابات والقيود والفواتير والمخزون.</summary>
    public static class AccountingRules
    {
        /// <summary>
        /// كود الحساب: أرقام فقط، وطولُه بسعة عموده في الجدول (30). كان مقيَّداً بـ12 وهو رقمٌ ثالث لا
        /// يخالف العمود فقط بل يمنع العمق: عرض اللاحقة = مستوى الأب، فالمستوى السادس يحتاج 16 خانة.
        /// القاعدة تتبع التخزين، فلا حدٌّ ثالث يُنسى عند تعميق الشجرة.
        /// </summary>
        public static void AccountCodeFormat(ValidationResult result, string field, string code,
                                             string label = "كود الحساب")
        {
            if (string.IsNullOrWhiteSpace(code) || !Regex.IsMatch(code, @"^\d{1,30}$"))
                result.AddError(field, $"{label} يجب أن يتكوّن من أرقام فقط (1-12 خانة)");
        }

        /// <summary>القيد لا يُحفظ إلا متوازناً: مجموع المدين = مجموع الدائن.</summary>
        public static void BalancedEntry(ValidationResult result, decimal totalDebit, decimal totalCredit,
                                         string field = "Balance")
        {
            if (totalDebit != totalCredit)
                result.AddError(field, $"القيد غير متوازن — مدين: {totalDebit:N2}  دائن: {totalCredit:N2}");
        }

        /// <summary>قيد اليومية يحتاج سطرين على الأقل (طرفين) ليكون قيداً حقيقياً.</summary>
        public static void MinimumJournalLines(ValidationResult result, string field, int lineCount, int minimum = 2)
        {
            if (lineCount < minimum)
                result.AddError(field, $"القيد يحتاج {minimum} سطور على الأقل");
        }

        /// <summary>لا يُحفظ قيد بلا أي قيمة (مدين ودائن كلاهما صفر).</summary>
        public static void NonZeroEntry(ValidationResult result, string field, decimal totalDebit, decimal totalCredit)
        {
            if (totalDebit == 0 && totalCredit == 0)
                result.AddError(field, "لا يمكن حفظ قيد بقيمة صفر");
        }

        /// <summary>السطر الواحد يكون مديناً أو دائناً، لا الاثنين معاً.</summary>
        public static void NoLineWithBothDebitAndCredit(ValidationResult result, string field, decimal debit, decimal credit, string accountCode = null)
        {
            if (debit > 0 && credit > 0)
                result.AddError(field, $"لا يمكن إدخال مدين ودائن في نفس السطر{(accountCode != null ? $" ({accountCode})" : "")}");
        }

        /// <summary>الحساب لا يقبل قيود إلا لو كان فرعياً (IsLeaf) — حساب له أبناء ممنوع تسجيل قيود عليه مباشرة.</summary>
        public static void LeafAccountOnly(ValidationResult result, string field, string accountCode,
                                           bool isLeaf, string accountName = null)
        {
            if (!isLeaf)
                result.AddError(field, $"الحساب {accountName ?? accountCode} لا يقبل قيود مباشرة — اختر حساباً فرعياً");
        }

        /// <summary>لا تسجيل في فترة مالية مقفلة.</summary>
        public static void OpenPeriod(ValidationResult result, string field, bool isPeriodOpen, DateTime documentDate)
        {
            if (!isPeriodOpen)
                result.AddError(field, $"الفترة المالية بتاريخ {documentDate:yyyy-MM-dd} مقفلة — لا يمكن التسجيل فيها");
        }

        /// <summary>
        /// يمنع أن يصبح رصيد المخزون سالباً. allowNegativeStock تُقرأ من ISettingsService في المستدعي (الخدمة) —
        /// دالة نقية هنا، بلا وصول لقاعدة بيانات، تماماً كباقي AccountingRules.
        /// </summary>
        public static void NoNegativeStock(ValidationResult result, string field, decimal currentStock,
                                           decimal requestedQuantity, bool allowNegativeStock, string productName = null)
        {
            if (allowNegativeStock) return;

            if (currentStock - requestedQuantity < 0)
                result.AddError(field,
                    $"الكمية غير متوفرة{(productName != null ? $" لـ {productName}" : "")} — " +
                    $"المتاح: {currentStock:N2}، المطلوب: {requestedQuantity:N2}");
        }
    }
}
