using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Services.Backup;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Admin
{
    /// <summary>نسخةُ برنامجٍ مستقلّة في مسارٍ</summary>
    public interface IProgramEditionService
    {
        Result Create(CreateEditionDto edition, IProgress<EditionProgress> progress = null);
    }

    public class ProgramEditionService : ServiceBase, IProgramEditionService
    {
        protected override string PermissionPrefix => "BuilderExport";
        protected override string StringPrefix => "Str.Builder";
        protected override string EntityName => "ProgramEdition";

        private const double FilesShare = 88;
        private const double DatabaseShare = 8;

        private readonly IBackupService _backup;
        private readonly IEditionRepository _edition;
        private readonly ILicenseRepository _licenses;
        private readonly ISettingStore _settings;
        private readonly IBackupRepository _backupRepo;

        public ProgramEditionService(IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit,
            IBackupService backup, IEditionRepository edition, ILicenseRepository licenses,
            ISettingStore settingStore, IBackupRepository backupRepo)
            : base(permissions, settings, localization, audit)
        {
            _settings = settingStore;
            _backupRepo = backupRepo;
            _backup = backup;
            _edition = edition;
            _licenses = licenses;
        }

        public Result Create(CreateEditionDto edition, IProgress<EditionProgress> progress = null)
        {
            if (!Can("Create")) return FailDenied();

            var invalid = Check(new EditionValidator(_backupRepo.Capability), edition);
            if (invalid.IsFailure) return invalid;

            var source = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
            var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(edition.TargetFolder));

            try
            {
                CopyFolder(source, target, progress);

                progress?.Report(new EditionProgress(FilesShare, Msg("CopyingDatabase")));
                var copied = _backup.Create(target, Msg("EditionNote"), BackupType.Manual);
                if (copied.IsFailure) return Result.Fail(copied.ErrorMessage);

                progress?.Report(new EditionProgress(FilesShare + DatabaseShare, Msg("WritingManifest")));
                StripDeveloperData(copied.Value.FilePath);
                WriteEditionSettings(copied.Value.FilePath, edition);
                PointAtDatabase(Path.Combine(target, "appsettings.json"), copied.Value.FilePath);

                Audit.Log(EntityName, 0, AuditAction.Insert,
                    newValue: new { target, edition.Simplified, pages = edition.ModuleKeys.Count });

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message, ErrorCode.Unexpected);
            }
        }

        private void StripDeveloperData(string databasePath)
        {
            using var connection = _edition.Open(databasePath);
            using var db = DbContextFactory.On(connection);

            db.AppSettings.RemoveRange(db.AppSettings.Where(s => s.Key.StartsWith("Developer.")));
            db.Licenses.RemoveRange(db.Licenses);
            db.SaveChanges();
        }

        private void WriteEditionSettings(string databasePath, CreateEditionDto edition)
        {
            using var connection = _edition.Open(databasePath);
            using var db = DbContextFactory.On(connection);

            foreach (var (key, value) in new Dictionary<string, string>
            {
                [SettingKeys.UI.Manifest] = string.Join(",", edition.ModuleKeys),
                [SettingKeys.Documents.SimplifiedFlow] = edition.Simplified ? "true" : "false"
            })
            {
                var row = db.AppSettings.FirstOrDefault(s => s.Key == key);
                if (row == null)
                    db.AppSettings.Add(new AppSetting
                    {
                        Key = key, Value = value, Category = "UI", DataType = "string",
                        IsSystem = true, ModifiedBy = AppSession.Username, ModifiedAt = DateTime.Now
                    });
                else
                {
                    row.Value = value;
                    row.ModifiedBy = AppSession.Username;
                    row.ModifiedAt = DateTime.Now;
                }
            }

            db.SaveChanges();
        }

        private void CopyFolder(string source, string target, IProgress<EditionProgress> progress)
        {
            Directory.CreateDirectory(target);

            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(directory.Replace(source, target));

            var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
            var stage = Msg("CopyingFiles");
            var reported = -1;

            for (var i = 0; i < files.Length; i++)
            {
                if (!Path.GetExtension(files[i]).Equals(".pdb", StringComparison.OrdinalIgnoreCase))
                    File.Copy(files[i], files[i].Replace(source, target), overwrite: true);

                var percent = (int)((i + 1) * FilesShare / files.Length);
                if (percent == reported) continue;

                reported = percent;
                progress?.Report(new EditionProgress(percent, stage));
            }
        }

        private static void PointAtDatabase(string settingsFile, string databasePath)
        {
            var settings = File.Exists(settingsFile)
                ? JsonNode.Parse(File.ReadAllText(settingsFile))
                : new JsonObject();

            settings["Database"] ??= new JsonObject();
            settings["Database"]["FilePath"] = databasePath;

            File.WriteAllText(settingsFile, settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
