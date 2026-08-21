using System;
using System.Collections.Generic;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class PurchaseInvoice : BaseModel
    {
        public string    InvoiceNo       { get; set; }
        public DateTime  InvoiceDate     { get; set; }
        public DateTime? DueDate         { get; set; }
        public int       SupplierId      { get; set; }
        public int?       WarehouseId     { get; set; }
        public int?       CurrencyId      { get; set; }
        public decimal    ExchangeRate    { get; set; } = 1;
        public decimal    SubTotal        { get; set; }
        public decimal    DiscountAmount  { get; set; }
        public decimal    DiscountPercent { get; set; }
        public decimal    TaxAmount       { get; set; }
        public decimal    NetTotal        { get; set; }
        public decimal    PaidAmount      { get; set; }
        public decimal    RemainingAmount { get; set; }
        public InvoiceStatus Status        { get; set; } = InvoiceStatus.Draft;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public int?        JournalEntryId  { get; set; }
        public string       Notes           { get; set; }

        public List<PurchaseInvoiceLine> Lines { get; set; } = new();
    }
}
