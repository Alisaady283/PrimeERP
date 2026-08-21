using System;
using System.Collections.Generic;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Backup
{
    public interface IBackupService
    {
        Result<BackupInfo> Create(string folder = null, string note = null, BackupType type = BackupType.Manual);
        Result Restore(string filePath);
        Result<bool> Validate(string filePath);
        List<BackupInfo> List(string folder = null);
        Result Delete(string filePath);

        /// <summary>يحذف الأقدم من مجلد النسخ ويُبقي آخر keepCount فقط.</summary>
        Result ApplyRetention(string folder, int keepCount);

        void StartAutoBackup();
        void StopAutoBackup();

        event Action<BackupInfo> BackupCompleted;
        event Action<string> BackupFailed;
    }
}
