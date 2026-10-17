using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Platform.Net
{
    /// <summary>الموضع الوحيد الذي يلمس الشبكة</summary>
    public interface IHttpGateway
    {
        Task<(bool Ok, string Error, JsonElement Body)> PostAsync(string url, object payload, string adminToken = null);
        Task<(bool Ok, string Error, JsonElement Body)> SendAsync(HttpMethod method, string url, string body = null,
            IReadOnlyDictionary<string, string> headers = null, string contentType = "application/json");
        Task<(bool Ok, string Error)> DownloadAsync(string url, string targetPath, IProgress<double> progress = null);
    }

    public class HttpGateway : IHttpGateway, IDisposable
    {
        private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(20);

        private readonly HttpClient _client = new() { Timeout = TimeSpan.FromMinutes(30) };
        private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

        public Task<(bool Ok, string Error, JsonElement Body)> PostAsync(string url, object payload, string adminToken = null) =>
            SendAsync(HttpMethod.Post, url, JsonSerializer.Serialize(payload, Web),
                string.IsNullOrWhiteSpace(adminToken) ? null : new Dictionary<string, string> { ["X-Admin-Token"] = adminToken });

        /// <summary>طلبٌ بجسمٍ نصّي وترويسات (التوقيع يُحسب على النص نفسه)</summary>
        public async Task<(bool Ok, string Error, JsonElement Body)> SendAsync(HttpMethod method, string url, string body = null,
            IReadOnlyDictionary<string, string> headers = null, string contentType = "application/json")
        {
            try
            {
                using var request = new HttpRequestMessage(method, url);
                if (body != null) request.Content = new StringContent(body, System.Text.Encoding.UTF8, contentType);

                foreach (var (name, value) in headers ?? new Dictionary<string, string>())
                    if (!request.Headers.TryAddWithoutValidation(name, value)) request.Content?.Headers.TryAddWithoutValidation(name, value);

                using var cancel = new System.Threading.CancellationTokenSource(AskTimeout);

                using var response = await _client.SendAsync(request, cancel.Token).ConfigureAwait(false);
                var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var parsed = Parse(text);

                if (response.IsSuccessStatusCode) return (true, null, parsed);

                var error = parsed.ValueKind == JsonValueKind.Object && parsed.TryGetProperty("error", out var message)
                    ? message.GetString()
                    : LocalizationService.Get("Str.Net.ServerFailed", (int)response.StatusCode);

                return (false, error, parsed);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, default);
            }
        }

        public async Task<(bool Ok, string Error)> DownloadAsync(string url, string targetPath, IProgress<double> progress = null)
        {
            try
            {
                using var response = await _client
                    .GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return (false, LocalizationService.Get("Str.Net.DownloadFailed", (int)response.StatusCode));

                var total = response.Content.Headers.ContentLength ?? 0;
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? ".");

                await using var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await using var target = File.Create(targetPath);

                var buffer = new byte[81920];
                long done = 0;
                int read;

                while ((read = await source.ReadAsync(buffer).ConfigureAwait(false)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
                    done += read;

                    if (total > 0) progress?.Report(done * 100d / total);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private static JsonElement Parse(string text)
        {
            try { return JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text).RootElement.Clone(); }
            catch { return default; }
        }

        public void Dispose() => _client.Dispose();
    }
}
