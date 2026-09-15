using System;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>
    /// قسط إهلاكٍ واحد: أصلٌ وشهرٌ ومبلغٌ وقيده — سجلٌّ مستقلّ كالسند تماماً، يُنشأ ويُعدَّل ويُحذَف
    /// بمفرده، وحذفه يأخذ قيده معه. وزرّ «احتساب الإهلاك» يُنشئ أقساط كل الأصول المستحقّة دفعةً واحدة
    /// بنفس المسار — فلا مسار ترحيلٍ ثانٍ ولا سجلّ مجمَّع لا يُحذَف بعضُه.
    /// </summary>
    public class AssetDepreciation : BaseModel
    {
        public int      AssetId        { get; set; }

        /// <summary>الشهر المستحقّ — آخر يومٍ فيه، وهو تاريخ القيد.</summary>
        public DateTime PeriodDate     { get; set; }
        public decimal  Amount         { get; set; }
        public int?     JournalEntryId { get; set; }
        public string   Notes          { get; set; }
    }
}
