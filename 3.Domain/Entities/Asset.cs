using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class Asset : BaseModel
    {
        public string   Code          { get; set; }
        public string   Name          { get; set; }
        public int?     CategoryId    { get; set; }
        public System.DateTime? PurchaseDate { get; set; }
        public decimal  PurchaseCost  { get; set; }
        public decimal  CurrentValue  { get; set; }
        public string   Location      { get; set; }
        public string   Notes         { get; set; }
        public bool     IsActive      { get; set; } = true;
    }
}
