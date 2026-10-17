using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Net;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Integration
{
    /// <summary>ما تقوله البوابة في إشعارها بعد التحقق من توقيعه</summary>
    public sealed record PaymentCallback(bool Valid, int? PaymentId, bool Paid, decimal Amount, string Reference, string Error = null)
    {
        public static PaymentCallback Invalid(string error) => new(false, null, false, 0, null, error);
    }

    /// <summary>
    /// بوابة دفع: تُنشئ رابط الدفع لطلبٍ، وتقرأ إشعارها وتتحقق من توقيعه بمفتاحها السرّي —
    /// لا يُصدَّق إشعارٌ بلا توقيعٍ صحيح، والمبلغ يُقارن بالطلب في PaymentLinkService.
    /// </summary>
    public interface IPaymentGateway
    {
        string Name { get; }
        Task<Result<(string Url, string Reference)>> CreateAsync(PaymentRequest payment, string customerName, string customerPhone);
        PaymentCallback Read(string body, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> query);
    }

    internal static class Minor
    {
        public static long Cents(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        public static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
        public static bool Same(string a, string b) =>
            a != null && b != null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a.ToLowerInvariant()), Encoding.UTF8.GetBytes(b.ToLowerInvariant()));
        public static string Get(IReadOnlyDictionary<string, string> map, string key) =>
            map?.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
    }

    /// <summary>Stripe Checkout: جلسة دفع، والإشعار checkout.session.completed موقّعٌ في Stripe-Signature (t=…,v1=HMAC-SHA256)</summary>
    public sealed class StripeGateway : IPaymentGateway
    {
        private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(5);
        private readonly ISettingsProvider _settings;
        private readonly IHttpGateway _http;

        public StripeGateway(ISettingsProvider settings, IHttpGateway http) { _settings = settings; _http = http; }

        public string Name => "Stripe";

        public async Task<Result<(string Url, string Reference)>> CreateAsync(PaymentRequest payment, string customerName, string customerPhone)
        {
            var back = PaymentLinkService.ReturnUrl(_settings, payment);
            var form = new Dictionary<string, string>
            {
                ["mode"] = "payment",
                ["client_reference_id"] = payment.Id.ToString(),
                ["metadata[payment_id]"] = payment.Id.ToString(),
                ["line_items[0][quantity]"] = "1",
                ["line_items[0][price_data][currency]"] = payment.Currency.ToLowerInvariant(),
                ["line_items[0][price_data][unit_amount]"] = Minor.Cents(payment.Amount).ToString(CultureInfo.InvariantCulture),
                ["line_items[0][price_data][product_data][name]"] = payment.InvoiceNo,
                ["success_url"] = back, ["cancel_url"] = back,
            };
            var body = string.Join("&", form.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
            var (ok, error, json) = await _http.SendAsync(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions", body,
                new Dictionary<string, string> { ["Authorization"] = $"Bearer {_settings.Secret(SettingKeys.Payment.StripeSecretKey)}" },
                "application/x-www-form-urlencoded").ConfigureAwait(false);
            return ok ? Result.Ok((json.GetProperty("url").GetString(), json.GetProperty("id").GetString()))
                      : Result.Fail<(string, string)>(error, ErrorCode.Unexpected);
        }

        public PaymentCallback Read(string body, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> query)
        {
            var signature = Minor.Get(headers, "Stripe-Signature") ?? "";
            var parts = signature.Split(',').Select(p => p.Split('=', 2)).Where(p => p.Length == 2).ToList();
            var t = parts.FirstOrDefault(p => p[0] == "t")?[1];
            var expected = Minor.Hex(HMACSHA256.HashData(Encoding.UTF8.GetBytes(_settings.Secret(SettingKeys.Payment.StripeWebhookSecret)), Encoding.UTF8.GetBytes($"{t}.{body}")));
            if (t == null || !parts.Any(p => p[0] == "v1" && Minor.Same(p[1], expected))) return PaymentCallback.Invalid("Bad signature");
            if (!long.TryParse(t, out var at) || DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(at) > Tolerance) return PaymentCallback.Invalid("Stale event");

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.GetProperty("type").GetString() != "checkout.session.completed") return new PaymentCallback(true, null, false, 0, null);
            var session = doc.RootElement.GetProperty("data").GetProperty("object");
            return new PaymentCallback(true, int.TryParse(session.GetProperty("client_reference_id").GetString(), out var id) ? id : null,
                session.GetProperty("payment_status").GetString() == "paid", session.GetProperty("amount_total").GetInt64() / 100m,
                session.GetProperty("id").GetString());
        }
    }

    /// <summary>Paymob Accept: رمز ← طلب ← مفتاح دفع ← إطار الدفع، والإشعار موقّعٌ HMAC-SHA512 على حقولٍ مرتّبة في ?hmac=</summary>
    public sealed class PaymobGateway : IPaymentGateway
    {
        private const string Api = "https://accept.paymob.com/api";
        private static readonly string[] Signed =
        {
            "amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction", "id", "integration_id", "is_3d_secure",
            "is_auth", "is_capture", "is_refunded", "is_standalone_payment", "is_voided", "order.id", "owner", "pending",
            "source_data.pan", "source_data.sub_type", "source_data.type", "success"
        };

        private readonly ISettingsProvider _settings;
        private readonly IHttpGateway _http;

        public PaymobGateway(ISettingsProvider settings, IHttpGateway http) { _settings = settings; _http = http; }

        public string Name => "Paymob";

        public async Task<Result<(string Url, string Reference)>> CreateAsync(PaymentRequest payment, string customerName, string customerPhone)
        {
            var auth = await Post("/auth/tokens", new { api_key = _settings.Secret(SettingKeys.Payment.PaymobApiKey) }).ConfigureAwait(false);
            if (auth.IsFailure) return auth.As<(string, string)>();
            var token = auth.Value.GetProperty("token").GetString();

            var cents = Minor.Cents(payment.Amount);
            var order = await Post("/ecommerce/orders", new
            {
                auth_token = token, delivery_needed = false, amount_cents = cents, currency = payment.Currency,
                merchant_order_id = payment.Id.ToString(), items = Array.Empty<object>()
            }).ConfigureAwait(false);
            if (order.IsFailure) return order.As<(string, string)>();
            var orderId = order.Value.GetProperty("id").GetInt64();

            var key = await Post("/acceptance/payment_keys", new
            {
                auth_token = token, amount_cents = cents, expiration = 3600, order_id = orderId, currency = payment.Currency,
                integration_id = int.TryParse(_settings.Get(SettingKeys.Payment.PaymobIntegrationId, ""), out var integration) ? integration : 0,
                billing_data = new
                {
                    first_name = customerName ?? "NA", last_name = "NA", email = "NA@NA.com", phone_number = customerPhone ?? "NA",
                    apartment = "NA", floor = "NA", street = "NA", building = "NA", shipping_method = "NA", postal_code = "NA",
                    city = "NA", country = "EG", state = "NA"
                }
            }).ConfigureAwait(false);
            if (key.IsFailure) return key.As<(string, string)>();

            return Result.Ok(($"{Api}/acceptance/iframes/{_settings.Get(SettingKeys.Payment.PaymobIframeId, "")}?payment_token={key.Value.GetProperty("token").GetString()}",
                orderId.ToString(CultureInfo.InvariantCulture)));
        }

        public PaymentCallback Read(string body, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> query)
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("obj", out var obj)) return PaymentCallback.Invalid("No transaction");

            var concatenated = string.Concat(Signed.Select(path => Text(obj, path)));
            var expected = Minor.Hex(HMACSHA512.HashData(Encoding.UTF8.GetBytes(_settings.Secret(SettingKeys.Payment.PaymobHmac)), Encoding.UTF8.GetBytes(concatenated)));
            if (!Minor.Same(Minor.Get(query, "hmac"), expected)) return PaymentCallback.Invalid("Bad signature");

            var order = obj.GetProperty("order");
            var merchant = order.TryGetProperty("merchant_order_id", out var m) ? m.ToString() : null;
            return new PaymentCallback(true, int.TryParse(merchant, out var id) ? id : null,
                obj.GetProperty("success").GetBoolean() && !obj.GetProperty("pending").GetBoolean(),
                obj.GetProperty("amount_cents").GetInt64() / 100m, Text(obj, "id"));
        }

        /// <summary>القيمة كما تُوقَّع: المنطقية بحروفٍ صغيرة</summary>
        public static string Text(JsonElement root, string path)
        {
            var node = root;
            foreach (var part in path.Split('.'))
                if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(part, out node)) return "";
            return node.ValueKind switch
            {
                JsonValueKind.True => "true", JsonValueKind.False => "false", JsonValueKind.Null => "",
                JsonValueKind.String => node.GetString(), _ => node.GetRawText()
            };
        }

        private async Task<Result<JsonElement>> Post(string path, object payload)
        {
            var (ok, error, json) = await _http.SendAsync(HttpMethod.Post, Api + path, JsonSerializer.Serialize(payload)).ConfigureAwait(false);
            return ok ? Result.Ok(json) : Result.Fail<JsonElement>(error, ErrorCode.Unexpected);
        }
    }

    /// <summary>FawryPay: رابط الدفع بتوقيع SHA-256، والإشعار V2 موقّعٌ في messageSignature</summary>
    public sealed class FawryGateway : IPaymentGateway
    {
        private readonly ISettingsProvider _settings;
        private readonly IHttpGateway _http;

        public FawryGateway(ISettingsProvider settings, IHttpGateway http) { _settings = settings; _http = http; }

        public string Name => "Fawry";

        private string Base => _settings.Get(SettingKeys.Payment.FawrySandbox, true) ? "https://atfawry.fawrystaging.com" : "https://www.atfawry.com";

        public async Task<Result<(string Url, string Reference)>> CreateAsync(PaymentRequest payment, string customerName, string customerPhone)
        {
            var merchant = _settings.Get(SettingKeys.Payment.FawryMerchantCode, "");
            var reference = payment.Id.ToString();
            var price = payment.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            var back = PaymentLinkService.ReturnUrl(_settings, payment);
            var signature = Minor.Hex(SHA256.HashData(Encoding.UTF8.GetBytes(
                merchant + reference + "" + back + payment.InvoiceNo + "1" + price + _settings.Secret(SettingKeys.Payment.FawrySecureKey))));

            var (ok, error, json) = await _http.SendAsync(HttpMethod.Post, $"{Base}/fawrypay-api/api/payments/init", JsonSerializer.Serialize(new
            {
                merchantCode = merchant, merchantRefNum = reference, customerName, customerMobile = customerPhone, language = "ar-eg",
                chargeItems = new[] { new { itemId = payment.InvoiceNo, description = payment.InvoiceNo, price = payment.Amount, quantity = 1 } },
                returnUrl = back, authCaptureModePayment = false, signature
            })).ConfigureAwait(false);
            var url = json.ValueKind == JsonValueKind.String ? json.GetString() : null;
            return ok && url != null ? Result.Ok((url, reference)) : Result.Fail<(string, string)>(error ?? "Fawry", ErrorCode.Unexpected);
        }

        public PaymentCallback Read(string body, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> query)
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            string S(string name) => root.TryGetProperty(name, out var v) ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText()) : "";
            string Money(string name) => decimal.TryParse(S(name), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d.ToString("0.00", CultureInfo.InvariantCulture) : "";

            var expected = Minor.Hex(SHA256.HashData(Encoding.UTF8.GetBytes(S("fawryRefNumber") + S("merchantRefNumber") + Money("paymentAmount") + Money("orderAmount")
                + S("orderStatus") + S("paymentMethod") + S("paymentRefrenceNumber") + _settings.Secret(SettingKeys.Payment.FawrySecureKey))));
            if (!Minor.Same(S("messageSignature"), expected)) return PaymentCallback.Invalid("Bad signature");

            return new PaymentCallback(true, int.TryParse(S("merchantRefNumber"), out var id) ? id : null, S("orderStatus") == "PAID",
                decimal.TryParse(S("paymentAmount"), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ? amount : 0, S("fawryRefNumber"));
        }
    }
}
