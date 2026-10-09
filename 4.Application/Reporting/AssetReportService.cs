using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Enums;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.PageServices.Assets;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Reporting
{
    /// <summary>تقريرا الأصول</summary>
    public interface IAssetReportService
    {
        Result<ReportData> Register(DateTime from, DateTime to, int? categoryId);
        Result<ReportData> ByCategory();
    }

    public class AssetReportService : ReportServiceBase, IAssetReportService
    {
        private readonly IAssetService _assets;
        private readonly IAssetDepreciationRepository _charges;
        private readonly IAssetRevaluationRepository _revaluations;
        private readonly IAssetDisposalRepository _disposals;
        private readonly IPartyRepository<Supplier> _suppliers;

        public AssetReportService(IAssetService assets, IAssetDepreciationRepository charges, IAssetRevaluationRepository revaluations,
            IAssetDisposalRepository disposals, IPartyRepository<Supplier> suppliers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _assets = assets; _charges = charges; _revaluations = revaluations; _disposals = disposals; _suppliers = suppliers;
        }

        public Result<ReportData> Register(DateTime from, DateTime to, int? categoryId)
        {
            var gate = Gate(); if (gate != null) return gate;

            var loaded = Load(categoryId);
            if (loaded.IsFailure) return Result.Fail<ReportData>(loaded.ErrorMessage);

            var charges = _charges.UpTo(to).ToLookup(c => c.AssetId);
            var revaluations = _revaluations.UpTo(to).ToLookup(r => r.AssetId);
            var disposals = _disposals.UpTo(to).GroupBy(d => d.AssetId).ToDictionary(g => g.Key, g => g.First());
            var suppliers = _suppliers.NamesOf(loaded.Value.Where(a => a.AcquisitionMethod == AssetAcquisition.Supplier && a.FundingId != null)
                .Select(a => a.FundingId.Value));

            var rows = loaded.Value
                .Where(a => (a.PurchaseDate ?? from) <= to && !(disposals.TryGetValue(a.Id, out var gone) && gone.DisposalDate < from))
                .OrderBy(a => a.CategoryName).ThenBy(a => a.Code)
                .Select((a, i) =>
                {
                    var roll = AssetCalc.Roll(a, charges[a.Id], revaluations[a.Id], disposals.GetValueOrDefault(a.Id), from);
                    return new AssetRegisterRow
                    {
                        No = i + 1, CategoryName = a.CategoryName, Name = a.Name, Code = a.Code,
                        SupplierName = a.FundingId is int supplier ? suppliers.GetValueOrDefault(supplier) : null,
                        PurchaseDate = a.PurchaseDate?.ToString("yyyy-MM-dd"), PurchaseCost = a.PurchaseCost,
                        Additions = roll.Additions, Reductions = roll.Reductions, Rate = roll.Rate,
                        AccumulatedStart = roll.AccumulatedStart, Charge = roll.Charge, AccumulatedEnd = roll.AccumulatedEnd, Net = roll.Net
                    };
                }).ToList();

            return Result.Ok(new ReportData { Rows = rows, Totals = RollTotals(rows) });
        }

        private static Dictionary<string, string> RollTotals(List<AssetRegisterRow> rows) => new()
        {
            ["Cost"] = $"{LocalizationService.Get("Str.Asset.Cost")}: {rows.Sum(r => r.PurchaseCost):N2}",
            ["Charge"] = $"{LocalizationService.Get("Str.Asset.Charge")}: {rows.Sum(r => r.Charge):N2}",
            ["AccumulatedEnd"] = $"{LocalizationService.Get("Str.Asset.AccumulatedEnd")}: {rows.Sum(r => r.AccumulatedEnd):N2}",
            ["Net"] = $"{LocalizationService.Get("Str.Asset.NetValue")}: {rows.Sum(r => r.Net):N2}"
        };

        public Result<ReportData> ByCategory()
        {
            var gate = Gate(); if (gate != null) return gate;

            var loaded = Load(null);
            if (loaded.IsFailure) return Result.Fail<ReportData>(loaded.ErrorMessage);

            var rows = new List<AssetRegisterRow>();

            foreach (var group in loaded.Value
                         .GroupBy(asset => asset.CategoryName ?? LocalizationService.Get("Str.Uncategorized"))
                         .OrderBy(group => group.Key))
            {
                var assets = group.Select(Row).ToList();

                rows.Add(new AssetRegisterRow { Name = group.Key, Kind = "heading" });
                rows.AddRange(assets);
                rows.Add(new AssetRegisterRow
                {
                    Name = $"{LocalizationService.Get("Str.Total")} — {group.Key}",
                    Kind = "total",
                    PurchaseCost = assets.Sum(a => a.PurchaseCost),
                    Revalued = assets.Sum(a => a.Revalued),
                    Accumulated = assets.Sum(a => a.Accumulated),
                    BookValue = assets.Sum(a => a.BookValue)
                });
            }

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = Totals(rows.Where(row => string.IsNullOrEmpty(row.Kind)).ToList())
            });
        }

        private Result<List<Asset>> Load(int? categoryId)
        {
            var result = _assets.GetPaged(1, int.MaxValue, new AssetFilter { CategoryId = categoryId, SortBy = "Code" });

            return result.IsSuccess
                ? Result.Ok(result.Value.Items.ToList())
                : Result.Fail<List<Asset>>(result.ErrorMessage);
        }

        private static AssetRegisterRow Row(Asset asset) => new()
        {
            Code = asset.Code,
            Name = asset.Name,
            CategoryName = asset.CategoryName,
            PurchaseDate = asset.PurchaseDate?.ToString("yyyy-MM-dd"),
            PurchaseCost = asset.PurchaseCost,
            Revalued = asset.RevaluedValue,
            Accumulated = asset.AccumulatedDepreciation,
            BookValue = asset.CurrentValue,
            Location = asset.Location
        };

        private static Dictionary<string, string> Totals(List<AssetRegisterRow> rows) => new()
        {
            ["Cost"] = $"{LocalizationService.Get("Str.Asset.Cost")}: {rows.Sum(r => r.PurchaseCost):N2}",
            ["Revalued"] = $"{LocalizationService.Get("Str.Asset.Revalued")}: {rows.Sum(r => r.Revalued):N2}",
            ["Accumulated"] = $"{LocalizationService.Get("Str.Asset.Accumulated")}: {rows.Sum(r => r.Accumulated):N2}",
            ["BookValue"] = $"{LocalizationService.Get("Str.Asset.BookValue")}: {rows.Sum(r => r.BookValue):N2}"
        };
    }
}
