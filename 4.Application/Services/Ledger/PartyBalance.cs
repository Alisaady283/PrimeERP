using System.Linq;
using System;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>رصيد الطرف من حسابه</summary>
    public static class PartyBalance
    {
        public static void Refresh<T>(IPartyRepository<T> parties, IJournalRepository journal, PrimeDbContext db, int? partyId)
            where T : PartyBase
        {
            if (partyId == null) return;

            var party = parties.GetById(partyId.Value, db);
            if (string.IsNullOrWhiteSpace(party?.AccountCode)) return;

            parties.SetBalance(party.Id, journal.SumPosted(party.AccountCode, null, DateTime.Today, db), db);
        }

        /// <summary>أرصدة الأطراف كلّها من حساباتها</summary>
        public static void RefreshAll<T>(IPartyRepository<T> parties, IJournalRepository journal, PrimeDbContext db) where T : PartyBase
        {
            var sums = journal.GetAccountSums(null, DateTime.Today, postedOnly: true, db).ToDictionary(s => s.AccountCode, s => s.SumDebit - s.SumCredit);
            foreach (var party in parties.GetAll(activeOnly: false).Where(p => !string.IsNullOrWhiteSpace(p.AccountCode)))
                parties.SetBalance(party.Id, sums.GetValueOrDefault(party.AccountCode), db);
        }
    }
}
