using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Treasury;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Application.Services.Admin;

namespace PrimeERP.Application.Services.Assets
{
    /// <summary>بيع الأصل واستبعاده</summary>
    public interface IAssetDisposalService
    {
        Result<PagedResult<AssetDisposalDto>> GetPaged(int page, int pageSize, AssetDisposalFilter filter = null);
        Result<AssetDisposalDto> GetById(int id);
        Result<AssetDisposalDto> Create(CreateAssetDisposalDto dto);
        Result Update(UpdateAssetDisposalDto dto);
        Result Delete(int id);
    }

    public class AssetDisposalService
        : AssetMovementServiceBase<AssetDisposal, AssetDisposalDto, AssetDisposalFilter>, IAssetDisposalService
    {
        protected override string EntityName => "AssetDisposals";

        private readonly IAssetDisposalRepository _disposals;
        private readonly IAssetRepository _assets;
        private readonly ITreasuryService _treasuries;

        public AssetDisposalService(IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IJournalService journals, ISettingsService settingsService,
            IAssetDisposalRepository disposals, IAssetRepository assets, ITreasuryService treasuries)
            : base(permissions, settings, localization, audit, journals, settingsService)
        {
            _disposals = disposals;
            _assets = assets;
            _treasuries = treasuries;
        }

        protected override AssetDisposal FindById(int id) => _disposals.GetById(id);

        protected override (List<AssetDisposal> Items, int Total) FindPaged(int page, int pageSize, AssetDisposalFilter filter)
        {
            filter ??= new AssetDisposalFilter();
            return _disposals.GetPaged(page, pageSize, filter.SearchText, filter.AssetId, filter.SortBy, filter.SortDescending);
        }

        protected override List<AssetDisposal> FindSearch(string term, int maxResults) =>
            _disposals.GetPaged(1, maxResults, term).Items;

        public Result<AssetDisposalDto> Create(CreateAssetDisposalDto dto)
        {
            if (!Can("Create")) return FailDenied<AssetDisposalDto>();

            var asset = _assets.GetById(dto.AssetId);
            if (asset == null) return Result.Fail<AssetDisposalDto>(Msg("NotFound"), ErrorCode.NotFound);

            if (_disposals.GetPaged(1, 1, assetId: dto.AssetId).Total > 0)
                return Result.Fail<AssetDisposalDto>(Msg("AlreadyDisposed"), ErrorCode.ValidationFailed);

            var disposal = new AssetDisposal
            {
                AssetId = dto.AssetId,
                DisposalDate = dto.DisposalDate,
                TreasuryId = dto.TreasuryId,
                SalePrice = dto.SalePrice,
                AssetValue = asset.RevaluedValue > 0 ? asset.RevaluedValue : asset.PurchaseCost,
                AccumulatedDepreciation = asset.AccumulatedDepreciation,
                Notes = dto.Notes,
                CreatedBy = CurrentUser
            };

            var invalid = Check(new AssetDisposalValidator(), disposal);
            if (invalid.IsFailure) return Result.Fail<AssetDisposalDto>(invalid.ErrorMessage, invalid.ErrorCode);

            var lines = Lines(asset, disposal);
            if (lines.IsFailure) return Result.Fail<AssetDisposalDto>(lines.ErrorMessage, lines.ErrorCode);

            try
            {
                Tx(db =>
                {
                    disposal.Id = _disposals.Insert(disposal, db);

                    disposal.JournalEntryId = PostEntry(db, disposal.DisposalDate,
                        $"{Msg("Disposal")} — {asset.Name}", lines.Value);

                    _disposals.SetJournalEntryId(db, disposal.Id, disposal.JournalEntryId.Value);
                    Activate(db, asset, false);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<AssetDisposalDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log(EntityName, disposal.Id, AuditAction.Insert,
                newValue: new { asset.Code, disposal.SalePrice, disposal.GainOrLoss });

            return Result.Ok(ToDto(disposal));
        }

        public Result Update(UpdateAssetDisposalDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            if (_disposals.GetById(dto.Id) == null) return Fail("NotFound", ErrorCode.NotFound);

            var removed = Delete(dto.Id);
            if (removed.IsFailure) return removed;

            var created = Create(new CreateAssetDisposalDto
            {
                AssetId = dto.AssetId, DisposalDate = dto.DisposalDate,
                TreasuryId = dto.TreasuryId, SalePrice = dto.SalePrice, Notes = dto.Notes
            });

            return created.IsSuccess ? Result.Ok() : Result.Fail(created.ErrorMessage, created.ErrorCode);
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var disposal = _disposals.GetById(id);
            if (disposal == null) return Fail("NotFound", ErrorCode.NotFound);

            var asset = _assets.GetById(disposal.AssetId);
            if (asset == null) return Result.Fail(Msg("NotFound"), ErrorCode.NotFound);

            var funds = EnsureReversible(disposal.JournalEntryId);
            if (funds.IsFailure) return funds;

            Tx(db =>
            {
                ReverseEntry(db, disposal.JournalEntryId);
                _disposals.Delete(disposal.Id, CurrentUser, db);

                Activate(db, asset, true);
            });

            Audit.Log(EntityName, id, AuditAction.Delete, details: asset.Code);
            return Result.Ok();
        }

        private Result<List<CreateJournalLineDto>> Lines(Asset asset, AssetDisposal disposal)
        {
            var own = Required(asset.AccountCode, "AccountsMissing");
            if (own.IsFailure) return Result.Fail<List<CreateJournalLineDto>>(own.ErrorMessage, own.ErrorCode);

            var treasury = _treasuries.GetById(disposal.TreasuryId);
            if (treasury.IsFailure) return Result.Fail<List<CreateJournalLineDto>>(treasury.ErrorMessage, treasury.ErrorCode);

            var cash = Required(treasury.Value.AccountCode, "FundingAccountMissing");
            if (cash.IsFailure) return Result.Fail<List<CreateJournalLineDto>>(cash.ErrorMessage, cash.ErrorCode);

            var gain = disposal.GainOrLoss;
            var counter = gain == 0 ? Result.Ok("")
                : Account(gain > 0 ? SettingKeys.Accounts.CapitalGains : SettingKeys.Accounts.CapitalLosses);
            if (counter.IsFailure) return Result.Fail<List<CreateJournalLineDto>>(counter.ErrorMessage, counter.ErrorCode);

            var note = $"{Msg("Disposal")} — {asset.Name}";
            var lines = new List<CreateJournalLineDto>
            {
                new() { AccountCode = cash.Value,  Debit  = disposal.SalePrice,               Notes = note },
                new() { AccountCode = asset.DepreciationAccountCode, Debit = disposal.AccumulatedDepreciation, Notes = note },
                new() { AccountCode = own.Value,   Credit = disposal.AssetValue,              Notes = note },
                new() { AccountCode = counter.Value,
                        Debit  = gain < 0 ? -gain : 0,
                        Credit = gain > 0 ?  gain : 0, Notes = note },
            };

            var lineNo = 1;
            foreach (var line in lines.Where(Meaningful)) line.LineNo = lineNo++;

            return Result.Ok(lines.Where(Meaningful).ToList());
        }

        private static bool Meaningful(CreateJournalLineDto line) =>
            !string.IsNullOrWhiteSpace(line.AccountCode) && (line.Debit != 0 || line.Credit != 0);

        private void Activate(PrimeDbContext db, Asset asset, bool active)
        {
            asset.IsActive = active;
            _assets.Update(asset, db);
        }

        protected override AssetDisposalDto ToDto(AssetDisposal d)
        {
            var asset = _assets.GetById(d.AssetId);
            var gain = d.GainOrLoss >= 0;

            return new AssetDisposalDto
            {
                Id = d.Id, AssetId = d.AssetId,
                AssetCode = asset?.Code, AssetName = asset?.Name,
                DisposalDate = d.DisposalDate,
                TreasuryId = d.TreasuryId, TreasuryName = _treasuries.GetById(d.TreasuryId).Value?.Name,
                SalePrice = d.SalePrice, BookValue = d.BookValue, GainOrLoss = d.GainOrLoss,
                KindText = LocalizationService.Get(gain ? "Str.Asset.Gain" : "Str.Asset.Loss"),
                KindVariant = gain ? StatusVariant.Success : StatusVariant.Danger,
                Notes = d.Notes, JournalEntryId = d.JournalEntryId,
                CreatedAt = d.CreatedAt, UpdatedAt = d.UpdatedAt,
                CanEdit = Can("Edit"), CanDelete = Can("Delete")
            };
        }
    }
}
