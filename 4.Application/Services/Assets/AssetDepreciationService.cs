using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Assets
{
    public interface IAssetDepreciationService
    {
        Result<decimal> MonthlyAmount(int assetId);
        Result<int> RunFor(DateTime upTo);
    }

    /// <summary>إهلاك بالقسط الثابت: (التكلفة − القيمة المتبقية) ÷ (العمر × 12) لكل شهر مضى منذ آخر إهلاك.
    /// كل تشغيلة تُنتج قيداً واحداً (مصروف إهلاك / مجمع الإهلاك) — فالإهلاك معاملة كغيرها تصبّ في القيود،
    /// والتقارير تقرأه منها لا من جدول الأصول.</summary>
    public class AssetDepreciationService : ServiceBase, IAssetDepreciationService
    {
        private readonly IAssetRepository _assets;
        private readonly IJournalService _journals;
        private readonly ISettingsService _settingsService;

        public AssetDepreciationService(IAssetRepository assets, IJournalService journals, ISettingsService settingsService,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _assets = assets; _journals = journals; _settingsService = settingsService;
        }

        protected override string PermissionPrefix => "Assets";
        protected override string StringPrefix => "Str.Asset";
        protected override string EntityName => "Assets";

        private static decimal PerMonth(Domain.Entities.Asset asset) =>
            asset.UsefulLifeYears <= 0 ? 0 : (asset.PurchaseCost - asset.SalvageValue) / (asset.UsefulLifeYears * 12);

        public Result<decimal> MonthlyAmount(int assetId)
        {
            var asset = _assets.GetById(assetId);
            return asset == null ? Result.Fail<decimal>("الأصل غير موجود", ErrorCode.NotFound) : Result.Ok(PerMonth(asset));
        }

        public Result<int> RunFor(DateTime upTo)
        {
            if (!Can("Edit")) return FailDenied<int>();

            var expenseAccount = _settingsService.Get<string>(SettingKeys.Accounts.DepreciationExpense, "");
            var accumAccount = _settingsService.Get<string>(SettingKeys.Accounts.AccumulatedDepreciation, "");
            if (string.IsNullOrWhiteSpace(expenseAccount) || string.IsNullOrWhiteSpace(accumAccount))
                return Result.Fail<int>("حسابا مصروف الإهلاك ومجمع الإهلاك غير مضبوطين في الإعدادات", ErrorCode.ValidationFailed);

            var lines = new List<CreateJournalLineDto>();
            var touched = new List<(Domain.Entities.Asset Asset, decimal Amount)>();


            foreach (var asset in _assets.GetPaged(1, 100000).Items.Where(a => a.IsActive && a.UsefulLifeYears > 0))
            {
                var start = asset.LastDepreciationDate ?? asset.PurchaseDate;
                if (start == null || start >= upTo) continue;

                var months = ((upTo.Year - start.Value.Year) * 12) + upTo.Month - start.Value.Month;
                if (months <= 0) continue;

                var remaining = asset.PurchaseCost - asset.SalvageValue - asset.AccumulatedDepreciation;
                var amount = Math.Min(PerMonth(asset) * months, remaining);
                if (amount <= 0) continue;

                touched.Add((asset, (decimal)amount));
            }

            if (touched.Count == 0) return Result.Ok(0);

            var lineNo = 1;
            foreach (var (asset, amount) in touched)
                lines.Add(new CreateJournalLineDto { LineNo = lineNo++, AccountCode = expenseAccount, Debit = amount, Notes = $"إهلاك {asset.Name}" });

            lines.Add(new CreateJournalLineDto { LineNo = lineNo, AccountCode = accumAccount, Credit = touched.Sum(t => t.Amount) });

            var entry = _journals.Create(new CreateJournalDto
            {
                EntryDate = upTo,
                Description = $"إهلاك الأصول حتى {upTo:yyyy-MM-dd}",
                Source = "AssetDepreciation",
                Lines = lines
            });
            if (entry.IsFailure) return Result.Fail<int>(entry.ErrorMessage, entry.ErrorCode);

            var posted = _journals.Post(entry.Value.Id);
            if (posted.IsFailure) return Result.Fail<int>(posted.ErrorMessage, posted.ErrorCode);

            foreach (var (asset, amount) in touched)
            {
                asset.AccumulatedDepreciation += amount;
                asset.CurrentValue = asset.PurchaseCost - asset.AccumulatedDepreciation;
                asset.LastDepreciationDate = upTo;
                _assets.Update(asset);
            }

            Audit.Log(EntityName, entry.Value.Id, Domain.Enums.AuditAction.Insert, details: $"إهلاك {touched.Count} أصل");
            return Result.Ok(touched.Count);
        }
    }
}
