using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using PrimeERP.Application.Services;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Data.Core;
using PrimeERP.Data.Schema;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Db = PrimeERP.Data.Core.DbHelper;
using Timer = System.Timers.Timer;

namespace PrimeERP.Application.Services.Backup
{
    /// <summary>
    /// المكان الوحيد لأخذ/استعادة/التحقق من النسخ الاحتياطية — بديل Core/BackupManager.cs (المحذوف).
    /// كل اختلاف بين محركات القاعدة عبر IDbProvider (BuildBackupCommand/BuildRestoreCommand/GetBackupCapability) —
    /// لا "if" على نوع المحرك هنا سوى في Validate (فحص لا يمثّله IDbProvider بعد).
    /// </summary>
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

            var kind = DbFactory.Current.Kind;

            if (kind == DatabaseProvider.Sqlite) return ValidateSqlite(filePath);
            if (kind == DatabaseProvider.SqlServer) return ValidateSqlServer(filePath);

            // PostgreSQL: التحقق الكامل يحتاج pg_restore خارجياً — نكتفي بفحص الوجود/الحجم أعلاه.
            return Result.Ok(true);
        }

        private Result<bool> ValidateSqlite(string filePath)
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
                        return Result.Fail<bool>($"{Msg("IntegrityCheckFailed")}: {result}", ErrorCode.ValidationFailed);
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__Migrations'";
                    var exists = Convert.ToInt64(cmd.ExecuteScalar()) > 0;
                    if (!exists) return Fail<bool>("NotAPrimeErpDatabase", ErrorCode.ValidationFailed);
                }

                return Result.Ok(true);
            }
            catch (Exception ex)
            {
                return Result.Fail<bool>($"{Msg("CorruptFile")}: {ex.Message}", ErrorCode.Unexpected);
            }
        }

        private Result<bool> ValidateSqlServer(string filePath)
        {
            try
            {
                Db.Execute("RESTORE VERIFYONLY FROM DISK = @path", Db.Params(("@path", filePath)));
                return Result.Ok(true);
            }
            catch (Exception ex)
            {
                return Result.Fail<bool>($"{Msg("CorruptFile")}: {ex.Message}", ErrorCode.Unexpected);
            }
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
