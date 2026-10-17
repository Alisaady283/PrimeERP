using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Application.Services.Integration
{
    /// <summary>ما حدث: يُعرض تنبيهاً داخلياً ويُرسل لقنوات القاعدة</summary>
    public sealed record AlertMessage(string Event, string Title, string Body, string PageKey = null, int? RecordId = null,
        string PermissionKey = null, string DedupKey = null, string Severity = "info");

    /// <summary>قناة إرسالٍ خارجية: بريد، واتساب، Webhook…</summary>
    public interface IAlertChannel
    {
        string Name { get; }
        Task<(bool Ok, string Error)> SendAsync(OutboxMessage message);
    }

    /// <summary>إشعار الواجهات بتنبيهٍ جديد (SignalR في الخادم، حدثٌ في الدسكتوب)</summary>
    public interface INotificationPush
    {
        void Notify();
    }

    public sealed class InProcessPush : INotificationPush
    {
        public event Action Notified;
        public void Notify() => Notified?.Invoke();
    }

    /// <summary>
    /// أحداث القواعد: فحوصٌ دورية مُعلَنة (Scans)، وحفظ أي صفحةٍ مسجّلة (إنشاء/تعديل/حذف) آلياً من سجلّ التدقيق —
    /// فالصفحة الجديدة يصير حفظها حدثاً بلا كتابة.
    /// </summary>
    public static class AlertEvents
    {
        public const string StockLow = "Stock.Low", NearExpiry = "Batch.NearExpiry", PaymentReceived = "Payment.Received";

        public static readonly string[] Actions = { "Insert", "Update", "Delete" };

        public static IEnumerable<(string Key, string Title)> All() =>
            new[] { StockLow, NearExpiry, PaymentReceived }.Select(e => (e, TitleOf(e)))
                .Concat(PermissionKeys.PageKeys().SelectMany(page => Actions.Select(a => ($"{page}.{a}", TitleOf($"{page}.{a}")))));

        public static string TitleOf(string key)
        {
            var dot = key.LastIndexOf('.');
            if (dot <= 0) return key;
            var (page, action) = (key[..dot], key[(dot + 1)..]);
            return Actions.Contains(action)
                ? $"{PermissionCatalog.ModuleName(page)} — {LocalizationService.Get($"Str.Alert.Action.{action}")}"
                : LocalizationService.GetOr($"Str.Alert.Event.{key}", key);
        }
    }
}
