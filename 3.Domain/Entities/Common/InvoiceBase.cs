using System;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Domain.Entities.Common
{
    /// <summary>ما تتقاسمه فاتورتا البيع والشراء</summary>
    public abstract class InvoiceBase : BaseModel
    {
        public string    InvoiceNo         { get; set; }
        public DateTime  InvoiceDate       { get; set; }
        public DateTime? DueDate           { get; set; }
        public int?      WarehouseId       { get; set; }
        public int?      CurrencyId        { get; set; }
        public decimal   ExchangeRate      { get; set; } = 1;
        public decimal   SubTotal          { get; set; }
        public decimal   DiscountAmount    { get; set; }
        public decimal   DiscountPercent   { get; set; }
        public decimal   VatAmount         { get; set; }
        public decimal   WithholdingAmount { get; set; }
        public decimal   NetTotal          { get; set; }
        public decimal   PaidAmount        { get; set; }
        public decimal   RemainingAmount   { get; set; }
        public InvoiceStatus Status        { get; set; } = InvoiceStatus.Draft;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public int?      JournalEntryId    { get; set; }
        public string    Notes             { get; set; }
    }

    /// <summary>سطر مستند بيعٍ أو شراء</summary>
    public abstract class DocumentLineBase
    {
        public int     Id                 { get; set; }
        public int     LineNo             { get; set; }
        public int     ProductId          { get; set; }
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

    public abstract class InvoiceLineBase : DocumentLineBase
    {
        public int  InvoiceId { get; set; }
        public int? UnitId    { get; set; }
    }
}
