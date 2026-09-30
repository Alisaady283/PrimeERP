using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Legacy.Admin;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Legacy.Accounting;
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
        Result<PagedResult<AssetRevaluationDto>> GetPaged(int page, int pageSize, AssetRevaluationFilter filter = null);
        Result<AssetRevaluationDto> GetById(int id);
        Result<AssetRevaluationDto> Create(CreateAssetRevaluationDto dto);
        Result Update(UpdateAssetRevaluationDto dto);
        Result Delete(int id);
    }

    public class AssetRevaluationService
        : AssetMovementServiceBase<AssetRevaluation, AssetRevaluationDto, AssetRevaluationFilter>, IAssetRevaluationService
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

        public Result<AssetRevaluationDto> Create(CreateAssetRevaluationDto dto) => Record(db => Write(db, dto));

        public Result Update(UpdateAssetRevaluationDto dto) => Replace(dto.Id, db => Write(db, Rows.Copy(dto, new CreateAssetRevaluationDto())));

        public static readonly Field<AssetRevaluation>[] RevaluationFields =
        {
            new(x => x.AssetId, "", Required: true, Message: "Str.Asset.PickAsset"),
            new(x => x.RevaluationDate, "", Required: true, Message: "Str.Asset.RevaluationDateRequired"),
            new(x => x.NewValue, "Str.Asset.Revalued", From: 0),
            new(x => x.NewValue, "", Must: r => AssetCalc.Difference(r) != 0, Message: "Str.Asset.RevaluationNoChange"),
        };

        protected override int? EntryOf(AssetRevaluation r) => r.JournalEntryId;
        protected override object AuditOf(AssetRevaluation r) => new { r.AssetId, r.OldValue, r.NewValue };

        private Result<AssetRevaluation> Write(PrimeDbContext db, CreateAssetRevaluationDto dto)
        {
            var asset = _assets.GetById(dto.AssetId, db);
            if (asset == null) return Fail<AssetRevaluation>("NotFound", ErrorCode.NotFound);

            var revaluation = Rows.Copy(dto, new AssetRevaluation(), to =>
            {
                to.OldValue = asset.RevaluedValue;
            });

            return Check.Valid(revaluation, RevaluationFields)
                .Then(() => Sides(asset, AssetCalc.Difference(revaluation)))
                .Then(sides =>
                {
                    revaluation.Id = _revaluations.Insert(revaluation, db);
                    Apply(db, asset, revaluation.NewValue);

                    revaluation.JournalEntryId = PostEntry(db, revaluation.RevaluationDate,
                        $"{Msg("Revaluation")} — {asset.Name}", sides.Debit, sides.Credit, Math.Abs(AssetCalc.Difference(revaluation)));

                    _revaluations.SetJournalEntryId(db, revaluation.Id, revaluation.JournalEntryId.Value);
                    return Result.Ok(revaluation);
                });
        }

        protected override Result Undo(PrimeDbContext db, AssetRevaluation revaluation)
        {
            var asset = _assets.GetById(revaluation.AssetId, db);
            if (asset == null) return Fail("NotFound", ErrorCode.NotFound);

            ReverseEntry(db, revaluation.JournalEntryId);
            Apply(db, asset, revaluation.OldValue);
            _revaluations.Delete(revaluation.Id, CurrentUser, db);
            return Result.Ok();
        }

        private Result<(string Debit, string Credit)> Sides(Asset asset, decimal difference)
        {
            var own = Required(asset.AccountCode, "AccountsMissing");
            if (own.IsFailure) return Result.Fail<(string, string)>(own.ErrorMessage, own.ErrorCode);

            var counter = Account(difference > 0 ? SettingKeys.Accounts.CapitalGains : SettingKeys.Accounts.CapitalLosses);
            if (counter.IsFailure) return Result.Fail<(string, string)>(counter.ErrorMessage, counter.ErrorCode);

            return Result.Ok(TwoSided.BySign(difference > 0, own.Value, counter.Value));
        }

        private void Apply(PrimeDbContext db, Asset asset, decimal value)
        {
            asset.RevaluedValue = value;
            asset.CurrentValue = AssetCalc.BookValue(value, asset.AccumulatedDepreciation);
            _assets.Update(asset, db);
        }

        protected override AssetRevaluationDto ToDto(AssetRevaluation r) => ToDto(r, _assets.GetById(r.AssetId));

        protected override List<AssetRevaluationDto> ToDtos(List<AssetRevaluation> rows) =>
            WithAssets(rows, _assets, r => r.AssetId, ToDto);

        private AssetRevaluationDto ToDto(AssetRevaluation r, Asset asset)
        {
            var increase = AssetCalc.Difference(r) >= 0;

            var kind = Rows.State((increase, StatusVariant.Success, "Str.Asset.Increase"), (true, StatusVariant.Danger, "Str.Asset.Decrease"));
            return Rows.Copy(r, new AssetRevaluationDto(), to =>
            {
                to.AssetCode = asset?.Code;
                to.AssetName = asset?.Name;
                to.Difference = AssetCalc.Difference(r);
                to.KindText = kind.Text;
                to.KindVariant = kind.Variant;
                to.CanEdit = Can("Edit");
                to.CanDelete = Can("Delete");
            });
        }
    }
}
