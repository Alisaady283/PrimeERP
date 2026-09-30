using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>ميزان المراجعة لفترة</summary>
    public sealed class TrialBalance
    {
        private readonly IAccountRepository _accounts;
        private readonly IJournalRepository _journal;

        public TrialBalance(IAccountRepository accounts, IJournalRepository journal)
        {
            _accounts = accounts;
            _journal = journal;
        }

        public Result<List<TrialBalanceLine>> Of(DateTime from, DateTime to, bool includeZero = false, bool postedOnly = true)
        {
            var leaves = _accounts.Find(null, null, null, leafOnly: true, includeInactive: false);
            var names = _accounts.GetAll(includeInactive: true).ToDictionary(a => a.Code, a => a.Name);

            var openingSums = _journal.GetAccountSums(null, from.AddDays(-1), postedOnly).ToDictionary(x => x.AccountCode);
            var periodSums  = _journal.GetAccountSums(from, to, postedOnly).ToDictionary(x => x.AccountCode);

            var result = new List<TrialBalanceLine>();
            foreach (var account in leaves)
            {
                openingSums.TryGetValue(account.Code, out var opening);
                periodSums.TryGetValue(account.Code, out var period);

                var amounts = StatementCalc.Trial(opening.SumDebit, opening.SumCredit, period.SumDebit, period.SumCredit);
                if (!includeZero && amounts.IsZero) continue;

                var line = Rows.Copy(amounts, Rows.Copy(account, new TrialBalanceLine()));
                line.ParentName = account.ParentCode != null && names.TryGetValue(account.ParentCode, out var parentName)
                                  ? parentName : account.ParentCode;
                result.Add(line);
            }

            var (debit, credit) = (result.Sum(l => l.ClosingDebit), result.Sum(l => l.ClosingCredit));
            return debit == credit
                ? Result.Ok(result)
                : Result.Fail<List<TrialBalanceLine>>($"{LocalizationService.Get("Str.Journal.TrialBalanceMismatch")} ({debit - credit:N2})",
                    ErrorCode.Unexpected);
        }
    }
}
