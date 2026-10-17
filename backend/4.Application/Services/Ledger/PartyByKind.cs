using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>الطرف بنوعه</summary>
    public sealed class PartyByKind
    {
        private readonly IPartyRepository<Customer> _customers;
        private readonly IPartyRepository<Supplier> _suppliers;

        public PartyByKind(IPartyRepository<Customer> customers, IPartyRepository<Supplier> suppliers)
        {
            _customers = customers;
            _suppliers = suppliers;
        }

        public PartyBase Find(PartyKind kind, int? id) =>
            id == null ? null : kind == PartyKind.Customer ? _customers.GetById(id.Value) : _suppliers.GetById(id.Value);

        public Dictionary<int, string> NamesOf(PartyKind kind, IEnumerable<int?> ids)
        {
            var wanted = ids.Where(id => id != null).Select(id => id.Value).ToList();
            return kind == PartyKind.Customer ? _customers.NamesOf(wanted) : _suppliers.NamesOf(wanted);
        }
    }
}
