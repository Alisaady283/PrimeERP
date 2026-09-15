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

        /// <summary>القيمة الإجمالية الحالية للأصل — تبدأ بالتكلفة وتتغيّر بإعادة التقييم وحدها.
        /// أساس الإهلاك منها لا من التكلفة التاريخية: (المُعاد تقييمها − الخردة).</summary>
        public decimal  RevaluedValue { get; set; }

        /// <summary>القيمة الدفترية = القيمة المُعاد تقييمها − مجمّع الإهلاك. محسوبة لا مُدخَلة.</summary>
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

        /// <summary>نقداً من خزينة/بنك، أو آجلاً على مورد — يحكم الطرف الدائن في قيد الاقتناء.</summary>
        /// <summary>حساب الأصل في الشجرة — ورقيّ تحت حساب فئته، عليه تُرحَّل تكلفته وإعادة تقييمه.</summary>
        public string   AccountCode { get; set; }

        /// <summary>مجمّع إهلاكه — ورقيّ تحت مجمّع فئته، عليه يُرحَّل قسط الإهلاك.</summary>
        public string   DepreciationAccountCode { get; set; }

        public Enums.AssetAcquisition AcquisitionMethod { get; set; }

        /// <summary>المموّل المختار: خزينة أو بنك أو مورد — يُحفَظ ليُعاد عرضه عند التعديل.</summary>
        public int?     FundingId { get; set; }

        /// <summary>كود الحساب الدائن في قيد الاقتناء — يُحلّ من المموّل وقت الحفظ.</summary>
        public string   FundingAccountCode { get; set; }

        /// <summary>قيد الاقتناء — يُعكَس عند حذف الأصل كما تفعل كل مستندات النظام.</summary>
        public int?     JournalEntryId { get; set; }
    }
}
