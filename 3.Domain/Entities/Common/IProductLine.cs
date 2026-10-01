namespace PrimeERP.Domain.Entities.Common
{
    /// <summary>سطرٌ بصنفه</summary>
    public interface IProductLine
    {
        int    ProductId   { get; set; }
        string ProductCode { get; set; }
        string ProductName { get; set; }
    }
}
