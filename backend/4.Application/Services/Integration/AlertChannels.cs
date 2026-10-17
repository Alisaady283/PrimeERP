using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Net;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Integration
{
    /// <summary>بريدٌ عبر SMTP (Gmail بكلمة مرور تطبيق، أو أي خادم)</summary>
    public sealed class EmailChannel : IAlertChannel
    {
        private readonly ISettingsProvider _settings;
        public EmailChannel(ISettingsProvider settings) => _settings = settings;

        public string Name => "Email";

        public async Task<(bool Ok, string Error)> SendAsync(OutboxMessage message)
        {
            if (!_settings.Get(SettingKeys.Integration.EmailEnabled, false)) return (false, "Email disabled");
            try
            {
                var user = _settings.Get(SettingKeys.Integration.EmailUser, "");
                using var client = new SmtpClient(_settings.Get(SettingKeys.Integration.EmailHost, ""), _settings.Get(SettingKeys.Integration.EmailPort, 587))
                {
                    EnableSsl = _settings.Get(SettingKeys.Integration.EmailSsl, true),
                    Credentials = new NetworkCredential(user, _settings.Secret(SettingKeys.Integration.EmailPassword))
                };
                var from = _settings.Get(SettingKeys.Integration.EmailFrom, "");
                using var mail = new MailMessage(string.IsNullOrWhiteSpace(from) ? user : from, message.Target, message.Subject, message.Body)
                { BodyEncoding = Encoding.UTF8, SubjectEncoding = Encoding.UTF8 };
                await client.SendMailAsync(mail).ConfigureAwait(false);
                return (true, null);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
    }

    /// <summary>واتساب عبر WhatsApp Cloud API (Meta) — رسالةٌ نصّية</summary>
    public sealed class WhatsAppChannel : IAlertChannel
    {
        private const string Graph = "https://graph.facebook.com/v20.0";

        private readonly ISettingsProvider _settings;
        private readonly IHttpGateway _http;

        public WhatsAppChannel(ISettingsProvider settings, IHttpGateway http)
        {
            _settings = settings;
            _http = http;
        }

        public string Name => "WhatsApp";

        public async Task<(bool Ok, string Error)> SendAsync(OutboxMessage message)
        {
            if (!_settings.Get(SettingKeys.Integration.WhatsAppEnabled, false)) return (false, "WhatsApp disabled");

            var body = JsonSerializer.Serialize(new
            {
                messaging_product = "whatsapp", to = message.Target, type = "text", text = new { body = message.Body }
            });
            var headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {_settings.Secret(SettingKeys.Integration.WhatsAppToken)}" };
            var (ok, error, _) = await _http.SendAsync(HttpMethod.Post,
                $"{Graph}/{_settings.Get(SettingKeys.Integration.WhatsAppPhoneId, "")}/messages", body, headers).ConfigureAwait(false);
            return (ok, error);
        }
    }

    /// <summary>
    /// Webhook: JSON إلى العنوان (n8n · Zapier · نظامٌ خاص)، موقّعٌ بـHMAC-SHA256 على الجسم نفسه في
    /// X-PrimeERP-Signature: sha256=… — فيتحقّق المستقبِل أن المرسِل النظام وأن الجسم لم يتغيّر.
    /// </summary>
    public sealed class WebhookChannel : IAlertChannel
    {
        private readonly ISettingsProvider _settings;
        private readonly IHttpGateway _http;

        public WebhookChannel(ISettingsProvider settings, IHttpGateway http)
        {
            _settings = settings;
            _http = http;
        }

        public string Name => Alerts.Webhook;

        public async Task<(bool Ok, string Error)> SendAsync(OutboxMessage message)
        {
            if (!Uri.TryCreate(message.Target, UriKind.Absolute, out var url) || url.Scheme is not ("https" or "http"))
                return (false, "Invalid URL");

            var headers = new Dictionary<string, string> { ["X-PrimeERP-Event"] = message.Event, ["X-PrimeERP-Delivery"] = message.Id.ToString() };
            var secret = _settings.Secret(SettingKeys.Integration.WebhookSecret);
            if (!string.IsNullOrEmpty(secret)) headers["X-PrimeERP-Signature"] = "sha256=" + Sign(secret, message.Body);

            var (ok, error, _) = await _http.SendAsync(HttpMethod.Post, url.ToString(), message.Body, headers).ConfigureAwait(false);
            return (ok, error);
        }

        public static string Sign(string secret, string body) =>
            Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body ?? ""))).ToLowerInvariant();
    }
}
