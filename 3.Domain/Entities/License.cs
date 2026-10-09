using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>ترخيص عميل</summary>
    public class License : BaseModel
    {
        public string CustomerName { get; set; }
        public string Location     { get; set; }
        public string Serial       { get; set; }
        public string Version      { get; set; }

        public string Manifest     { get; set; }
        public bool   Simplified   { get; set; }

        public string MachineHash  { get; set; }
        public bool   IsActive     { get; set; } = true;
    }
}
