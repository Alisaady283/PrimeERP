using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Audit;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Legacy.Assets;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Reporting
{
    /// <summary>تقريرا الأصول</summary>
    public interface IAssetReportService
    {
        Result<ReportData> Register(int? categoryId);
        Result<ReportData> ByCategory();
    }

    public class AssetReportService : ReportServiceBase, IAssetReportService
    {
        private readonly IAssetService _assets;

        public AssetReportService(IAssetService assets, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) => _assets = assets;

        public Result<ReportData> Register(int? categoryId)
        {
            var gate = Gate(); if (gate != null) return gate;

            var loaded = Load(categoryId);
            if (loaded.IsFailure) return Result.Fail<ReportData>(loaded.ErrorMessage);

            var rows = loaded.Value.Select(Row).ToList();

            return Result.Ok(new ReportData { Rows = rows, Totals = Totals(rows) });
        }

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

        private Result<List<AssetDto>> Load(int? categoryId)
        {
            var result = _assets.GetPaged(1, int.MaxValue, new AssetFilter { CategoryId = categoryId, SortBy = "Code" });

            return result.IsSuccess
                ? Result.Ok(result.Value.Items.ToList())
                : Result.Fail<List<AssetDto>>(result.ErrorMessage);
        }

        private static AssetRegisterRow Row(AssetDto asset) => new()
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
