using PrimeERP.Application.Services.Entities;
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
    /// <summary>قسط الإهلاك سجلٌّ مستقلّ</summary>
    public interface IAssetDepreciationService
    {
        Result<decimal> MonthlyAmount(int assetId);

        Result<int> RunFor(DateTime upTo);

        Result<PagedResult<AssetDepreciationDto>> GetPaged(int page, int pageSize, AssetDepreciationFilter filter = null);
        Result<AssetDepreciationDto> GetById(int id);
        Result<AssetDepreciationDto> Create(CreateAssetDepreciationDto dto);
        Result Update(UpdateAssetDepreciationDto dto);
        Result Delete(int id);
    }

    public class AssetDepreciationService
        : AssetMovementServiceBase<AssetDepreciation, AssetDepreciationDto, AssetDepreciationFilter>, IAssetDepreciationService
    {
        protected override string EntityName => "AssetDepreciation";

        private readonly IAssetRepository _assets;
        private readonly IAssetDepreciationRepository _charges;

        public AssetDepreciationService(IAssetRepository assets, IAssetDepreciationRepository charges,
            Entries journals, AccountOf accountsOf,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit, journals, accountsOf)
        {
            _assets = assets; _charges = charges;
        }

        protected override AssetDepreciation FindById(int id) => _charges.GetById(id);

        protected override (List<AssetDepreciation> Items, int Total) FindPaged(int page, int pageSize, AssetDepreciationFilter filter)
        {
            filter ??= new AssetDepreciationFilter();
            return _charges.GetPaged(page, pageSize, filter.SearchText, filter.AssetId, filter.SortBy, filter.SortDescending);
        }

        protected override List<AssetDepreciation> FindSearch(string term, int maxResults) =>
            _charges.GetPaged(1, maxResults, term).Items;

        private static decimal Base(Asset asset) =>
            asset.RevaluedValue > 0 ? asset.RevaluedValue : asset.PurchaseCost;

        public Result<decimal> MonthlyAmount(int assetId)
        {
            var asset = _assets.GetById(assetId);
            return asset == null
                ? Result.Fail<decimal>(Msg("NotFound"), ErrorCode.NotFound)
                : Result.Ok(AssetCalc.PerMonth(Base(asset), asset.SalvageValue, asset.UsefulLifeYears));
        }

        public Result<AssetDepreciationDto> Create(CreateAssetDepreciationDto dto) => Record(db => Write(db, dto));

        public Result Update(UpdateAssetDepreciationDto dto) => Replace(dto.Id, db => Write(db, Rows.Copy(dto, new CreateAssetDepreciationDto())));

        public static readonly Field<AssetDepreciation>[] ChargeFields =
        {
            new(x => x.AssetId, "", Required: true, Message: "Str.Asset.PickAsset"),
            new(x => x.PeriodDate, "", Required: true, Message: "Str.Asset.DepreciationMonthRequired"),
            new(x => x.Amount, "Str.Asset.ChargeAmount", From: 0),
        };

        protected override int? EntryOf(AssetDepreciation charge) => charge.JournalEntryId;
        protected override object AuditOf(AssetDepreciation charge) => new { charge.AssetId, charge.PeriodDate, charge.Amount };

        private Result<AssetDepreciation> Write(PrimeDbContext db, CreateAssetDepreciationDto dto)
        {
            var expense = Account(SettingKeys.Accounts.DepreciationExpense);
            if (expense.IsFailure) return expense.As<AssetDepreciation>();

            var asset = _assets.GetById(dto.AssetId, db);
            if (asset == null) return Fail<AssetDepreciation>("NotFound", ErrorCode.NotFound);
            if (string.IsNullOrWhiteSpace(asset.DepreciationAccountCode))
                return Fail<AssetDepreciation>("MirrorMissing", ErrorCode.ValidationFailed);

            var charge = Rows.Copy(dto, new AssetDepreciation(), to =>
            {
                to.PeriodDate = AssetCalc.EndOfMonth(dto.PeriodDate);
                to.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? Msg("DepreciationNote", asset.Name, dto.PeriodDate) : dto.Notes;
            });

            return Check.Valid(charge, ChargeFields).Then(() =>
            {
                Post(db, charge, asset, expense.Value);
                return Result.Ok(charge);
            });
        }

        protected override Result Undo(PrimeDbContext db, AssetDepreciation charge)
        {
            ReverseEntry(db, charge.JournalEntryId);
            _charges.Delete(charge.Id, db);
            Recalculate(db, charge.AssetId);
            return Result.Ok();
        }

        /// <summary>تشغيلةٌ في معاملةٍ واحدة</summary>
        public Result<int> RunFor(DateTime upTo)
        {
            if (!Can("Create")) return FailDenied<int>();

            var expense = Account(SettingKeys.Accounts.DepreciationExpense);
            if (expense.IsFailure) return expense.As<int>();

            var run = Commit(db =>
            {
                var created = 0;
                foreach (var asset in _assets.Depreciable(db))
                {
                    var schedule = AssetCalc.Schedule(
                        Base(asset), asset.SalvageValue, asset.UsefulLifeYears, asset.AccumulatedDepreciation,
                        AssetCalc.FirstUndepreciatedMonth(asset.LastDepreciationDate, asset.PurchaseDate), upTo).ToList();

                    foreach (var (period, amount) in schedule)
                    {
                        Post(db, new AssetDepreciation
                        {
                            AssetId = asset.Id, PeriodDate = period, Amount = amount,
                            Notes = Msg("DepreciationNote", asset.Name, period)
                        }, asset, expense.Value);
                        created++;
                    }
                }
                return Result.Ok(created);
            });
            if (run.IsFailure) return run;

            if (run.Value > 0) Audit.Log(EntityName, 0, AuditAction.Insert, details: Msg("DepreciationRunLog", run.Value));
            return run;
        }

        private void Post(PrimeDbContext db, AssetDepreciation charge, Asset asset, string expenseAccount)
        {
            charge.Id = _charges.Insert(charge, db);

            var (debit, credit) = (expenseAccount, asset.DepreciationAccountCode);
            charge.JournalEntryId = PostEntry(db, charge.PeriodDate, charge.Notes, debit, credit, charge.Amount, asset.Name);

            _charges.SetJournalEntryId(db, charge.Id, charge.JournalEntryId.Value);
            Recalculate(db, charge.AssetId);
        }

        private void Recalculate(PrimeDbContext db, int assetId)
        {
            var asset = _assets.GetById(assetId, db);
            if (asset == null) return;

            var charges = _charges.OfAsset(assetId, db);

            asset.AccumulatedDepreciation = charges.Sum(c => c.Amount);
            asset.CurrentValue = AssetCalc.BookValue(Base(asset), asset.AccumulatedDepreciation);
            asset.LastDepreciationDate = charges.Count == 0 ? null : charges.Max(c => c.PeriodDate);

            _assets.Update(asset, db);
        }

        protected override AssetDepreciationDto ToDto(AssetDepreciation charge) => ToDto(charge, _assets.GetById(charge.AssetId));

        protected override List<AssetDepreciationDto> ToDtos(List<AssetDepreciation> charges) =>
            WithAssets(charges, _assets, c => c.AssetId, ToDto);

        private AssetDepreciationDto ToDto(AssetDepreciation charge, Asset asset)
        {

            return Rows.Copy(charge, new AssetDepreciationDto(), to =>
            {
                to.AssetCode = asset?.Code;
                to.AssetName = asset?.Name;
                to.CanEdit = Can("Edit");
                to.CanDelete = Can("Delete");
            });
        }
    }
}
