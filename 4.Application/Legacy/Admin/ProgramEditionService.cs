using PrimeERP.Application.Legacy.Backup;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Admin
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

        private readonly IEditionRepository _edition;
        private readonly ILicenseRepository _licenses;
        private readonly IBackupRepository _backupRepo;

        public ProgramEditionService(IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit,
            IEditionRepository edition, ILicenseRepository licenses,
            IBackupRepository backupRepo)
            : base(permissions, settings, localization, audit)
        {
            _backupRepo = backupRepo;
            _edition = edition;
            _licenses = licenses;
        }

        public Result Create(CreateEditionDto edition, IProgress<EditionProgress> progress = null)
        {
            if (!Can("Create")) return FailDenied();

            var invalid = Check.Valid(edition, EditionFields(_backupRepo.Capability));
            if (invalid.IsFailure) return invalid;

            var source = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
            var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(edition.TargetFolder));

            try
            {
                var stage = Msg("CopyingFiles");
                _edition.CopyProgram(source, target, FilesShare, percent => progress?.Report(new EditionProgress(percent, stage)));

                progress?.Report(new EditionProgress(FilesShare, Msg("CopyingDatabase")));
                var copied = _backupRepo.Snapshot(target, Msg("EditionNote"), BackupType.Manual);

                progress?.Report(new EditionProgress(FilesShare + DatabaseShare, Msg("WritingManifest")));
                StripDeveloperData(copied.FilePath);
                WriteEditionSettings(copied.FilePath, edition);
                _edition.PointAtDatabase(Path.Combine(target, "appsettings.json"), copied.FilePath);

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
            _licenses.Clear(db);
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

        /// <summary>شروط النسخة المنشأة</summary>
        private static Field<CreateEditionDto>[] EditionFields(BackupCapability capability) => new Field<CreateEditionDto>[]
        {
            new(x => x.TargetFolder, "Str.Field.EditionPath", Required: true),
            new(x => x.ModuleKeys, "", Required: true, Message: "Str.Builder.PickOnePage"),
            new(x => x.TargetFolder, "", Must: _ => capability == BackupCapability.FileCopy, Message: "Str.Builder.EditionLocalOnly"),
            new(x => x.TargetFolder, "", Must: d => string.IsNullOrWhiteSpace(d.TargetFolder) ||
                !Path.TrimEndingDirectorySeparator(Path.GetFullPath(d.TargetFolder))
                    .StartsWith(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory), StringComparison.OrdinalIgnoreCase),
                Message: "Str.Builder.EditionInsideApp"),
        };
    }
}
