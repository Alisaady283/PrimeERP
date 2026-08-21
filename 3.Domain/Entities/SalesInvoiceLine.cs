namespace PrimeERP.Domain.Entities
{
    public class SalesInvoiceLine
    {
        public int     Id              { get; set; }
        public int     InvoiceId       { get; set; }
        public int     LineNo          { get; set; }
        public int     ProductId       { get; set; }
        public string  ProductCode     { get; set; }
        public string  ProductName     { get; set; }
        public decimal Qty             { get; set; }
        public int?    UnitId          { get; set; }
        public decimal UnitPrice       { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount  { get; set; }
        public decimal TaxPercent      { get; set; }
        public decimal TaxAmount       { get; set; }
        public decimal LineTotal       { get; set; }
        public string  Notes           { get; set; }
    }
}
