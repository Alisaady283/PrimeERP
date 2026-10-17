namespace PrimeERP.Application.DTOs.Documents
{
    /// <summary>سطرٌ قابل للسحب</summary>
    public interface IPullableLine
    {
        string SourceType   { get; set; }
        int    SourceId     { get; set; }
        string SourceNo     { get; set; }
        int    SourceLineId { get; set; }
    }
}
