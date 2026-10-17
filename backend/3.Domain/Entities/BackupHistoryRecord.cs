using System;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Domain.Entities
{
    /// <summary>سجل نسخة احتياطية</summary>
    public class BackupHistoryRecord
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string Note { get; set; }
        public BackupType BackupType { get; set; }
        public string DatabaseProvider { get; set; }
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }
    }
}
