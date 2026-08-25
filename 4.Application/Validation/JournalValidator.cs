using PrimeERP.Domain.Rules;
using PrimeERP.Domain.Contracts;
using System.Linq;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    /// <summary>
    /// ينفّذ IValidator&lt;JournalEntry&gt; — تحقق شكل البيانات فقط (تاريخ صالح) عبر RuleSet، وكل قاعدة أعمال
    /// مركّبة (توازن، حد أدنى للسطور، عدم الصفر) عبر AccountingRules (دوال نقية) داخل Custom — تعمل على
    /// مجموعة السطور كلها، لا تُختزل في فحص خاصية واحدة فتناسب RuleSet.Custom لا فحصاً مباشراً.
    /// </summary>
    public class JournalValidator : IValidator<JournalEntry>
    {
        public ValidationResult Validate(JournalEntry entry) =>
            Rules.For<JournalEntry>()
                .DateValid(x => x.EntryDate, "تاريخ القيد")
                .Custom((e, r) =>
                {
                    var validLines = e.Lines.Where(l => !string.IsNullOrEmpty(l.AccountCode)).ToList();
                    AccountingRules.MinimumJournalLines(r, "Lines", validLines.Count);

                    for (int i = 0; i < validLines.Count; i++)
                        AccountingRules.NoLineWithBothDebitAndCredit(r, $"Lines[{i}]", validLines[i].Debit, validLines[i].Credit, validLines[i].AccountCode);

                    decimal totalDebit  = validLines.Sum(l => l.Debit);
                    decimal totalCredit = validLines.Sum(l => l.Credit);

                    AccountingRules.BalancedEntry(r, totalDebit, totalCredit);
                    AccountingRules.NonZeroEntry(r, "Balance", totalDebit, totalCredit);
                })
                .Validate(entry);
    }
}
