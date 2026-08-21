using System.Linq;
using PrimeERP.Models;

namespace PrimeERP.Core.Validation.Validators
{
    /// <summary>
    /// ينفّذ IValidator&lt;JournalEntry&gt; — تحقق شكل البيانات فقط (تاريخ صالح)، وكل قاعدة أعمال مركّبة
    /// (توازن، حد أدنى للسطور، عدم الصفر) عبر AccountingRules (دوال نقية) لا محسوبة هنا مباشرة.
    /// </summary>
    public class JournalValidator : ValidatorBase, IValidator<JournalEntry>
    {
        public ValidationResult Validate(JournalEntry entry)
        {
            var result = new ValidationResult();

            DateValid(result, "EntryDate", entry.EntryDate, "تاريخ القيد");

            var validLines = entry.Lines.Where(l => !string.IsNullOrEmpty(l.AccountCode)).ToList();
            AccountingRules.MinimumJournalLines(result, "Lines", validLines.Count);

            for (int i = 0; i < validLines.Count; i++)
                AccountingRules.NoLineWithBothDebitAndCredit(result, $"Lines[{i}]", validLines[i].Debit, validLines[i].Credit, validLines[i].AccountCode);

            decimal totalDebit  = validLines.Sum(l => l.Debit);
            decimal totalCredit = validLines.Sum(l => l.Credit);

            AccountingRules.BalancedEntry(result, totalDebit, totalCredit);
            AccountingRules.NonZeroEntry(result, "Balance", totalDebit, totalCredit);

            return result;
        }
    }
}
