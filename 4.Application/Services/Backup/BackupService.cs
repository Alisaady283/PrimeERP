using PrimeERP.Platform.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Data.Core;
using PrimeERP.Data.Schema;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using Auditor = PrimeERP.Platform.Audit.AuditLogger;
using AuditAction = PrimeERP.Domain.Enums.AuditAction;
using Db = PrimeERP.Data.Core.DbHelper;
using Timer = System.Timers.Timer;

namespace PrimeERP.Application.Services.Backup
{
    /// <summary>
    /// المكان الوحيد لأخذ/استعادة/التحقق من النسخ الاحتياطية — بديل Core/BackupManager.cs (المحذوف).
    /// كل اختلاف بين محركات القاعدة عبر IDbProvider (BuildBackupCommand/BuildRestoreCommand/GetBackupCapability) —
    /// لا "if" على نوع المحرك هنا سوى في Validate (فحص لا يمثّله IDbProvider بعد).
    /// </summary>
    public class BackupService : IBackupService
    {
        public static readonly BackupService Instance = new();

        private readonly IPermissionService _permissions = PermissionService.Instance;
        private readonly ISettingsService _settings = SettingsService.Instance;
        private Timer _autoTimer;

        public event Action<BackupInfo> BackupCompleted;
        public event Action<string> BackupFailed;

        public Result<BackupInfo> Create(string folder = null, string note = null, BackupType type = BackupType.Manual)
        {
            if (!_permissions.Can(PermissionKeys.Settings.Backup))
                return Result.Fail<BackupInfo>(LocalizationService.Get("Str.Backup.PermissionDenied"), ErrorCode.Unauthorized);

            try
            {
                folder ??= ResolveBackupFolder();
                Directory.CreateDirectory(folder);

                var provider = DbFactory.Current;
                var config   = DbFactory.Config;

                var fileName   = $"PrimeERP_{DateTime.Now:yyyyMMdd_HHmmss}{provider.BackupFileExtension}";
                var targetPath = Path.Combine(folder, fileName);

                var sql = provider.BuildBackupCommand(config, targetPath);
                Db.Execute(sql, Db.Params(("@path", targetPath)));

                var record = new BackupHistoryRecord
                {
                    FileName         = fileName,
                    FilePath         = targetPath,
                    SizeBytes        = new FileInfo(targetPath).Length,
                    CreatedAt        = DateTime.Now,
                    CreatedBy        = AppSession.Username,
                    Note             = note,
                    BackupType       = type,
                    DatabaseProvider = provider.Kind.ToString(),
                    IsValid          = true
                };

                var id = BackupRepository.Insert(record);
                Auditor.Log("BackupHistory", id, AuditAction.Insert,
                    newValue: new { record.FileName, record.SizeBytes }, details: $"إنشاء نسخة احتياطية ({type})");

                var info = ToInfo(record);

                var retentionCount = _settings.Get(SettingKeys.Backup.RetentionCount, 10);
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
                var message = $"{LocalizationService.Get("Str.Backup.CreateFailed")}: {ex.Message}";
                BackupFailed?.Invoke(message);
                return Result.Fail<BackupInfo>(message, ErrorCode.Unexpected);
            }
        }

        public Result Restore(string filePath)
        {
            if (!_permissions.Can(PermissionKeys.Settings.Restore))
                return Result.Fail(LocalizationService.Get("Str.Backup.RestorePermissionDenied"), ErrorCode.Unauthorized);

            var validation = Validate(filePath);
            if (validation.IsFailure)
                return Result.Fail(validation.ErrorMessage, validation.ErrorCode);

            var safetyBackup = Create(note: "نسخة أمان تلقائية قبل الاستعادة", type: BackupType.PreRestore);
            if (safetyBackup.IsFailure)
                return Result.Fail($"{LocalizationService.Get("Str.Backup.SafetyBackupFailed")}: {safetyBackup.ErrorMessage}", ErrorCode.Unexpected);

            try
            {
                var provider = DbFactory.Current;
                var config   = DbFactory.Config;

                if (provider.GetBackupCapability() == BackupCapability.FileCopy)
                {
                    // SQLite يجمع الاتصالات (Connection Pooling) — يجب تفريغها قبل استبدال الملف فعلياً، وإلا بقي مقفلاً.
                    SqliteConnection.ClearAllPools();
                    File.Copy(filePath, provider.GetDatabaseFilePath(config), overwrite: true);
                }
                else
                {
                    var sql = provider.BuildRestoreCommand(config, filePath);
                    Db.Execute(sql, Db.Params(("@path", filePath)));
                }

                Auditor.Log("BackupHistory", 0, AuditAction.Update, details: $"استعادة من نسخة: {Path.GetFileName(filePath)}");
                return Result.Ok();
            }
            catch (NotSupportedException ex)
            {
                return Result.Fail(ex.Message, ErrorCode.Unexpected);
            }
            catch (Exception ex)
            {
                return Result.Fail($"{LocalizationService.Get("Str.Backup.RestoreFailed")}: {ex.Message}", ErrorCode.Unexpected);
            }
        }

        public Result<bool> Validate(string filePath)
        {
            if (!File.Exists(filePath))
                return Result.Fail<bool>(LocalizationService.Get("Str.Backup.FileNotFound"), ErrorCode.NotFound);

            if (new FileInfo(filePath).Length <= 0)
                return Result.Fail<bool>(LocalizationService.Get("Str.Backup.FileEmpty"), ErrorCode.ValidationFailed);

            var kind = DbFactory.Current.Kind;

            if (kind == DatabaseProvider.Sqlite)
                return ValidateSqlite(filePath);

            if (kind == DatabaseProvider.SqlServer)
                return ValidateSqlServer(filePath);

            // PostgreSQL: التحقق الكامل يحتاج pg_restore خارجياً — نكتفي بفحص الوجود/الحجم أعلاه.
            return Result.Ok(true);
        }

        private static Result<bool> ValidateSqlite(string filePath)
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly");
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA integrity_check;";
                    var result = cmd.ExecuteScalar()?.ToString();
                    if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                        return Result.Fail<bool>($"{LocalizationService.Get("Str.Backup.IntegrityCheckFailed")}: {result}", ErrorCode.ValidationFailed);
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__Migrations'";
                    var exists = Convert.ToInt64(cmd.ExecuteScalar()) > 0;
                    if (!exists)
                        return Result.Fail<bool>(LocalizationService.Get("Str.Backup.NotAPrimeErpDatabase"), ErrorCode.ValidationFailed);
                }

                return Result.Ok(true);
            }
            catch (Exception ex)
            {
                return Result.Fail<bool>($"{LocalizationService.Get("Str.Backup.CorruptFile")}: {ex.Message}", ErrorCode.Unexpected);
            }
        }

        private static Result<bool> ValidateSqlServer(string filePath)
        {
            try
            {
                Db.Execute("RESTORE VERIFYONLY FROM DISK = @path", Db.Params(("@path", filePath)));
                return Result.Ok(true);
            }
            catch (Exception ex)
            {
                return Result.Fail<bool>($"{LocalizationService.Get("Str.Backup.CorruptFile")}: {ex.Message}", ErrorCode.Unexpected);
            }
        }

        public List<BackupInfo> List(string folder = null) =>
            BackupRepository.GetAll()
                .Where(r => folder == null || string.Equals(Path.GetDirectoryName(r.FilePath), folder, StringComparison.OrdinalIgnoreCase))
                .Select(ToInfo)
                .ToList();

        public Result Delete(string filePath)
        {
            if (!_permissions.Can(PermissionKeys.Settings.Backup))
                return Result.Fail(LocalizationService.Get("Str.Backup.PermissionDenied"), ErrorCode.Unauthorized);

            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);

                var record = BackupRepository.GetAll().FirstOrDefault(r => string.Equals(r.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                if (record != null)
                    BackupRepository.Delete(record.Id);

                Auditor.Log("BackupHistory", record?.Id ?? 0, AuditAction.Delete, details: Path.GetFileName(filePath));
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"{LocalizationService.Get("Str.Backup.DeleteFailed")}: {ex.Message}", ErrorCode.Unexpected);
            }
        }

        public Result ApplyRetention(string folder, int keepCount)
        {
            try
            {
                var toRemove = BackupRepository.GetAll()
                    .Where(r => string.Equals(Path.GetDirectoryName(r.FilePath), folder, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip(Math.Max(0, keepCount));

                foreach (var old in toRemove)
                {
                    if (File.Exists(old.FilePath))
                        File.Delete(old.FilePath);
                    BackupRepository.Delete(old.Id);
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"{LocalizationService.Get("Str.Backup.RetentionFailed")}: {ex.Message}", ErrorCode.Unexpected);
            }
        }

        public void StartAutoBackup()
        {
            StopAutoBackup();

            if (!_settings.Get(SettingKeys.Backup.AutoBackupEnabled, false))
                return;

            var hours = Math.Max(1, _settings.Get(SettingKeys.Backup.AutoBackupIntervalHours, 24));

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
            var configured = _settings.Get(SettingKeys.Backup.AutoBackupPath, "");
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
