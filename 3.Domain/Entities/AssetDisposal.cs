using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>
    /// بيع أصل واستبعاده: ثمنه والخزينة التي قُبض فيها. الأصل لا يُحذَف — يُستبعَد فيخرج من الإهلاك
    /// وتُغلَق حساباته بقيدٍ واحد: الخزينة مدينةً بالثمن، ومجمّعه مديناً بما أُهلك، وحسابه دائناً
    /// بقيمته، والفرق ربحاً أو خسارةً رأسمالية.
    ///
    /// قيمة الأصل ومجمّعه يُلتقَطان لحظة البيع لا يُقرآن لاحقاً: بعد الاستبعاد يُصفَّر الحسابان، فقراءةٌ
    /// متأخّرة تجعل عكس القيد عند الحذف بمبالغ خاطئة.
    /// </summary>
    public class AssetDisposal : BaseModel
    {
        public int      AssetId      { get; set; }
        public System.DateTime DisposalDate { get; set; }

        /// <summary>وجهة قبض الثمن — خزينة أو بنك، كأي مقبوضٍ في النظام.</summary>
        public int      TreasuryId   { get; set; }
        public decimal  SalePrice    { get; set; }

        /// <summary>قيمة الأصل (تكلفته أو آخر إعادة تقييم) ومجمّع إهلاكه، كلاهما لحظة البيع.</summary>
        public decimal  AssetValue   { get; set; }
        public decimal  AccumulatedDepreciation { get; set; }

        public string   Notes        { get; set; }
        public int?     JournalEntryId { get; set; }

        public decimal  BookValue  => AssetValue - AccumulatedDepreciation;

        /// <summary>موجبٌ ربحٌ رأسمالي وسالبٌ خسارة — يُشتقّ لا يُدخَل.</summary>
        public decimal  GainOrLoss => SalePrice - BookValue;
    }
}
