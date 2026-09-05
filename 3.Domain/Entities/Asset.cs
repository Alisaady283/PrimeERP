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

        /// <summary>العمر الإنتاجي بالسنوات — أساس القسط الثابت. صفر يعني أصلاً لا يُهلَك (أرض مثلاً).</summary>
        public int      UsefulLifeYears { get; set; }

        /// <summary>القيمة المتبقية في نهاية العمر — تُستبعَد من الأساس القابل للإهلاك.</summary>
        public decimal  SalvageValue    { get; set; }
        public decimal  AccumulatedDepreciation { get; set; }
        public System.DateTime? LastDepreciationDate { get; set; }
        public string   Location      { get; set; }
        public string   Notes         { get; set; }
        public bool     IsActive      { get; set; } = true;
    }
}
