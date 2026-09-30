using PrimeERP.Data.Core;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>حذف الحساب وكيانه</summary>
    public sealed class CloseLinkedAccount
    {
        private readonly CloseAccount _close;

        public CloseLinkedAccount(CloseAccount close) => _close = close;

        public Result Run(PrimeDbContext db, string code, IAccountLinkedService linked)
        {
            _close.Run(db, code);
            return linked?.DeleteByAccountCode(db, code) ?? Result.Ok();
        }
    }
}
