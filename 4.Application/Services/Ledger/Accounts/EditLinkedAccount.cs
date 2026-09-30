using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>تعديل الحساب واسم كيانه</summary>
    public sealed class EditLinkedAccount
    {
        private readonly EditTreeAccount _edit;

        public EditLinkedAccount(EditTreeAccount edit) => _edit = edit;

        public Result Run(PrimeDbContext db, Account account, string name, string notes, bool isActive, IAccountLinkedService linked)
        {
            var renamed = account.Name != name;
            return _edit.Run(db, account, name, notes, isActive).Then(() =>
                renamed && linked != null ? linked.UpdateNameFromAccount(db, account.Code, name) : Result.Ok());
        }
    }
}
