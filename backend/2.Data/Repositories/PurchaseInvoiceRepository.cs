using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using System.Collections.Generic;

namespace PrimeERP.Data.Repositories
{
    public class PurchaseInvoiceRepository : InvoiceRepositoryBase<PurchaseInvoice, PurchaseInvoiceLine>, IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine>
    {
        protected override string HeaderTable => "PurchaseInvoices";
        protected override string LinesTable  => "PurchaseInvoiceLines";
        protected override string PartyColumn => "SupplierId";

        protected override int  PartyIdOf(PurchaseInvoice invoice) => invoice.SupplierId;
        protected override void ApplyPartyId(PurchaseInvoice invoice, int partyId) => invoice.SupplierId = partyId;
    }
}
