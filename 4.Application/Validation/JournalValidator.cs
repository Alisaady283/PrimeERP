using PrimeERP.Data.Core;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Rules;

namespace PrimeERP.Application.Validation
{
    /// <summary>تحقّق القيد وسطوره</summary>
    public class JournalValidator : IValidator<JournalEntry>
    {
        private readonly IAccountRepository _accounts;
        private readonly bool _allowDuplicateAccount;
        private readonly PrimeDbContext _db;

        /// <summary>شكل القيد وحده</summary>
        public JournalValidator() { }

        /// <summary>الشكل وحسابات السطور</summary>
        public JournalValidator(IAccountRepository accounts, bool allowDuplicateAccount = false,
                                PrimeDbContext db = null)
        {
            _accounts = accounts;
            _allowDuplicateAccount = allowDuplicateAccount;
            _db = db;
        }

        /// <summary>أسماء حسابات السطور بعد التحقق</summary>
        public Dictionary<string, string> AccountNames { get; } = new();

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

                    if (_accounts != null) CheckAccounts(r, validLines);
                })
                .Validate(entry);

        /// <summary>حساب السطر ورقيٌّ نشط</summary>
        private void CheckAccounts(ValidationResult result, List<JournalLine> lines)
        {
            var codes = lines.Select(l => l.AccountCode).Distinct().ToList();
            var found = _accounts.GetByCodes(codes, _db).ToDictionary(a => a.Code);

            foreach (var code in codes)
            {
                if (!found.TryGetValue(code, out var account))
                {
                    result.AddError("AccountCode", $"الحساب غير موجود: {code}");
                    continue;
                }

                if (!account.IsLeaf) result.AddError("AccountCode", $"الحساب تجميعي لا يقبل قيوداً: {code}");
                else if (!account.IsActive) result.AddError("AccountCode", $"الحساب موقوف: {code}");
                else AccountNames[code] = account.Name;
            }

            if (_allowDuplicateAccount) return;

            var duplicate = lines.GroupBy(l => l.AccountCode).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null) result.AddError("AccountCode", $"الحساب مكرَّر في القيد: {duplicate.Key}");
        }
    }
}
