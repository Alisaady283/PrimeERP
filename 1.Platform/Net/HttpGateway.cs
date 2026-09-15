using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace PrimeERP.Platform.Net
{
    /// <summary>
    /// الموضع الوحيد الذي يلمس الشبكة في البرنامج — التفعيل والتحديث والتنزيل يمرّون به، فلا عميل HTTP
    /// ثانٍ ولا مهلة تُضبط في موضعين.
    /// </summary>
    public interface IHttpGateway
    {
        Task<(bool Ok, string Error, JsonElement Body)> PostAsync(string url, object payload, string adminToken = null);
        Task<(bool Ok, string Error)> DownloadAsync(string url, string targetPath, IProgress<double> progress = null);
    }

    public class HttpGateway : IHttpGateway, IDisposable
    {
        // التنزيل يطول بطبعه، والسؤال القصير لا: مهلةٌ قصيرة له، وإلا جمّد خادمٌ لا يردّ الواجهةَ دقائق.
        private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(20);

        private readonly HttpClient _client = new() { Timeout = TimeSpan.FromMinutes(30) };

        public async Task<(bool Ok, string Error, JsonElement Body)> PostAsync(string url, object payload, string adminToken = null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                { Content = JsonContent.Create(payload) };

                if (!string.IsNullOrWhiteSpace(adminToken))
                    request.Headers.Add("X-Admin-Token", adminToken);

                using var cancel = new System.Threading.CancellationTokenSource(AskTimeout);

                // ConfigureAwait(false): المستدعي قد ينتظر النتيجة حاجزاً، والعودة لخيط الواجهة حينها تُعلِّقه.
                using var response = await _client.SendAsync(request, cancel.Token).ConfigureAwait(false);
                var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var body = Parse(text);

                if (response.IsSuccessStatusCode) return (true, null, body);

                var error = body.ValueKind == JsonValueKind.Object && body.TryGetProperty("error", out var message)
                    ? message.GetString()
                    : $"تعذّر الاتصال بالخادم ({(int)response.StatusCode})";

                return (false, error, body);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, default);
            }
        }

        /// <summary>تنزيلٌ بتقدّمٍ محسوب — الحزمة مئات الميجابايت، والشريط يقرأ الطول المُعلَن.</summary>
        public async Task<(bool Ok, string Error)> DownloadAsync(string url, string targetPath, IProgress<double> progress = null)
        {
            try
            {
                using var response = await _client
                    .GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return (false, $"تعذّر التنزيل ({(int)response.StatusCode})");

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
