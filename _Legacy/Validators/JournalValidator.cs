// ═══════════════════════════════════════════════════════
// LEGACY — لا تستخدمه في كود جديد
// البديل: Core/Validation/Validators/JournalValidator.cs
// يُحذف في: المرحلة G (التنظيف)
// ═══════════════════════════════════════════════════════
using System.Linq;
using PrimeERP.Core;
using PrimeERP.Models;

namespace PrimeERP.Validators
{
    public static class JournalValidator
    {
        public static ValidationResult Validate(JournalEntry entry)
        {
            var result = new ValidationResult();

            Validator.Required(result, "EntryDate", entry.EntryDate, "التاريخ");

            var validLines = entry.Lines.Where(l => !string.IsNullOrEmpty(l.AccountCode)).ToList();

            if (validLines.Count < 2)
                result.AddError("Lines", "القيد يحتاج سطرين على الأقل");

            decimal totalDebit  = validLines.Sum(l => l.Debit);
            decimal totalCredit = validLines.Sum(l => l.Credit);

            if (totalDebit != totalCredit)
                result.AddError("Balance",
                    $"القيد غير متوازن — مدين: {totalDebit:N2}  دائن: {totalCredit:N2}");

            if (totalDebit == 0 && totalCredit == 0)
                result.AddError("Balance", "لا يمكن حفظ قيد بقيمة صفر");

            return result;
        }
    }
}
