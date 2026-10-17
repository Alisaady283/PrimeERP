using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Integration
{
    /// <summary>
    /// المُشغِّل الدوري — يبدؤه المضيف (الدسكتوب والخادم) كالنسخ الاحتياطي التلقائي:
    /// كل نصف دقيقة يرسل المستحقّ من صندوق الصادر بإعادة محاولةٍ متباعدة، وكل «فحص التنبيهات كل» يشغّل الفحوص لقواعدها.
    /// الحجز في القاعدة بجملةٍ واحدة، فلا يرسل الدسكتوب والخادم الرسالة نفسها مرتين.
    /// </summary>
    public sealed class AlertRunner : IDisposable
    {
        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(30);
        private static readonly int[] RetryMinutes = { 1, 5, 15, 60, 240 };

        private readonly Alerts _alerts;
        private readonly IOutboxRepository _outbox;
        private readonly ILookupRepository<AlertRule> _rules;
        private readonly ISettingsProvider _settings;
        private readonly IEnumerable<IAlertScan> _scans;
        private readonly Dictionary<string, IAlertChannel> _channels;
        private readonly SemaphoreSlim _busy = new(1, 1);
        private Timer _timer;
        private DateTime _lastScan = DateTime.MinValue;

        public AlertRunner(Alerts alerts, IOutboxRepository outbox, ILookupRepository<AlertRule> rules, ISettingsProvider settings,
            IEnumerable<IAlertScan> scans, IEnumerable<IAlertChannel> channels)
        {
            _alerts = alerts;
            _outbox = outbox;
            _rules = rules;
            _settings = settings;
            _scans = scans;
            _channels = channels.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        }

        public void Start()
        {
            SeedDefaults();
            _timer?.Dispose();
            _timer = new Timer(_ => _ = RunAsync(), null, TimeSpan.FromSeconds(10), Tick);
        }

        public async Task RunAsync()
        {
            if (!await _busy.WaitAsync(0).ConfigureAwait(false)) return;
            try
            {
                var every = TimeSpan.FromMinutes(Math.Max(1, _settings.Get(SettingKeys.Integration.ScanMinutes, 15)));
                if (DateTime.Now - _lastScan >= every) { Scan(); _lastScan = DateTime.Now; }
                await DispatchAsync().ConfigureAwait(false);
            }
            catch { /* لا يُسقط المضيف؛ الفشل يُسجَّل على الرسالة */ }
            finally { _busy.Release(); }
        }

        /// <summary>كل فحصٍ لقواعده بمعاملها</summary>
        public void Scan()
        {
            foreach (var scan in _scans)
                foreach (var rule in _alerts.RulesFor(scan.Event))
                    foreach (var message in scan.Find(rule))
                        _alerts.Raise(message, new[] { rule });
        }

        public async Task DispatchAsync()
        {
            foreach (var message in _outbox.Claim(DateTime.Now, 20))
            {
                var (ok, error) = _channels.TryGetValue(message.Channel, out var channel)
                    ? await channel.SendAsync(message).ConfigureAwait(false)
                    : (false, $"Unknown channel {message.Channel}");

                if (ok) _outbox.Sent(message.Id, DateTime.Now);
                else _outbox.Failed(message.Id, error, message.Attempts < RetryMinutes.Length
                    ? DateTime.Now.AddMinutes(RetryMinutes[message.Attempts]) : null);
            }
        }

        /// <summary>قاعدتا المخزون والصلاحية تبدآن مفعّلتين بالجرس</summary>
        private void SeedDefaults()
        {
            if (_rules.GetAll(includeInactive: true).Count > 0) return;

            _rules.Insert(new AlertRule { Code = "AL-1", Name = LocalizationService.Get("Str.Alert.Event.Stock.Low"), Event = AlertEvents.StockLow, Channels = Alerts.InApp });
            _rules.Insert(new AlertRule { Code = "AL-2", Name = LocalizationService.Get("Str.Alert.Event.Batch.NearExpiry"), Event = AlertEvents.NearExpiry, Channels = Alerts.InApp });
            _alerts.Invalidate();
        }

        public void Dispose() => _timer?.Dispose();
    }
}
