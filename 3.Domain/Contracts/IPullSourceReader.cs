namespace PrimeERP.Domain.Contracts
{
    /// <summary>يقرأ كمية سطر مستند مصدر أياً كان نوعه — يُنفَّذ في 7.Composition (وحدها تعرف خريطة
    /// النوع←الخدمة) ويُحقن هنا كعقد، نفس نمط IDocumentExporter. وجوده يجعل التحقق من تجاوز المتبقي
    /// يقع في الخدمة لا في الواجهة فقط.</summary>
    public interface IPullSourceReader
    {
        decimal GetSourceLineQty(string sourceType, int sourceId, int sourceLineId);
    }
}
