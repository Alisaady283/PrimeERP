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
        public const string Source = "AssetDepreciation";

        private readonly IAssetRepository _assets;
        private readonly IAssetDepreciationRepository _charges;
        private readonly Entries _journals;
        private readonly IAssetRevaluationRepository _revaluations;

        public DepreciationCharges(IAssetRepository assets, IAssetDepreciationRepository charges, Entries journals,
            IAssetRevaluationRepository revaluations) =>
            (_assets, _charges, _journals, _revaluations) = (assets, charges, journals, revaluations);

        /// <summary>كل قسطٍ مستحقّ</summary>
        public int Run(PrimeDbContext db, DateTime upTo, string expenseAccount, string source, Func<Asset, DateTime, string> note)
        {
            return _assets.Depreciable(db).Sum(asset => RunAsset(db, asset, upTo, expenseAccount, source, note));
        }

        private int RunAsset(PrimeDbContext db, Asset asset, DateTime upTo, string expenseAccount, string source, Func<Asset, DateTime, string> note)
        {
            var schedule = AssetCalc.Schedule(AssetCalc.TermsOf(asset, _revaluations.ForAsset(asset.Id, db)), asset.AccumulatedDepreciation,
                AssetCalc.FirstUndepreciatedMonth(asset.LastDepreciationDate, asset.PurchaseDate), upTo).ToList();

            foreach (var (period, amount) in schedule)
                Post(db, new AssetDepreciation { AssetId = asset.Id, PeriodDate = period, Amount = amount, Notes = note(asset, period) },
                    asset, expenseAccount, source);
            return schedule.Count;
        }

        /// <summary>القسط بقيده</summary>
        public bool DueBefore(PrimeDbContext db, Asset asset, DateTime date) =>
            AssetCalc.Schedule(AssetCalc.TermsOf(asset, _revaluations.ForAsset(asset.Id, db)), asset.AccumulatedDepreciation,
                AssetCalc.FirstUndepreciatedMonth(asset.LastDepreciationDate, asset.PurchaseDate),
                AssetCalc.EndOfMonth(date.AddMonths(-1))).Any();

        public void Post(PrimeDbContext db, AssetDepreciation charge, Asset asset, string expenseAccount, string source)
        {
            charge.Id = _charges.Insert(charge, db);

            var (debit, credit) = (expenseAccount, asset.DepreciationAccountCode);
            charge.JournalEntryId = Posting.Entry(_journals, db, charge.PeriodDate, charge.Notes, source, debit, credit, charge.Amount, asset.Name);

            _charges.SetJournalEntryId(db, charge.Id, charge.JournalEntryId.Value);
            Recalculate(db, charge.AssetId);
        }

        public void Recalculate(PrimeDbContext db, int assetId)
        {
            var asset = _assets.GetById(assetId, db);
            if (asset == null) return;

            asset.RevaluedValue = asset.PurchaseCost + _revaluations.ForAsset(assetId, db).Sum(AssetCalc.Difference);
            (asset.AccumulatedDepreciation, asset.LastDepreciationDate) = _charges.TotalOf(assetId, db);
            asset.CurrentValue = AssetCalc.CurrentValue(asset.RevaluedValue, asset.PurchaseCost, asset.AccumulatedDepreciation);

            _assets.Update(asset, db);
        }
    }
}
