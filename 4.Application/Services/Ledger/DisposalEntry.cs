using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>قيد استبعاد الأصل</summary>
    public static class DisposalEntry
    {
        public static List<CreateJournalLineDto> Lines(AssetDisposal disposal, string cash, string depreciation, string asset,
            (string Account, bool Debit) counter, string note)
        {
            var (gainDebit, gainCredit) = TwoSided.By(counter.Debit, Math.Abs(AssetCalc.GainOrLoss(disposal)), 0m);
            return new JournalLines()
                .Debit(cash, disposal.SalePrice, note)
                .Debit(depreciation, disposal.AccumulatedDepreciation, note)
                .Credit(asset, disposal.AssetValue, note)
                .Add(counter.Account, gainDebit, gainCredit, note)
                .ToList();
        }
    }
}
