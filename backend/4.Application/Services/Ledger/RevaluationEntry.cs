using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>قيد إعادة تقييم الأصل</summary>
    public static class RevaluationEntry
    {
        public static List<CreateJournalLineDto> Lines(AssetRevaluation revaluation, string asset,
            (string Account, bool Debit) counter, string note)
        {
            var (debit, credit) = TwoSided.By(!counter.Debit, asset, counter.Account);
            var amount = Math.Abs(AssetCalc.Difference(revaluation));
            return new JournalLines()
                .Debit(debit, amount, note)
                .Credit(credit, amount, note)
                .ToList();
        }
    }
}
