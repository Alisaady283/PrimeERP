using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان Unit</summary>
    public class Unit : BaseModel
    {
        public string Name     { get; set; }
        public string NameEn   { get; set; }
        public string Symbol   { get; set; }
        public bool   IsActive { get; set; } = true;
    }
}
