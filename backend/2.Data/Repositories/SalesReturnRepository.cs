using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using System.Collections.Generic;

namespace PrimeERP.Data.Repositories
{
    public class SalesReturnRepository : ReturnRepositoryBase<SalesReturn, SalesReturnLine>, IReturnRepository<SalesReturn, SalesReturnLine>
    {
        protected override string HeaderTable => "SalesReturns";
        protected override string LinesTable  => "SalesReturnLines";
        protected override string PartyColumn => "CustomerId";

        protected override int  PartyIdOf(SalesReturn ret) => ret.CustomerId;
        protected override void ApplyPartyId(SalesReturn ret, int partyId) => ret.CustomerId = partyId;
    }
}
