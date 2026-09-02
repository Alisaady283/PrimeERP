using System;

namespace PrimeERP.Domain.Entities
{
    /// <summary>ربط سحب واحد: سطر مستند مصدر ← سطر مستند هدف. جدول واحد يخدم دورتي الشراء والبيع معاً.</summary>
    public class DocumentLink
    {
        public int      Id           { get; set; }
        public string   SourceType   { get; set; }
        public int      SourceId     { get; set; }
        public string   SourceNo     { get; set; }
        public int      SourceLineId { get; set; }
        public string   TargetType   { get; set; }
        public int      TargetId     { get; set; }
        public int      TargetLineId { get; set; }
        public decimal  PulledQty    { get; set; }
        public DateTime CreatedAt    { get; set; }
        public string   CreatedBy    { get; set; }
    }
}
