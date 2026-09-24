using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using System.Collections.Generic;

namespace PrimeERP.Data.Repositories
{
    public class PurchaseReturnRepository : ReturnRepositoryBase<PurchaseReturn, PurchaseReturnLine>, IReturnRepository<PurchaseReturn, PurchaseReturnLine>
    {
        protected override string HeaderTable => "PurchaseReturns";
        protected override string LinesTable  => "PurchaseReturnLines";
        protected override string PartyColumn => "SupplierId";

        protected override int  PartyIdOf(PurchaseReturn ret) => ret.SupplierId;
        protected override void ApplyPartyId(PurchaseReturn ret, int partyId) => ret.SupplierId = partyId;
    }
}
