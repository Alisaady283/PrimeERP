using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Tests.Helpers;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Integration;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Net;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Security;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>التنبيهات والقنوات وصندوق الصادر والقيم السرّية</summary>
    public class AlertTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public AlertTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private T Service<T>() => _db.Services.GetRequiredService<T>();

        private void Rule(string @event, string channel, string recipients = null, decimal threshold = 0)
        {
            Service<ILookupRepository<AlertRule>>().Insert(new AlertRule
            {
                Code = Guid.NewGuid().ToString("N")[..6], Name = @event, Event = @event, Channels = channel, Recipients = recipients, Threshold = threshold
            });
            Service<Alerts>().Invalidate();
        }

        private static int Count<T>(Func<IQueryable<T>, IQueryable<T>> shape) where T : class
        {
            using var db = DbContextFactory.Open();
            return shape(db.Set<T>()).Count();
        }

        [Fact]
        public void ASecret_IsSealed_AndOpensBackToItself()
        {
            var sealedValue = SecretBox.Seal("p@ss-123");

            Assert.True(SecretBox.IsSealed(sealedValue));
            Assert.DoesNotContain("p@ss-123", sealedValue);
            Assert.Equal("p@ss-123", SecretBox.Open(sealedValue));
            Assert.Equal(sealedValue, SecretBox.Seal(sealedValue));
        }

        [Fact]
        public void ASecretSetting_IsStoredSealed_AndABlankSaveKeepsIt()
        {
            var settings = Service<ISettingsService>();
            Assert.True(settings.SetMany(new Dictionary<string, object> { [SettingKeys.Integration.EmailPassword] = "app-password" }).IsSuccess);

            var provider = Service<ISettingsProvider>();
            Assert.True(SecretBox.IsSealed(provider.Get(SettingKeys.Integration.EmailPassword, "")), "حُفظ السرّ مكشوفاً");
            Assert.Equal("app-password", provider.Secret(SettingKeys.Integration.EmailPassword));

            settings.SetMany(new Dictionary<string, object> { [SettingKeys.Integration.EmailPassword] = "" });
            Assert.Equal("app-password", provider.Secret(SettingKeys.Integration.EmailPassword));
        }

        [Fact]
        public void AnEvent_MakesOneNotification_AndAMessagePerRecipient_OnceForItsDedupKey()
        {
            Rule("Test.Event", Alerts.InApp);
            Rule("Test.Event", Alerts.Webhook, "https://a.example/hook, https://b.example/hook");

            var message = new AlertMessage("Test.Event", "عنوان", "نص", DedupKey: "Test.Event:1");
            Service<Alerts>().Raise(message);
            Service<Alerts>().Raise(message);

            Assert.Equal(1, Count<Notification>(q => q.Where(n => n.Event == "Test.Event")));
            Assert.Equal(2, Count<OutboxMessage>(q => q.Where(m => m.Event == "Test.Event" && m.Channel == Alerts.Webhook)));
        }

        [Fact]
        public void AnEventWithoutARule_LeavesNoTrace()
        {
            Service<Alerts>().Raise(new AlertMessage("Nobody.Listens", "t", "b"));

            Assert.Equal(0, Count<Notification>(q => q.Where(n => n.Event == "Nobody.Listens")));
            Assert.Equal(0, Count<OutboxMessage>(q => q.Where(m => m.Event == "Nobody.Listens")));
        }

        [Fact]
        public void SavingAPage_RaisesItsEvent_ThroughTheAuditLog()
        {
            Rule("Warehouses.Insert", Alerts.InApp);

            var id = Service<Lookup<Warehouse>>().Add("مخزن التنبيه");

            Assert.Equal(1, Count<Notification>(q => q.Where(n => n.Event == "Warehouses.Insert" && n.RecordId == id && n.PermissionKey == "Warehouses.View")));
        }

        [Fact]
        public void ANotification_ReachesOnlyWhoMayViewItsPage()
        {
            Service<INotificationRepository>().Insert(new Notification { Event = "E", Title = "t", Body = "b", PermissionKey = "Secret.View" });

            using (AppSession.Enter())
            {
                AppSession.SignIn(7, "u", "u", 1, "r", new[] { "Other.View" });
                Assert.Equal(0, Service<INotificationService>().UnreadCount().Value);
            }

            using (AppSession.Enter())
            {
                AppSession.SignIn(8, "v", "v", 1, "r", new[] { "Secret.View" });
                var notifications = Service<INotificationService>();
                Assert.Equal(1, notifications.UnreadCount().Value);

                notifications.MarkRead(notifications.Mine().Value.Select(n => n.Id));
                Assert.Equal(0, notifications.UnreadCount().Value);
            }
        }

        [Fact]
        public void TheStockLowScan_FlagsOnlyItemsAtOrBelowTheirReorderPoint()
        {
            var products = Service<IProductService>();
            var low = products.Create(new Product { Name = "صنف ناقص", CostPrice = 1, SalePrice = 2, ReorderPoint = 50 }).Value.Id;
            var enough = products.Create(new Product { Name = "صنف كافٍ", CostPrice = 1, SalePrice = 2, ReorderPoint = 50 }).Value.Id;
            var warehouse = Service<Lookup<Warehouse>>().Add("مخزن الفحص");

            var stock = Service<IStockMove>();
            DbContextFactory.RunTransaction(db => stock.RecordMovement(db, low, warehouse, MovementType.In, 30, 1, "Test", null, "T"));
            DbContextFactory.RunTransaction(db => stock.RecordMovement(db, enough, warehouse, MovementType.In, 70, 1, "Test", null, "T"));

            var flagged = Service<IEnumerable<IAlertScan>>().OfType<StockLowScan>().Single()
                .Find(new AlertRule { Event = AlertEvents.StockLow }).Select(m => m.RecordId).ToList();

            Assert.Contains(low, flagged);
            Assert.DoesNotContain(enough, flagged);
        }

        [Fact]
        public async Task AWebhook_IsSignedWithHmacOverTheExactBody()
        {
            Service<ISettingsService>().SetMany(new Dictionary<string, object> { [SettingKeys.Integration.WebhookSecret] = "shh" });
            var http = new RecordingGateway();
            var channel = new WebhookChannel(Service<ISettingsProvider>(), http);

            var (ok, _) = await channel.SendAsync(new OutboxMessage { Id = 5, Event = "E", Target = "https://n8n.example/webhook/x", Body = "{\"a\":1}" });

            Assert.True(ok);
            var expected = "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("shh"), Encoding.UTF8.GetBytes("{\"a\":1}"))).ToLowerInvariant();
            Assert.Equal(expected, http.Headers["X-PrimeERP-Signature"]);
            Assert.Equal("{\"a\":1}", http.Body);
        }

        [Fact]
        public async Task TheDispatcher_SendsWhatSucceeds_AndRetriesWhatFails()
        {
            var outbox = Service<IOutboxRepository>();
            var good = outbox.Enqueue(new OutboxMessage { Event = "E", Channel = "Fake", Target = "ok", Subject = "s", Body = "b", NextAttemptAt = DateTime.Now.AddMinutes(-1) });
            var bad = outbox.Enqueue(new OutboxMessage { Event = "E", Channel = "Fake", Target = "fail", Subject = "s", Body = "b", NextAttemptAt = DateTime.Now.AddMinutes(-1) });

            var runner = new AlertRunner(Service<Alerts>(), outbox, Service<ILookupRepository<AlertRule>>(), Service<ISettingsProvider>(),
                Array.Empty<IAlertScan>(), new IAlertChannel[] { new FakeChannel() });
            await runner.DispatchAsync();

            using var db = DbContextFactory.Open();
            var sent = db.Set<OutboxMessage>().Single(m => m.Id == good);
            var retried = db.Set<OutboxMessage>().Single(m => m.Id == bad);
            Assert.Equal(OutboxStatus.Sent, sent.Status);
            Assert.Equal(OutboxStatus.Pending, retried.Status);
            Assert.Equal(1, retried.Attempts);
            Assert.True(retried.NextAttemptAt > DateTime.Now, "أُعيد فوراً بلا مهلة");
            Assert.Equal("refused", retried.LastError);
        }

        private sealed class FakeChannel : IAlertChannel
        {
            public string Name => "Fake";
            public Task<(bool Ok, string Error)> SendAsync(OutboxMessage message) =>
                Task.FromResult(message.Target == "ok" ? (true, (string)null) : (false, "refused"));
        }

        private sealed class RecordingGateway : IHttpGateway
        {
            public IReadOnlyDictionary<string, string> Headers { get; private set; }
            public string Body { get; private set; }

            public Task<(bool Ok, string Error, JsonElement Body)> PostAsync(string url, object payload, string adminToken = null) =>
                throw new NotSupportedException();

            public Task<(bool Ok, string Error, JsonElement Body)> SendAsync(HttpMethod method, string url, string body = null,
                IReadOnlyDictionary<string, string> headers = null, string contentType = "application/json")
            {
                Headers = headers;
                Body = body;
                return Task.FromResult((true, (string)null, default(JsonElement)));
            }

            public Task<(bool Ok, string Error)> DownloadAsync(string url, string targetPath, IProgress<double> progress = null) =>
                throw new NotSupportedException();
        }
    }
}
