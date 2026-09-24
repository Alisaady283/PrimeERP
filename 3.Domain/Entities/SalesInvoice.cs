using System.Collections.Generic;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان SalesInvoice</summary>
    public class SalesInvoice : InvoiceBase
    {
        public int  CustomerId { get; set; }
        public int? SalesRepId { get; set; }

        public List<SalesInvoiceLine> Lines { get; set; } = new();
    }

    public class SalesInvoiceLine : InvoiceLineBase
    {
    }
}
