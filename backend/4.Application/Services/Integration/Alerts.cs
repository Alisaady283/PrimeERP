using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Services.Integration
{
    /// <summary>
    /// الحدث ← قواعده النشطة: «InApp» تنبيهٌ داخلي واحد (لا يتكرر بمفتاحه)، وكل قناةٍ خارجية رسالةٌ لكل مستلم في صندوق الصادر.
    /// لا يرسل هنا شيئاً: الإرسال وإعادة المحاولة في AlertRunner.
    /// </summary>
    public sealed class Alerts
    {
        public const string InApp = "InApp", Webhook = "Webhook";

        private static readonly TimeSpan RulesLifetime = TimeSpan.FromMinutes(1);

        private readonly ILookupRepository<AlertRule> _rules;
        private readonly INotificationRepository _notifications;
        private readonly IOutboxRepository _outbox;
        private readonly INotificationPush _push;
        private readonly object _lock = new();
        private (DateTime At, List<AlertRule> Rules) _cache;

        public Alerts(ILookupRepository<AlertRule> rules, INotificationRepository notifications, IOutboxRepository outbox, INotificationPush push)
        {
            _rules = rules;
            _notifications = notifications;
            _outbox = outbox;
            _push = push;
        }

        public List<AlertRule> RulesFor(string @event) => Active().Where(r => r.Event == @event).ToList();

        public bool Wanted(string @event) => Active().Any(r => r.Event == @event);

        public void Raise(AlertMessage message) => Raise(message, RulesFor(message.Event));

        /// <summary>القواعد ممرّرة: الفحص يقيس بمعامل قاعدته</summary>
        public void Raise(AlertMessage message, IReadOnlyCollection<AlertRule> rules)
        {
            if (rules.Count == 0) return;
            // ما أُبلغ به اليوم لا يُعاد في أي قناة
            if (message.DedupKey != null && (_notifications.Exists(message.DedupKey) || _outbox.Exists(message.DedupKey))) return;

            if (rules.Any(r => ChannelsOf(r).Contains(InApp)))
            {
                _notifications.Insert(new Notification
                {
                    Event = message.Event, Title = message.Title, Body = message.Body, PageKey = message.PageKey,
                    RecordId = message.RecordId, PermissionKey = message.PermissionKey, Severity = message.Severity, DedupKey = message.DedupKey
                });
                _push.Notify();
            }

            foreach (var rule in rules)
                foreach (var channel in ChannelsOf(rule).Where(c => c != InApp))
                    foreach (var target in TargetsOf(rule))
                        _outbox.Enqueue(new OutboxMessage
                        {
                            RuleId = rule.Id, Event = message.Event, Channel = channel, Target = target,
                            Subject = message.Title, Body = channel == Webhook ? Payload(message) : $"{message.Title}\n{message.Body}",
                            NextAttemptAt = DateTime.Now, DedupKey = message.DedupKey
                        });
        }

        public void Invalidate() { lock (_lock) _cache = default; }

        private List<AlertRule> Active()
        {
            lock (_lock)
            {
                if (_cache.Rules == null || DateTime.Now - _cache.At > RulesLifetime)
                    _cache = (DateTime.Now, _rules.GetAll().Where(r => r.IsActive).ToList());
                return _cache.Rules;
            }
        }

        public static HashSet<string> ChannelsOf(AlertRule rule) =>
            Split(rule.Channels).ToHashSet(StringComparer.OrdinalIgnoreCase);

        private static IEnumerable<string> TargetsOf(AlertRule rule) => Split(rule.Recipients);

        private static IEnumerable<string> Split(string text) =>
            (text ?? "").Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        /// <summary>جسم Webhook: JSON ثابت الشكل لأي نظامٍ يستقبل (n8n وغيره)</summary>
        private static string Payload(AlertMessage m) => JsonSerializer.Serialize(new
        {
            @event = m.Event, title = m.Title, body = m.Body, page = m.PageKey, recordId = m.RecordId, severity = m.Severity, at = DateTime.UtcNow
        });
    }
}
