namespace PrimeERP.Application.DTOs.Documents
{
    /// <summary>سطر فاتورةٍ أو مرتجع</summary>
    public class TradeLineDto
    {
        public int     LineNo             { get; set; }
        public string  ProductCode        { get; set; }
        public string  ProductName        { get; set; }
        public decimal Qty                { get; set; }
        public decimal UnitPrice          { get; set; }
        public decimal DiscountPercent    { get; set; }
        public decimal DiscountAmount     { get; set; }
        public decimal VatPercent         { get; set; }
        public decimal VatAmount          { get; set; }
        public decimal WithholdingPercent { get; set; }
        public decimal WithholdingAmount  { get; set; }
        public decimal LineTotal          { get; set; }
        public decimal NetAmount          { get; set; }
        public string  Notes              { get; set; }
    }

    /// <summary>سطر فاتورةٍ أو مرتجع مُدخَل</summary>
    public class CreateTradeLineDto : IPullableLine
    {
        public int     LineNo             { get; set; }
        public string  ProductCode        { get; set; }
        public decimal Qty                { get; set; }
        public decimal UnitPrice          { get; set; }
        public decimal DiscountPercent    { get; set; }
        public decimal VatPercent         { get; set; }
        public decimal WithholdingPercent { get; set; }
        public string  Notes              { get; set; }

        public string  SourceType   { get; set; }
        public int     SourceId     { get; set; }
        public string  SourceNo     { get; set; }
        public int     SourceLineId { get; set; }
    }
}
