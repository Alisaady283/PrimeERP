using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>
    /// إعادة تقييم أصل: القيمة قبلها وبعدها، وفرقٌ يُرحَّل قيداً — زيادةً أرباحاً رأسمالية أو نقصاً
    /// خسائر رأسمالية. سجلٌّ لا حقلٌ يُعدَّل: تغيير القيمة بلا قيد يجعل الميزانية لا تساوي الدفاتر.
    /// </summary>
    public class AssetRevaluation : BaseModel
    {
        public int      AssetId        { get; set; }
        public System.DateTime RevaluationDate { get; set; }

        /// <summary>القيمة قبل إعادة التقييم — تُلتقَط وقت الحفظ فيبقى السجل صحيحاً بعد أي تقييم لاحق.</summary>
        public decimal  OldValue       { get; set; }
        public decimal  NewValue       { get; set; }
        public string   Notes          { get; set; }
        public int?     JournalEntryId { get; set; }

        /// <summary>الفرق موجبٌ زيادة وسالبٌ نقص — لا يُدخَل، يُشتقّ.</summary>
        public decimal  Difference => NewValue - OldValue;
    }
}
