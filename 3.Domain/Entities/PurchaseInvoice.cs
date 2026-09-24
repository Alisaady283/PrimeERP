using System.Collections.Generic;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان PurchaseInvoice</summary>
    public class PurchaseInvoice : InvoiceBase
    {
        public int SupplierId { get; set; }

        public List<PurchaseInvoiceLine> Lines { get; set; } = new();
    }

    public class PurchaseInvoiceLine : InvoiceLineBase
    {
    }
}
