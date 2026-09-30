using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>إضافة حساب لكيان صفحة</summary>
    public sealed class AddEntityAccount
    {
        private readonly AddTreeAccount _add;

        public AddEntityAccount(AddTreeAccount add) => _add = add;

        public Result Run<T>(PrimeDbContext db, T entity, IEnumerable<AccountSpec<T>> specs)
        {
            foreach (var spec in specs.Where(s => string.IsNullOrWhiteSpace(s.Get(entity))))
            {
                var created = spec.Parent(db, entity).Then(parent => _add.Run(db, parent, spec.Name(entity), spec.Leaf));

                if (created.IsSuccess) spec.Set(entity, created.Value.Code);
                else if (!spec.Optional) return created;
            }

            return Result.Ok();
        }

        public static List<string> Codes<T>(T entity, IEnumerable<AccountSpec<T>> specs) =>
            specs.Select(s => s.Get(entity)).Where(code => !string.IsNullOrWhiteSpace(code)).ToList();

        public static List<string> Names<T>(T entity, IEnumerable<AccountSpec<T>> specs) =>
            specs.Select(s => s.Name(entity)).ToList();

        /// <summary>حسابات المخزَّن للمُدخل الفارغ</summary>
        public static void Keep<T>(T input, T stored, IEnumerable<AccountSpec<T>> specs)
        {
            foreach (var spec in specs.Where(s => string.IsNullOrWhiteSpace(s.Get(input))))
                spec.Set(input, spec.Get(stored));
        }
    }
}
