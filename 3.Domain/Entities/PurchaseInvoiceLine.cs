namespace PrimeERP.Domain.Entities
{
    public class PurchaseInvoiceLine
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
        public decimal VatPercent      { get; set; }
        public decimal VatAmount       { get; set; }
        public decimal WithholdingPercent { get; set; }
        public decimal WithholdingAmount  { get; set; }
        /// <summary>الكمية × السعر قبل أي خصم أو ضريبة.</summary>
        public decimal LineTotal       { get; set; }
        /// <summary>صافي المبلغ: الوعاء + القيمة المضافة − الخصم والإضافة.</summary>
        public decimal NetAmount       { get; set; }
        public string  Notes           { get; set; }
    }
}
