using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Core;
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
        private readonly IJournalRepository _journal;

        public PartyByKind(IPartyRepository<Customer> customers, IPartyRepository<Supplier> suppliers, IJournalRepository journal)
        {
            _customers = customers;
            _suppliers = suppliers;
            _journal = journal;
        }

        public PartyBase Find(PartyKind kind, int? id) =>
            id == null ? null : kind == PartyKind.Customer ? _customers.GetById(id.Value) : _suppliers.GetById(id.Value);

        public string CodeOf(PartyKind kind, int? id) => Find(kind, id)?.AccountCode;

        public Dictionary<int, string> NamesOf(PartyKind kind, IEnumerable<int?> ids)
        {
            var wanted = ids.Where(id => id != null).Select(id => id.Value).ToList();
            return kind == PartyKind.Customer ? _customers.NamesOf(wanted) : _suppliers.NamesOf(wanted);
        }

        /// <summary>رصيد الطرف من حسابه</summary>
        public void Refresh(PrimeDbContext db, PartyKind kind, int? id)
        {
            if (kind == PartyKind.Customer) PartyBalance.Refresh(_customers, _journal, db, id);
            else PartyBalance.Refresh(_suppliers, _journal, db, id);
        }
    }
}
