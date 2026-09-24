using System;
using System.IO;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Application.Services.Backup
{
    /// <summary>وصف نسخة احتياطية</summary>
    public class BackupInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public long SizeBytes { get; set; }
        public string Note { get; set; }
        public BackupType BackupType { get; set; }
        public string DatabaseProvider { get; set; }
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }

        public bool Exists => !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);

        public string SizeText
        {
            get
            {
                double size = SizeBytes;
                string[] units = { "B", "KB", "MB", "GB" };
                int i = 0;
                while (size >= 1024 && i < units.Length - 1)
                {
                    size /= 1024;
                    i++;
                }
                return $"{size:0.##} {units[i]}";
            }
        }
    }
}
