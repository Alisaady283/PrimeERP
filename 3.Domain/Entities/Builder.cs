using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Domain.Entities
{
    /// <summary>قسم في الشريط الجانبي — يُطلَب قبل الصفحة، فلا صفحة بلا قسم.</summary>
    public class BuilderSection : BaseModel
    {
        public string Key      { get; set; }
        public string Title    { get; set; }
        public string IconKey  { get; set; }
        public int    SortOrder { get; set; }

        /// <summary>مفاتيح وحدات القسم مفصولةً بفاصلة — تُبذَر من خريطة الكود ثم تُعدَّل.</summary>
        public string Modules  { get; set; }
    }

    /// <summary>
    /// وحدة مبنيّة: صفحة كاملة بنوعها. النوع يحكم كل ما بعده — التقرير بلا جدول يقرأ من غيره، والسجلّ
    /// جدولٌ بحوار، والحركة جدولٌ برأس وسطور. لا يُخلط بينها بعد الحفظ.
    /// </summary>
    public class BuilderModule : BaseModel
    {
        public string          Key        { get; set; }
        public string          Title      { get; set; }
        public BuilderKind     Kind       { get; set; }
        public int             SectionId  { get; set; }
        public string          TableName  { get; set; }   // فارغ للتقرير
        public string          LineTable  { get; set; }   // للحركة وحدها
        public string          SourceKey  { get; set; }   // للتقرير: الوحدة التي يقرأ منها
        public string          CopiedFrom { get; set; }   // نسخة من صفحة قائمة
        public int             SortOrder  { get; set; }
        public bool            IsActive   { get; set; } = true;

        /// <summary>مبذورة من صفحات الكود: تُعرَض وتُرتَّب وتُحذَف، ولا تُسجَّل ولا يُنشأ لها جدول.</summary>
        public bool            IsCoded    { get; set; }
    }

    /// <summary>عمود في جدول الوحدة: تعريف تخزينه وعرضه معاً. المحسوب لا يُخزَّن بل يُجمَّع عند القراءة.</summary>
    public class BuilderColumn : BaseModel
    {
        public int             ModuleId    { get; set; }
        public string          Name        { get; set; }   // اسم العمود في القاعدة
        public string          Header      { get; set; }   // الاسم الظاهر
        public BuilderDataType DataType    { get; set; }
        public bool            IsRequired  { get; set; }
        public bool            IsUnique    { get; set; }
        public int?            MaxLength   { get; set; }
        public string          RefModule   { get; set; }   // نوع «من جدول»: مفتاح الوحدة المرجعية
        public string          RefDisplay  { get; set; }   // العمود المعروض منها

        // العمود المحسوب: تجميعٌ من جدول مرتبط، يُقرأ ولا يُكتب (مثل رصيد العميل).
        public BuilderAggregate Aggregate   { get; set; } = BuilderAggregate.None;
        public string           AggFrom     { get; set; }  // الجدول المُجمَّع منه
        public string           AggColumn   { get; set; }  // عموده
        public string           AggMatch    { get; set; }  // عمود الربط فيه

        public bool            ShowInGrid  { get; set; } = true;
        public bool            ShowInForm  { get; set; } = true;
        public bool            IsLine      { get; set; }   // سطر لا رأس (الحركة)
        public double          Width       { get; set; } = 140;
        public string          Footer      { get; set; }   // None / Sum / Count / Average — نصّ لأن التعداد في 6.UI
        public int             SortOrder   { get; set; }
    }

    /// <summary>زرّ مُختار من كتالوج النظام — لا يُكتب، يُؤشَّر.</summary>
    public class BuilderAction : BaseModel
    {
        public int    ModuleId { get; set; }
        public string ActionKey { get; set; }   // New / Edit / Delete / Refresh / Print / Export / Post…
        public bool   OnTable   { get; set; }   // إجراء جدول (على السجل المحدَّد) لا إجراء صفحة
        public int    SortOrder { get; set; }
    }

    /// <summary>فلتر فوق الشبكة — قائمة، تبديل، أو مدى تاريخين.</summary>
    public class BuilderFilter : BaseModel
    {
        public int    ModuleId  { get; set; }
        public string Key       { get; set; }
        public string Label     { get; set; }
        public string Kind      { get; set; }   // Combo / Toggle / DateRange
        public string RefModule { get; set; }
        public int    SortOrder { get; set; }
    }
}
