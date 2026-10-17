using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Integration
{
    /// <summary>فحصٌ دوري لحدثٍ واحد، يقيس بمعامل قاعدته</summary>
    public interface IAlertScan
    {
        string Event { get; }
        IEnumerable<AlertMessage> Find(AlertRule rule);
    }

    /// <summary>رصيد الصنف (كل المخازن) عند حدّ طلبه أو تحته — المعامل حدٌّ بديل إن لم يكن للصنف حدّ</summary>
    public sealed class StockLowScan : IAlertScan
    {
        private readonly IProductRepository _products;
        private readonly IStockMovementRepository _movements;

        public StockLowScan(IProductRepository products, IStockMovementRepository movements)
        {
            _products = products;
            _movements = movements;
        }

        public string Event => AlertEvents.StockLow;

        public IEnumerable<AlertMessage> Find(AlertRule rule)
        {
            var stock = _movements.GetAllBalances().GroupBy(b => b.ProductId).ToDictionary(g => g.Key, g => g.Sum(b => b.Balance));

            foreach (var product in _products.WithReorderPoint().Where(p => p.IsStockTracked && !p.IsService))
            {
                var balance = stock.GetValueOrDefault(product.Id);
                if (balance > product.ReorderPoint) continue;

                yield return new AlertMessage(Event, LocalizationService.Get("Str.Alert.Event.Stock.Low"),
                    LocalizationService.Get("Str.Alert.StockLowBody", product.Name, balance, product.ReorderPoint),
                    PageKey: "StockBalances", RecordId: product.Id, PermissionKey: $"{PermissionKeys.GateOf("StockBalances", "Reports")}.View",
                    DedupKey: $"{Event}:{product.Id}:{DateTime.Today:yyyyMMdd}", Severity: balance <= 0 ? "danger" : "warning");
            }
        }
    }

    /// <summary>تشغيلةٌ برصيدٍ تنتهي خلال المعامل بالأيام (وإلا أيام تنبيه الصلاحية في الإعدادات)</summary>
    public sealed class NearExpiryScan : IAlertScan
    {
        private readonly IStockMovementRepository _movements;
        private readonly IProductRepository _products;
        private readonly ISettingsProvider _settings;

        public NearExpiryScan(IStockMovementRepository movements, IProductRepository products, ISettingsProvider settings)
        {
            _movements = movements;
            _products = products;
            _settings = settings;
        }

        public string Event => AlertEvents.NearExpiry;

        public IEnumerable<AlertMessage> Find(AlertRule rule)
        {
            if (!_settings.TracksExpiry()) yield break;

            var days = rule.Threshold > 0 ? (int)rule.Threshold : _settings.Get(SettingKeys.Edition.ExpiryAlertDays, 90);
            var limit = DateTime.Today.AddDays(days);
            var near = _movements.AllBatches().Where(b => b.Balance > 0 && b.Expiry != null && b.Expiry.Value.Date <= limit).ToList();
            var names = _products.GetByIds(near.Select(b => b.ProductId).Distinct()).ToDictionary(p => p.Id, p => p.Name);

            foreach (var batch in near)
                yield return new AlertMessage(Event, LocalizationService.Get("Str.Alert.Event.Batch.NearExpiry"),
                    LocalizationService.Get("Str.Alert.NearExpiryBody", names.GetValueOrDefault(batch.ProductId), batch.Batch, batch.Expiry, batch.Balance),
                    PageKey: "ExpiryReport", RecordId: batch.ProductId, PermissionKey: $"{PermissionKeys.GateOf("ExpiryReport", "Reports")}.View",
                    DedupKey: $"{Event}:{batch.ProductId}:{batch.WarehouseId}:{batch.Batch}:{DateTime.Today:yyyyMMdd}",
                    Severity: batch.Expiry.Value.Date < DateTime.Today ? "danger" : "warning");
        }
    }
}
