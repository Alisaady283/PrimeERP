using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using System.Collections.Generic;

namespace PrimeERP.Data.Repositories
{
    public class SalesInvoiceRepository : InvoiceRepositoryBase<SalesInvoice, SalesInvoiceLine>, IInvoiceRepository<SalesInvoice, SalesInvoiceLine>
    {
        protected override string HeaderTable => "SalesInvoices";
        protected override string LinesTable  => "SalesInvoiceLines";
        protected override string PartyColumn => "CustomerId";

        protected override int  PartyIdOf(SalesInvoice invoice) => invoice.CustomerId;
        protected override void ApplyPartyId(SalesInvoice invoice, int partyId) => invoice.CustomerId = partyId;
    }
}
