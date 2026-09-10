using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Common
{
    /// <summary>طلب إنشاء نسخة برنامج: صفحاتها ووضعها ومسارها.</summary>
    public class CreateEditionDto
    {
        public List<string> ModuleKeys   { get; set; } = new();
        public bool         Simplified   { get; set; }
        public string       TargetFolder { get; set; }
    }

    /// <summary>تقدّم الإنشاء كما ترفعه الخدمة: نسبةٌ ومرحلة — تعرضهما الواجهة ولا تحسبهما.</summary>
    public record EditionProgress(double Percent, string Stage);
}
