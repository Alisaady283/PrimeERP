using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Legacy.Admin;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Legacy.Treasury;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Assets
{
    /// <summary>بيع الأصل واستبعاده</summary>
    public interface IAssetDisposalService
    {
        Result<PagedResult<AssetDisposal>> GetPaged(int page, int pageSize, AssetDisposalFilter filter = null);
        Result<AssetDisposal> GetById(int id);
        Result<AssetDisposal> Create(AssetDisposal disposal);
        Result Update(AssetDisposal disposal);
        Result Delete(int id);
    }

    public class AssetDisposalService
        : AssetMovementServiceBase<AssetDisposal, AssetDisposal, AssetDisposalFilter>, IAssetDisposalService
    {
        protected override string EntityName => "AssetDisposals";

        private readonly IAssetDisposalRepository _disposals;
        private readonly IAssetRepository _assets;
        private readonly ITreasuryRepository _treasuryRows;

        public AssetDisposalService(IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, Entries journals, AccountOf accountsOf,
            IAssetDisposalRepository disposals, IAssetRepository assets, ITreasuryRepository treasuryRows)
            : base(permissions, settings, localization, audit, journals, accountsOf)
        {
            _treasuryRows = treasuryRows;
            _disposals = disposals;
            _assets = assets;
        }

        protected override AssetDisposal FindById(int id) => _disposals.GetById(id);

        protected override (List<AssetDisposal> Items, int Total) FindPaged(int page, int pageSize, AssetDisposalFilter filter)
        {
            filter ??= new AssetDisposalFilter();
            return _disposals.GetPaged(page, pageSize, filter.SearchText, filter.AssetId, filter.SortBy, filter.SortDescending);
        }

        protected override List<AssetDisposal> FindSearch(string term, int maxResults) =>
            _disposals.GetPaged(1, maxResults, term).Items;

        public Result<AssetDisposal> Create(AssetDisposal disposal) => Record(db => Write(db, disposal));

        public Result Update(AssetDisposal disposal) => Replace(disposal, Write);

        public static readonly Field<AssetDisposal>[] DisposalFields =
        {
            new(x => x.AssetId, "", Required: true, Message: "Str.Asset.PickAsset"),
            new(x => x.TreasuryId, "", Required: true, Message: "Str.Asset.PickDisposalTreasury"),
            new(x => x.DisposalDate, "", Required: true, Message: "Str.Asset.DisposalDateRequired"),
            new(x => x.SalePrice, "", From: 0, Message: "Str.Asset.SalePriceNegative"),
        };

        protected override int? EntryOf(AssetDisposal d) => d.JournalEntryId;
        protected override object AuditOf(AssetDisposal d) => new { d.AssetId, d.SalePrice, GainOrLoss = AssetCalc.GainOrLoss(d) };

        private Result<AssetDisposal> Write(PrimeDbContext db, AssetDisposal disposal)
        {
            var asset = _assets.GetById(disposal.AssetId, db);
            if (asset == null) return Fail<AssetDisposal>("NotFound", ErrorCode.NotFound);

            if (_disposals.AnyForAsset(disposal.AssetId, db))
                return Fail<AssetDisposal>("AlreadyDisposed", ErrorCode.ValidationFailed);

            disposal.AssetValue = asset.RevaluedValue > 0 ? asset.RevaluedValue : asset.PurchaseCost;
            disposal.AccumulatedDepreciation = asset.AccumulatedDepreciation;

            return Check.Valid(disposal, DisposalFields)
                .Then(() => Lines(asset, disposal))
                .Then(lines =>
                {
                    disposal.Id = _disposals.Insert(disposal, db);

                    disposal.JournalEntryId = PostEntry(db, disposal.DisposalDate, $"{Msg("Disposal")} — {asset.Name}", lines);

                    _disposals.SetJournalEntryId(db, disposal.Id, disposal.JournalEntryId.Value);
                    Activate(db, asset, false);
                    return Result.Ok(disposal);
                });
        }

        protected override Result Undo(PrimeDbContext db, AssetDisposal disposal)
        {
            var asset = _assets.GetById(disposal.AssetId, db);
            if (asset == null) return Fail("NotFound", ErrorCode.NotFound);

            ReverseEntry(db, disposal.JournalEntryId);
            _disposals.Delete(disposal.Id, CurrentUser, db);
            Activate(db, asset, true);
            return Result.Ok();
        }

        private Result<List<CreateJournalLineDto>> Lines(Asset asset, AssetDisposal disposal)
        {
            var own = Required(asset.AccountCode, "AccountsMissing");
            if (own.IsFailure) return Result.Fail<List<CreateJournalLineDto>>(own.ErrorMessage, own.ErrorCode);

            var cash = AccountsOf.Treasury(disposal.TreasuryId, "Str.Asset.FundingAccountMissing");
            if (cash.IsFailure) return Result.Fail<List<CreateJournalLineDto>>(cash.ErrorMessage, cash.ErrorCode);
            var gain = AssetCalc.GainOrLoss(disposal);
            var counter = gain == 0 ? Result.Ok("")
                : Account(gain > 0 ? SettingKeys.Accounts.CapitalGains : SettingKeys.Accounts.CapitalLosses);
            if (counter.IsFailure) return Result.Fail<List<CreateJournalLineDto>>(counter.ErrorMessage, counter.ErrorCode);

            var note = $"{Msg("Disposal")} — {asset.Name}";
            return Result.Ok(new JournalLines()
                .Debit(cash.Value, disposal.SalePrice, note)
                .Debit(asset.DepreciationAccountCode, disposal.AccumulatedDepreciation, note)
                .Credit(own.Value, disposal.AssetValue, note)
                .Add(counter.Value, gain < 0 ? -gain : 0, gain > 0 ? gain : 0, note)
                .ToList());
        }

        private void Activate(PrimeDbContext db, Asset asset, bool active)
        {
            asset.IsActive = active;
            _assets.Update(asset, db);
        }

        protected override AssetDisposal ToDto(AssetDisposal d) =>
            ToDto(d, _assets.GetById(d.AssetId), _treasuryRows.NamesOf(new[] { d.TreasuryId }));

        protected override List<AssetDisposal> ToDtos(List<AssetDisposal> rows)
        {
            var treasuries = _treasuryRows.NamesOf(rows.Select(d => d.TreasuryId));
            return WithAssets(rows, _assets, d => d.AssetId, (d, asset) => ToDto(d, asset, treasuries));
        }

        private static AssetDisposal ToDto(AssetDisposal d, Asset asset, IReadOnlyDictionary<int, string> treasuries)
        {
            (d.AssetCode, d.AssetName) = (asset?.Code, asset?.Name);
            d.TreasuryName = treasuries.GetValueOrDefault(d.TreasuryId);
            d.BookValue = AssetCalc.BookValue(d);
            d.GainOrLoss = AssetCalc.GainOrLoss(d);
            d.KindName = LocalizationService.Get(d.GainOrLoss >= 0 ? "Str.Asset.Gain" : "Str.Asset.Loss");
            return d;
        }
    }
}
