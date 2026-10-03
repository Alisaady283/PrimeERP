using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Legacy.Admin;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Services.Entities;
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
    /// <summary>إعادة تقييم الأصل</summary>
    public interface IAssetRevaluationService
    {
        Result<PagedResult<AssetRevaluation>> GetPaged(int page, int pageSize, AssetRevaluationFilter filter = null);
        Result<AssetRevaluation> GetById(int id);
        Result<AssetRevaluation> Create(AssetRevaluation revaluation);
        Result Update(AssetRevaluation revaluation);
        Result Delete(int id);
    }

    public class AssetRevaluationService
        : AssetMovementServiceBase<AssetRevaluation, AssetRevaluation, AssetRevaluationFilter>, IAssetRevaluationService
    {
        protected override string EntityName => "AssetRevaluations";

        private readonly IAssetRevaluationRepository _revaluations;
        private readonly IAssetRepository _assets;

        public AssetRevaluationService(IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, Entries journals, AccountOf accountsOf,
            IAssetRevaluationRepository revaluations, IAssetRepository assets)
            : base(permissions, settings, localization, audit, journals, accountsOf)
        {
            _revaluations = revaluations;
            _assets = assets;
        }

        protected override AssetRevaluation FindById(int id) => _revaluations.GetById(id);

        protected override (List<AssetRevaluation> Items, int Total) FindPaged(int page, int pageSize, AssetRevaluationFilter filter)
        {
            filter ??= new AssetRevaluationFilter();
            return _revaluations.GetPaged(page, pageSize, filter.SearchText, filter.AssetId, filter.SortBy, filter.SortDescending);
        }

        protected override List<AssetRevaluation> FindSearch(string term, int maxResults) =>
            _revaluations.GetPaged(1, maxResults, term).Items;

        public Result<AssetRevaluation> Create(AssetRevaluation revaluation) => Record(db => Write(db, revaluation));

        public Result Update(AssetRevaluation revaluation) => Replace(revaluation, Write);

        public static readonly Field<AssetRevaluation>[] RevaluationFields =
        {
            new(x => x.AssetId, "", Required: true, Message: "Str.Asset.PickAsset"),
            new(x => x.RevaluationDate, "", Required: true, Message: "Str.Asset.RevaluationDateRequired"),
            new(x => x.NewValue, "Str.Asset.NewValue", Positive: true, Message: "Str.Asset.RevaluationZero"),
            new(x => x.NewValue, "", Must: r => AssetCalc.Difference(r) != 0, Message: "Str.Asset.RevaluationNoChange"),
        };

        protected override int? EntryOf(AssetRevaluation r) => r.JournalEntryId;
        protected override object AuditOf(AssetRevaluation r) => new { r.AssetId, r.OldValue, r.NewValue };

        private Result<AssetRevaluation> Write(PrimeDbContext db, AssetRevaluation revaluation)
        {
            var asset = _assets.GetById(revaluation.AssetId, db);
            if (asset == null) return Fail<AssetRevaluation>("NotFound", ErrorCode.NotFound);

            revaluation.OldValue = asset.RevaluedValue;
            var note = $"{Msg("Revaluation")} — {asset.Name}";

            return Check.Valid(revaluation, RevaluationFields)
                .Then(() => Lines(asset, revaluation, note))
                .Then(lines =>
                {
                    revaluation.Id = _revaluations.Insert(revaluation, db);
                    Apply(db, asset, revaluation.NewValue);

                    revaluation.JournalEntryId = Posting.Entry(Journals, db, revaluation.RevaluationDate, note, EntityName, lines);

                    _revaluations.SetJournalEntryId(db, revaluation.Id, revaluation.JournalEntryId.Value);
                    return Result.Ok(revaluation);
                });
        }

        protected override Result Undo(PrimeDbContext db, AssetRevaluation revaluation)
        {
            var asset = _assets.GetById(revaluation.AssetId, db);
            if (asset == null) return Fail("NotFound", ErrorCode.NotFound);

            Posting.Reverse(Journals, db, revaluation.JournalEntryId);
            Apply(db, asset, revaluation.OldValue);
            _revaluations.Delete(revaluation.Id, CurrentUser, db);
            return Result.Ok();
        }

        private Result<List<CreateJournalLineDto>> Lines(Asset asset, AssetRevaluation revaluation, string note)
        {
            var own = AccountOf.Required(asset.AccountCode, "Str.Asset.AccountsMissing");
            if (own.IsFailure) return own.As<List<CreateJournalLineDto>>();

            var counter = AccountsOf.SettingBySign(AssetCalc.Difference(revaluation), SettingKeys.Accounts.CapitalGains, SettingKeys.Accounts.CapitalLosses, "Str.Asset.AccountsMissing");
            if (counter.IsFailure) return counter.As<List<CreateJournalLineDto>>();

            return Result.Ok(RevaluationEntry.Lines(revaluation, own.Value, counter.Value, note));
        }

        private void Apply(PrimeDbContext db, Asset asset, decimal value)
        {
            asset.RevaluedValue = value;
            asset.CurrentValue = AssetCalc.CurrentValue(value, asset.PurchaseCost, asset.AccumulatedDepreciation);
            _assets.Update(asset, db);
        }

        protected override AssetRevaluation ToDto(AssetRevaluation r)
        {
            r.Difference = AssetCalc.Difference(r);
            r.KindName = Rows.State(
                (r.Difference >= 0, StatusVariant.Success, "Str.Asset.Increase"),
                (true, StatusVariant.Danger, "Str.Asset.Decrease")).Text;
            return r;
        }
    }
}
