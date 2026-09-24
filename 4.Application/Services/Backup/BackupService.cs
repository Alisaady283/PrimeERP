using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimeERP.Application.Services;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Timer = System.Timers.Timer;

namespace PrimeERP.Application.Services.Backup
{
    /// <summary>المكان الوحيد لأخذ/استعادة/التحقق من النسخ</summary>
    public class BackupService : ServiceBase, IBackupService
    {
        protected override string PermissionPrefix => "Settings";
        protected override string StringPrefix => "Str.Backup";
        protected override string EntityName => "BackupHistory";

        private readonly IBackupRepository _repo;
        private Timer _autoTimer;

        public BackupService(IPermissionService permissions, ISettingsProvider settings,
                              ILocalizationService localization, IAuditLogger audit, IBackupRepository repo)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo;
        }

        public event Action<BackupInfo> BackupCompleted;
        public event Action<string> BackupFailed;

        public Result<BackupInfo> Create(string folder = null, string note = null, BackupType type = BackupType.Manual)
        {
            if (!Can("Backup")) return Fail<BackupInfo>("PermissionDenied", ErrorCode.Unauthorized);

            try
            {
                folder ??= ResolveBackupFolder();
                Directory.CreateDirectory(folder);

                var fileName   = $"PrimeERP_{DateTime.Now:yyyyMMdd_HHmmss}{_repo.FileExtension}";
                var targetPath = Path.Combine(folder, fileName);

                _repo.CopyTo(targetPath);

                var record = new BackupHistoryRecord
                {
                    FileName         = fileName,
                    FilePath         = targetPath,
                    SizeBytes        = new FileInfo(targetPath).Length,
                    CreatedAt        = DateTime.Now,
                    CreatedBy        = AppSession.Username,
                    Note             = note,
                    BackupType       = type,
                    DatabaseProvider = DbConfig.Current.Provider.ToString(),
                    IsValid          = true
                };

                var id = _repo.Insert(record);
                Audit.Log(EntityName, id, AuditAction.Insert,
                    newValue: new { record.FileName, record.SizeBytes }, details: $"إنشاء نسخة احتياطية ({type})");

                var info = ToInfo(record);

                var retentionCount = Setting(SettingKeys.Backup.RetentionCount, 10);
                ApplyRetention(folder, retentionCount);

                BackupCompleted?.Invoke(info);
                return Result.Ok(info);
            }
            catch (NotSupportedException ex)
            {
                BackupFailed?.Invoke(ex.Message);
                return Result.Fail<BackupInfo>(ex.Message, ErrorCode.Unexpected);
            }
            catch (Exception ex)
            {
                var message = $"{Msg("CreateFailed")}: {ex.Message}";
                BackupFailed?.Invoke(message);
                return Result.Fail<BackupInfo>(message, ErrorCode.Unexpected);
            }
        }

        public Result Restore(string filePath)
        {
            if (!Can("Restore")) return Fail("RestorePermissionDenied", ErrorCode.Unauthorized);

            var validation = Validate(filePath);
            if (validation.IsFailure)
                return Result.Fail(validation.ErrorMessage, validation.ErrorCode);

            var safetyBackup = Create(note: "نسخة أمان تلقائية قبل الاستعادة", type: BackupType.PreRestore);
            if (safetyBackup.IsFailure)
                return Result.Fail($"{Msg("SafetyBackupFailed")}: {safetyBackup.ErrorMessage}", ErrorCode.Unexpected);

            try
            {
                _repo.RestoreFrom(filePath);

                Audit.Log(EntityName, 0, AuditAction.Update, details: $"استعادة من نسخة: {Path.GetFileName(filePath)}");
                return Result.Ok();
            }
            catch (NotSupportedException ex)
            {
                return Result.Fail(ex.Message, ErrorCode.Unexpected);
            }
            catch (Exception ex)
            {
                return Result.Fail($"{Msg("RestoreFailed")}: {ex.Message}", ErrorCode.Unexpected);
            }
        }

        public Result<bool> Validate(string filePath)
        {
            if (!File.Exists(filePath)) return Fail<bool>("FileNotFound", ErrorCode.NotFound);
            if (new FileInfo(filePath).Length <= 0) return Fail<bool>("FileEmpty", ErrorCode.ValidationFailed);

            var (ok, error) = _repo.Verify(filePath);
            if (ok) return Result.Ok(true);

            return string.IsNullOrEmpty(error)
                ? Fail<bool>("NotAPrimeErpDatabase", ErrorCode.ValidationFailed)
                : Result.Fail<bool>($"{Msg("CorruptFile")}: {error}", ErrorCode.Unexpected);
        }

        public List<BackupInfo> List(string folder = null) =>
            _repo.GetAll()
                .Where(r => folder == null || string.Equals(Path.GetDirectoryName(r.FilePath), folder, StringComparison.OrdinalIgnoreCase))
                .Select(ToInfo)
                .ToList();

        public Result Delete(string filePath)
        {
            if (!Can("Backup")) return Fail("PermissionDenied", ErrorCode.Unauthorized);

            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);

                var record = _repo.GetAll().FirstOrDefault(r => string.Equals(r.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                if (record != null)
                    _repo.Delete(record.Id);

                Audit.Log(EntityName, record?.Id ?? 0, AuditAction.Delete, details: Path.GetFileName(filePath));
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"{Msg("DeleteFailed")}: {ex.Message}", ErrorCode.Unexpected);
            }
        }

        public Result ApplyRetention(string folder, int keepCount)
        {
            try
            {
                var toRemove = _repo.GetAll()
                    .Where(r => string.Equals(Path.GetDirectoryName(r.FilePath), folder, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip(Math.Max(0, keepCount));

                foreach (var old in toRemove)
                {
                    if (File.Exists(old.FilePath))
                        File.Delete(old.FilePath);
                    _repo.Delete(old.Id);
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"{Msg("RetentionFailed")}: {ex.Message}", ErrorCode.Unexpected);
            }
        }

        public void StartAutoBackup()
        {
            StopAutoBackup();

            if (!Setting(SettingKeys.Backup.AutoBackupEnabled, false)) return;

            var hours = Math.Max(1, Setting(SettingKeys.Backup.AutoBackupIntervalHours, 24));

            _autoTimer = new Timer(TimeSpan.FromHours(hours).TotalMilliseconds) { AutoReset = true };
            _autoTimer.Elapsed += (s, e) => Create(note: "نسخة تلقائية مجدولة", type: BackupType.Auto);
            _autoTimer.Start();
        }

        public void StopAutoBackup()
        {
            _autoTimer?.Stop();
            _autoTimer?.Dispose();
            _autoTimer = null;
        }

        private string ResolveBackupFolder()
        {
            var configured = Setting(SettingKeys.Backup.AutoBackupPath, "");
            return string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups")
                : configured;
        }

        private static BackupInfo ToInfo(BackupHistoryRecord r) => new()
        {
            FilePath         = r.FilePath,
            FileName         = r.FileName,
            CreatedAt        = r.CreatedAt,
            CreatedBy        = r.CreatedBy,
            SizeBytes        = r.SizeBytes,
            Note             = r.Note,
            BackupType       = r.BackupType,
            DatabaseProvider = r.DatabaseProvider,
            IsValid          = r.IsValid,
            ValidationMessage = r.ValidationMessage
        };
    }
}
