using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>الكيان بلا حساب: ورقةٌ باسمه في جذره وإلا جديدة</summary>
    public sealed class RepairAccounts
    {
        private readonly IAccountRepository _accounts;
        private readonly AddEntityAccount _add;

        public RepairAccounts(IAccountRepository accounts, AddEntityAccount add)
        {
            _accounts = accounts;
            _add = add;
        }

        /// <summary>كلٌّ في معاملته، والفاشل لا يوقف غيره</summary>
        public void Run<T>(IReadOnlyCollection<T> all, Func<T, bool> wanted, AccountSpec<T> spec, Func<T, string> rootCode,
            Func<Func<PrimeDbContext, Result>, Result> commit, Action<PrimeDbContext, T> save)
        {
            var linked = all.Select(spec.Get).Where(code => !string.IsNullOrWhiteSpace(code)).ToHashSet();

            foreach (var entity in all.Where(e => wanted(e) && string.IsNullOrWhiteSpace(spec.Get(e))).ToList())
            {
                spec.Set(entity, _accounts.LeafNamed(rootCode(entity), spec.Name(entity), linked)?.Code);

                var repaired = commit(db => _add.Run(db, entity, new[] { spec }).Then(() =>
                {
                    save(db, entity);
                    return Result.Ok();
                }));

                if (repaired.IsSuccess) linked.Add(spec.Get(entity));
            }
        }
    }
}
