using System;
using System.IO;
using System.Threading.Tasks;
using PrimeERP.Domain.Results;
using PrimeERP.Platform;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Net;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services
{
    public record UpdateAvailability(bool Available, string Version, string PackageUrl);

    public interface IUpdateService
    {
        Task<Result<UpdateAvailability>> CheckAsync();
        Task<Result<string>> DownloadAsync(UpdateAvailability update, IProgress<double> progress = null);
    }

    /// <summary>
    /// التحديث سؤالٌ واحد: أعندك أحدث ممّا أُشغّل؟ الخادم يقارن المرفوع بالمُشغَّل — فما دام الإصدار
    /// نفسه لا يحدث شيء. والتنزيل بنفس بوّابة الشبكة التي يستعملها التفعيل.
    /// </summary>
    public class UpdateService : ServiceBase, IUpdateService
    {
        private readonly IHttpGateway _http;

        public UpdateService(IHttpGateway http, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) => _http = http;

        protected override string PermissionPrefix => "Settings";
        protected override string StringPrefix => "Str.Settings";
        protected override string EntityName => "Update";

        public async Task<Result<UpdateAvailability>> CheckAsync()
        {
            var server = Setting(SettingKeys.Developer.ServerUrl, "").TrimEnd('/');
            var serial = Setting(SettingKeys.License.Serial, "");

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(serial))
                return Result.Fail<UpdateAvailability>("النسخة بلا سريال مُفعَّل", ErrorCode.ValidationFailed);

            var (ok, error, body) = await _http.PostAsync($"{server}/update",
                new { serial, version = AppInfo.Version });

            if (!ok) return Result.Fail<UpdateAvailability>(error, ErrorCode.Unexpected);

            var available = body.TryGetProperty("available", out var flag) && flag.GetBoolean();
            var version = body.TryGetProperty("version", out var v) ? v.GetString() : AppInfo.Version;
            var package = body.TryGetProperty("package", out var p) ? p.GetString() : null;

            return Result.Ok(new UpdateAvailability(available, version,
                package == null ? null : $"{server}{package}"));
        }

        public async Task<Result<string>> DownloadAsync(UpdateAvailability update, IProgress<double> progress = null)
        {
            if (update?.PackageUrl == null)
                return Result.Fail<string>("لا حزمة للتنزيل", ErrorCode.ValidationFailed);

            var target = Path.Combine(Path.GetTempPath(), $"PrimeERP-{update.Version}.zip");
            var (ok, error) = await _http.DownloadAsync(update.PackageUrl, target, progress);

            return ok ? Result.Ok(target) : Result.Fail<string>(error, ErrorCode.Unexpected);
        }
    }
}
