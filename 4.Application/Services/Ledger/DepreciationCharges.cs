using System;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>أقساط الإهلاك وأثرها</summary>
    public sealed class DepreciationCharges
    {
        private readonly IAssetRepository _assets;
        private readonly IAssetDepreciationRepository _charges;
        private readonly Entries _journals;

        public DepreciationCharges(IAssetRepository assets, IAssetDepreciationRepository charges, Entries journals) =>
            (_assets, _charges, _journals) = (assets, charges, journals);

        /// <summary>كل قسطٍ مستحقّ</summary>
        public int Run(PrimeDbContext db, DateTime upTo, string expenseAccount, string source, Func<Asset, DateTime, string> note)
        {
            var created = 0;
            foreach (var asset in _assets.Depreciable(db))
            {
                var schedule = AssetCalc.Schedule(
                    AssetCalc.Basis(asset.RevaluedValue, asset.PurchaseCost), asset.SalvageValue, asset.UsefulLifeYears, asset.AccumulatedDepreciation,
                    AssetCalc.FirstUndepreciatedMonth(asset.LastDepreciationDate, asset.PurchaseDate), upTo).ToList();

                foreach (var (period, amount) in schedule)
                {
                    Post(db, new AssetDepreciation
                    {
                        AssetId = asset.Id, PeriodDate = period, Amount = amount,
                        Notes = note(asset, period)
                    }, asset, expenseAccount, source);
                    created++;
                }
            }
            return created;
        }

        /// <summary>القسط بقيده</summary>
        public void Post(PrimeDbContext db, AssetDepreciation charge, Asset asset, string expenseAccount, string source)
        {
            charge.Id = _charges.Insert(charge, db);

            var (debit, credit) = (expenseAccount, asset.DepreciationAccountCode);
            charge.JournalEntryId = Posting.Entry(_journals, db, charge.PeriodDate, charge.Notes, source, debit, credit, charge.Amount, asset.Name);

            _charges.SetJournalEntryId(db, charge.Id, charge.JournalEntryId.Value);
            Recalculate(db, charge.AssetId);
        }

        /// <summary>المُهلَك والقيمة من أقساطه</summary>
        public void Recalculate(PrimeDbContext db, int assetId)
        {
            var asset = _assets.GetById(assetId, db);
            if (asset == null) return;

            (asset.AccumulatedDepreciation, asset.LastDepreciationDate) = _charges.TotalOf(assetId, db);
            asset.CurrentValue = AssetCalc.CurrentValue(asset.RevaluedValue, asset.PurchaseCost, asset.AccumulatedDepreciation);

            _assets.Update(asset, db);
        }
    }
}
