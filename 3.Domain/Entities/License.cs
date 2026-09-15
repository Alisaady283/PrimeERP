using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>ترخيص عميل: سرياله وما يفتحه من صفحات، والجهاز الذي فُعّل عليه.</summary>
    public class License : BaseModel
    {
        public string CustomerName { get; set; }
        public string Location     { get; set; }
        public string Serial       { get; set; }

        /// <summary>مفاتيح الصفحات مفصولة بفاصلة — نفس شكل UI.Manifest الذي تُقلع به النسخة.</summary>
        public string Manifest     { get; set; }
        public bool   Simplified   { get; set; }

        /// <summary>بصمة الجهاز الذي فُعّل عليه — فارغة حتى أول تفعيل.</summary>
        public string MachineHash  { get; set; }
        public bool   IsActive     { get; set; } = true;
    }
}
