using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PrimeERP.Domain.Results;
using PrimeERP.Platform;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Net;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.PageServices.Admin
{
    /// <summary>التحديث سؤالٌ واحد</summary>
    public record UpdateAvailability(bool Available, string Version, string FilesUrl, bool PagesChanged = false);

    public record UpdateFiles(string Folder, int Changed, int Removed);

    public interface IUpdateService
    {
        Task<Result<UpdateAvailability>> CheckAsync();
        Task<Result<UpdateFiles>> DownloadAsync(UpdateAvailability update, IProgress<double> progress = null);
        void MarkInstalled(string version);
    }

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

            if (!string.IsNullOrWhiteSpace(Setting(SettingKeys.Developer.AdminToken, "")))
                return Result.Fail<UpdateAvailability>(Msg("BuilderNoUpdate"), ErrorCode.ValidationFailed);

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(serial))
                return Result.Fail<UpdateAvailability>(Msg("NoActiveSerial"), ErrorCode.ValidationFailed);

            var installed = Setting(SettingKeys.License.Version, AppInfo.Version);
            var (ok, error, body) = await _http.PostAsync($"{server}/update",
                new { serial, version = installed, machine = MachineFingerprint.Value() });

            if (!ok) return Result.Fail<UpdateAvailability>(error, ErrorCode.Unexpected);

            var available = body.TryGetProperty("available", out var flag) && flag.GetBoolean();
            var version = body.TryGetProperty("version", out var v) ? v.GetString() : installed;
            var files = body.TryGetProperty("files", out var f) ? f.GetString() : null;

            return Result.Ok(new UpdateAvailability(available, version,
                files == null ? null : $"{server}{files}", SyncPages(body)));
        }

        public void MarkInstalled(string version) => Settings.SetRaw(SettingKeys.License.Version, version);

        private bool SyncPages(System.Text.Json.JsonElement body)
        {
            if (!body.TryGetProperty("manifest", out var manifest)) return false;

            var pages = manifest.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(pages)) return false;
            var simplified = body.TryGetProperty("simplified", out var flag) && flag.GetBoolean();
            if (pages == Setting(SettingKeys.UI.Manifest, "") && simplified == Setting(SettingKeys.Documents.SimplifiedFlow, true)) return false;

            Settings.SetRaw(SettingKeys.UI.Manifest, pages);
            Settings.SetRaw(SettingKeys.Documents.SimplifiedFlow, simplified);
            return true;
        }

        public async Task<Result<UpdateFiles>> DownloadAsync(UpdateAvailability update, IProgress<double> progress = null)
        {
            if (update?.FilesUrl == null)
                return Result.Fail<UpdateFiles>(Msg("NoPackage"), ErrorCode.ValidationFailed);

            var stage = Path.Combine(Path.GetTempPath(), "PrimeERP-update");
            if (Directory.Exists(stage)) Directory.Delete(stage, recursive: true);
            var staged = Path.Combine(stage, "files");
            Directory.CreateDirectory(staged);

            var list = Path.Combine(staged, "files.json");
            var (listed, listError) = await _http.DownloadAsync(update.FilesUrl, list);
            if (!listed) return Result.Fail<UpdateFiles>(listError, ErrorCode.Unexpected);

            var root = AppContext.BaseDirectory;
            var server = FileList(list);
            var previous = FileList(Path.Combine(root, "files.json"));
            var changed = server.Where(file => Fingerprint(Path.Combine(root, file.Key)) != file.Value.Sha256).ToList();
            var removed = previous.Keys.Except(server.Keys).ToList();

            var source = update.FilesUrl[..update.FilesUrl.LastIndexOf('/')];
            var total = Math.Max(1, changed.Sum(file => file.Value.Size));
            long done = 0;
            foreach (var (name, entry) in changed)
            {
                var target = Path.Combine(staged, name);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                var url = $"{source}/{string.Join("/", name.Split('/').Select(Uri.EscapeDataString))}";
                var (ok, error) = await _http.DownloadAsync(url, target);
                if (!ok) return Result.Fail<UpdateFiles>(error, ErrorCode.Unexpected);
                done += entry.Size;
                progress?.Report(done * 100d / total);
            }

            await File.WriteAllLinesAsync(Path.Combine(stage, "removed.txt"), removed);
            return Result.Ok(new UpdateFiles(stage, changed.Count, removed.Count));
        }

        private static Dictionary<string, (string Sha256, long Size)> FileList(string path)
        {
            if (!File.Exists(path)) return new();
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.GetProperty("files").EnumerateObject()
                .ToDictionary(file => file.Name, file => (file.Value.GetProperty("sha256").GetString(), file.Value.GetProperty("size").GetInt64()));
        }

        private static string Fingerprint(string path)
        {
            if (!File.Exists(path)) return null;
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)).ToLowerInvariant();
        }
    }
}
