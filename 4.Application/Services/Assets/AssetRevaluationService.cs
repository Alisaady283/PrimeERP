using PrimeERP.Data.Core;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Domain.Rules;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Application.Services.Admin;

namespace PrimeERP.Application.Services.Assets
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
            ILocalizationService localization, IAuditLogger audit, IJournalService journals, ISettingsService settingsService,
            IAssetRevaluationRepository revaluations, IAssetRepository assets)
            : base(permissions, settings, localization, audit, journals, settingsService)
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

        public Result<AssetRevaluationDto> Create(CreateAssetRevaluationDto dto)
        {
            if (!Can("Create")) return FailDenied<AssetRevaluationDto>();

            var asset = _assets.GetById(dto.AssetId);
            if (asset == null) return Result.Fail<AssetRevaluationDto>(Msg("NotFound"), ErrorCode.NotFound);

            var revaluation = new AssetRevaluation
            {
                AssetId = dto.AssetId,
                RevaluationDate = dto.RevaluationDate,
                OldValue = asset.RevaluedValue,
                NewValue = dto.NewValue,
                Notes = dto.Notes,
                CreatedBy = CurrentUser
            };

            var invalid = Check(new AssetRevaluationValidator(), revaluation);
            if (invalid.IsFailure) return Result.Fail<AssetRevaluationDto>(invalid.ErrorMessage, invalid.ErrorCode);

            var accounts = Sides(asset, revaluation.Difference);
            if (accounts.IsFailure) return Result.Fail<AssetRevaluationDto>(accounts.ErrorMessage, accounts.ErrorCode);

            try
            {
                Tx(db =>
                {
                    revaluation.Id = _revaluations.Insert(revaluation, db);
                    Apply(db, asset, revaluation.NewValue);

                    revaluation.JournalEntryId = PostEntry(db, revaluation.RevaluationDate,
                        $"{Msg("Revaluation")} — {asset.Name}",
                        accounts.Value.Debit, accounts.Value.Credit, System.Math.Abs(revaluation.Difference));

                    _revaluations.SetJournalEntryId(db, revaluation.Id, revaluation.JournalEntryId.Value);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<AssetRevaluationDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log(EntityName, revaluation.Id, AuditAction.Insert,
                newValue: new { asset.Code, revaluation.OldValue, revaluation.NewValue });

            return Result.Ok(ToDto(revaluation));
        }

        public Result Update(UpdateAssetRevaluationDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            if (_revaluations.GetById(dto.Id) == null) return Fail("NotFound", ErrorCode.NotFound);

            var removed = Delete(dto.Id);
            if (removed.IsFailure) return removed;

            var created = Create(new CreateAssetRevaluationDto
            {
                AssetId = dto.AssetId, RevaluationDate = dto.RevaluationDate,
                NewValue = dto.NewValue, Notes = dto.Notes
            });

            return created.IsSuccess ? Result.Ok() : Result.Fail(created.ErrorMessage, created.ErrorCode);
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var revaluation = _revaluations.GetById(id);
            if (revaluation == null) return Fail("NotFound", ErrorCode.NotFound);

            var asset = _assets.GetById(revaluation.AssetId);
            if (asset == null) return Result.Fail(Msg("NotFound"), ErrorCode.NotFound);

            var funds = EnsureReversible(revaluation.JournalEntryId);
            if (funds.IsFailure) return funds;

            Tx(db =>
            {
                ReverseEntry(db, revaluation.JournalEntryId);

                Apply(db, asset, revaluation.OldValue);
                _revaluations.Delete(revaluation.Id, CurrentUser, db);
            });

            Audit.Log(EntityName, id, AuditAction.Delete, details: asset.Code);
            return Result.Ok();
        }

        private Result<(string Debit, string Credit)> Sides(Asset asset, decimal difference)
        {
            var own = Required(asset.AccountCode, "AccountsMissing");
            if (own.IsFailure) return Result.Fail<(string, string)>(own.ErrorMessage, own.ErrorCode);

            var counter = Account(difference > 0 ? SettingKeys.Accounts.CapitalGains : SettingKeys.Accounts.CapitalLosses);
            if (counter.IsFailure) return Result.Fail<(string, string)>(counter.ErrorMessage, counter.ErrorCode);

            return difference > 0
                ? Result.Ok((own.Value, counter.Value))
                : Result.Ok((counter.Value, own.Value));
        }

        private void Apply(PrimeDbContext db, Asset asset, decimal value)
        {
            asset.RevaluedValue = value;
            asset.CurrentValue = DepreciationRules.BookValue(value, asset.AccumulatedDepreciation);
            _assets.Update(asset, db);
        }

        protected override AssetRevaluationDto ToDto(AssetRevaluation r)
        {
            var asset = _assets.GetById(r.AssetId);
            var increase = r.Difference >= 0;

            return new AssetRevaluationDto
            {
                Id = r.Id, AssetId = r.AssetId,
                AssetCode = asset?.Code, AssetName = asset?.Name,
                RevaluationDate = r.RevaluationDate,
                OldValue = r.OldValue, NewValue = r.NewValue, Difference = r.Difference,
                KindText = LocalizationService.Get(increase ? "Str.Asset.Increase" : "Str.Asset.Decrease"),
                KindVariant = increase ? StatusVariant.Success : StatusVariant.Danger,
                Notes = r.Notes, JournalEntryId = r.JournalEntryId,
                CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt,
                CanEdit = Can("Edit"), CanDelete = Can("Delete")
            };
        }
    }
}
