namespace PrimeERP.Domain.Contracts
{
    /// <summary>يقرأ كمية سطر مستند مصدر</summary>
    public interface IPullSourceReader
    {
        decimal GetSourceLineQty(string sourceType, int sourceId, int sourceLineId);
        string DocNoOf(string docType, int docId);
    }
}
