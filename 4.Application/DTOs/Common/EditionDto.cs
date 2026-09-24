using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Common
{
    /// <summary>طلب إنشاء نسخة برنامج</summary>
    public class CreateEditionDto
    {
        public List<string> ModuleKeys   { get; set; } = new();
        public bool         Simplified   { get; set; }
        public string       TargetFolder { get; set; }
    }

    /// <summary>تقدّم الإنشاء: نسبةٌ ومرحلة</summary>
    public record EditionProgress(double Percent, string Stage);
}
