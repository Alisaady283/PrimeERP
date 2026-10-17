using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.PageServices.Parties;
using PrimeERP.Application.PageServices.Treasury;
using PrimeERP.Application.PageServices.Vouchers;
using PrimeERP.Application.Services.Integration;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Net;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>بوابات الدفع: لا يُصدَّق إشعارٌ بلا توقيعه، والتأكيد يُنشئ سند القبض مرةً واحدة ويسدّد الفاتورة</summary>
    public class PaymentTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public PaymentTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private T Service<T>() => _db.Services.GetRequiredService<T>();

        private void Set(string key, object value) => Service<ISettingsService>().SetMany(new Dictionary<string, object> { [key] = value });

        private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

        [Fact]
        public void Stripe_AcceptsItsSignature_AndRefusesATamperedOrStaleEvent()
        {
            Set(SettingKeys.Payment.StripeWebhookSecret, "whsec_test");
            var stripe = new StripeGateway(Service<ISettingsProvider>(), Service<IHttpGateway>());
            var body = "{\"type\":\"checkout.session.completed\",\"data\":{\"object\":{\"id\":\"cs_1\",\"client_reference_id\":\"42\",\"payment_status\":\"paid\",\"amount_total\":12550}}}";

            Dictionary<string, string> Signed(string payload, long t) => new()
            {
                ["Stripe-Signature"] = $"t={t},v1={Hex(HMACSHA256.HashData(Encoding.UTF8.GetBytes("whsec_test"), Encoding.UTF8.GetBytes($"{t}.{payload}")))}"
            };
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var read = stripe.Read(body, Signed(body, now), new Dictionary<string, string>());
            Assert.True(read.Valid, read.Error);
            Assert.Equal((42, true, 125.50m, "cs_1"), (read.PaymentId, read.Paid, read.Amount, read.Reference));

            Assert.False(stripe.Read(body.Replace("12550", "99999"), Signed(body, now), new Dictionary<string, string>()).Valid);
            Assert.False(stripe.Read(body, Signed(body, now - 3600), new Dictionary<string, string>()).Valid);
        }

        [Fact]
        public void Paymob_ChecksTheHmacOverItsOrderedFields()
        {
            Set(SettingKeys.Payment.PaymobHmac, "hmac_test");
            var paymob = new PaymobGateway(Service<ISettingsProvider>(), Service<IHttpGateway>());
            var body = "{\"type\":\"TRANSACTION\",\"obj\":{\"amount_cents\":10000,\"created_at\":\"2026-10-16T10:00:00\",\"currency\":\"EGP\",\"error_occured\":false," +
                       "\"has_parent_transaction\":false,\"id\":777,\"integration_id\":5,\"is_3d_secure\":true,\"is_auth\":false,\"is_capture\":false," +
                       "\"is_refunded\":false,\"is_standalone_payment\":true,\"is_voided\":false,\"order\":{\"id\":99,\"merchant_order_id\":\"7\"},\"owner\":1," +
                       "\"pending\":false,\"source_data\":{\"pan\":\"2346\",\"sub_type\":\"MasterCard\",\"type\":\"card\"},\"success\":true}}";
            using var doc = JsonDocument.Parse(body);
            var obj = doc.RootElement.GetProperty("obj");
            var concatenated = string.Concat(new[] { "amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction", "id", "integration_id",
                "is_3d_secure", "is_auth", "is_capture", "is_refunded", "is_standalone_payment", "is_voided", "order.id", "owner", "pending",
                "source_data.pan", "source_data.sub_type", "source_data.type", "success" }.Select(p => PaymobGateway.Text(obj, p)));
            Assert.StartsWith("10000" + "2026-10-16T10:00:00" + "EGP" + "false", concatenated);
            var hmac = Hex(HMACSHA512.HashData(Encoding.UTF8.GetBytes("hmac_test"), Encoding.UTF8.GetBytes(concatenated)));

            var read = paymob.Read(body, new Dictionary<string, string>(), new Dictionary<string, string> { ["hmac"] = hmac });
            Assert.True(read.Valid, read.Error);
            Assert.Equal((7, true, 100m), (read.PaymentId, read.Paid, read.Amount));
            Assert.False(paymob.Read(body, new Dictionary<string, string>(), new Dictionary<string, string> { ["hmac"] = "00" }).Valid);
        }

        [Fact]
        public void Fawry_ChecksTheMessageSignature()
        {
            Set(SettingKeys.Payment.FawrySecureKey, "secure");
            var fawry = new FawryGateway(Service<ISettingsProvider>(), Service<IHttpGateway>());
            var signature = Hex(SHA256.HashData(Encoding.UTF8.GetBytes("FR1" + "9" + "50.00" + "50.00" + "PAID" + "CARD" + "" + "secure")));
            string Body(string sig) => $"{{\"fawryRefNumber\":\"FR1\",\"merchantRefNumber\":\"9\",\"paymentAmount\":50,\"orderAmount\":50,\"orderStatus\":\"PAID\",\"paymentMethod\":\"CARD\",\"messageSignature\":\"{sig}\"}}";

            var read = fawry.Read(Body(signature), new Dictionary<string, string>(), new Dictionary<string, string>());
            Assert.True(read.Valid, read.Error);
            Assert.Equal((9, true, 50m), (read.PaymentId, read.Paid, read.Amount));
            Assert.False(fawry.Read(Body("bad"), new Dictionary<string, string>(), new Dictionary<string, string>()).Valid);
        }

        [Fact]
        public void AConfirmedPayment_MakesOneReceipt_AndSettlesTheInvoice()
        {
            var (invoiceId, paymentId) = Seed(amount: 80);
            var payments = Payments(new FakeGateway(paymentId, paid: 80));

            Assert.True(payments.Confirm("Fake", "{}", new Dictionary<string, string>(), new Dictionary<string, string>()).IsSuccess);
            Assert.True(payments.Confirm("Fake", "{}", new Dictionary<string, string>(), new Dictionary<string, string>()).IsSuccess);

            var payment = Service<IPaymentRepository>().GetById(paymentId);
            Assert.Equal(PaymentStatus.Paid, payment.Status);
            Assert.NotNull(payment.VoucherId);

            using var db = DbContextFactory.Open();
            Assert.Equal(1, db.Set<Voucher>().Count(v => v.Reference == "Fake:ref-1"));
            Assert.Equal(80m, Service<IInvoiceRepository<SalesInvoice, SalesInvoiceLine>>().GetById(invoiceId).PaidAmount);
        }

        [Fact]
        public void AnUnderpaidOrForgedCallback_ChangesNothing()
        {
            var (_, paymentId) = Seed(amount: 80);

            Assert.True(Payments(new FakeGateway(paymentId, paid: 50)).Confirm("Fake", "{}", new Dictionary<string, string>(), new Dictionary<string, string>()).IsFailure);
            Assert.True(Payments(new FakeGateway(paymentId, paid: 80, valid: false)).Confirm("Fake", "{}", new Dictionary<string, string>(), new Dictionary<string, string>()).IsFailure);

            Assert.Equal(PaymentStatus.Pending, Service<IPaymentRepository>().GetById(paymentId).Status);
        }

        private PaymentLinkService Payments(IPaymentGateway gateway) => new(Service<IPaymentRepository>(), Service<IInvoiceRepository<SalesInvoice, SalesInvoiceLine>>(),
            Service<IPartyRepository<Customer>>(), Service<IReceiptVoucherService>(), Service<IPermissionService>(), _db.Permissions,
            Service<ISettingsProvider>(), Service<Alerts>(), new[] { gateway });

        private (int InvoiceId, int PaymentId) Seed(decimal amount)
        {
            var parentId = Service<IAccountRepository>().GetByCode("12").Id;
            var account = Service<IAccountService>().Create(new CreateAccountDto { ParentId = parentId, Name = $"بنك {Guid.NewGuid():N}", IsLeaf = true });
            var treasury = Service<ITreasuryService>().Create(new Treasury { Name = $"بنك {Guid.NewGuid():N}", AccountCode = account.Value.Code }).Value;
            var customer = Service<ICustomerService>().Create(new Customer { Name = $"عميل {Guid.NewGuid():N}" }).Value;
            Set(SettingKeys.Payment.TreasuryId, treasury.Id.ToString(CultureInfo.InvariantCulture));

            var invoiceNo = $"T{Guid.NewGuid():N}"[..12];
            var invoiceId = Service<IInvoiceRepository<SalesInvoice, SalesInvoiceLine>>().InsertHeader(null, new SalesInvoice
            {
                InvoiceNo = invoiceNo, InvoiceDate = DateTime.Today, DueDate = DateTime.Today, CustomerId = customer.Id,
                SubTotal = amount, NetTotal = amount, Status = InvoiceStatus.Confirmed
            });
            var admin = _db.Permissions.FindByUsername("admin");
            var paymentId = Service<IPaymentRepository>().Insert(new PaymentRequest
            {
                Provider = "Fake", InvoiceId = invoiceId, InvoiceNo = invoiceNo, CustomerId = customer.Id, Amount = amount, Currency = "EGP", RequestedBy = admin.Id
            });
            return (invoiceId, paymentId);
        }

        private sealed class FakeGateway : IPaymentGateway
        {
            private readonly int _paymentId;
            private readonly decimal _paid;
            private readonly bool _valid;

            public FakeGateway(int paymentId, decimal paid, bool valid = true) { _paymentId = paymentId; _paid = paid; _valid = valid; }

            public string Name => "Fake";

            public Task<Result<(string Url, string Reference)>> CreateAsync(PaymentRequest payment, string customerName, string customerPhone) =>
                Task.FromResult(Result.Ok(("https://pay.example/1", "ref-1")));

            public PaymentCallback Read(string body, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> query) =>
                _valid ? new PaymentCallback(true, _paymentId, true, _paid, "ref-1") : PaymentCallback.Invalid("Bad signature");
        }
    }
}
