namespace PrimeERP.Application.DTOs.Documents
{
    // حقول ربط السحب على سطر مستند هدف — تُملأ فقط لو السطر جاء بسحب من مستند آخر. مشتركة نصياً (بالاسم)
    // بين كل أنواع السطور: DocumentRenderer ينسخها بالاسم، والخدمات تقرأها بالاسم — لا وراثة تفرض شكلاً واحداً
    // على DTOs مختلفة الأصل.
    public interface IPullableLine
    {
        string SourceType   { get; set; }
        int    SourceId     { get; set; }
        string SourceNo     { get; set; }
        int    SourceLineId { get; set; }
    }
}
