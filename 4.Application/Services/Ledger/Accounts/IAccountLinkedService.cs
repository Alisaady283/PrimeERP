using PrimeERP.Data.Core;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>كيان يعيش ورقةً في شجرة</summary>
    public interface IAccountLinkedService
    {
        string[] RootKeys { get; }
        Result CreateFromAccount(PrimeDbContext db, string accountCode, string name, string rootCode);
        Result UpdateNameFromAccount(PrimeDbContext db, string accountCode, string name);
        Result DeleteByAccountCode(PrimeDbContext db, string accountCode);
    }
}
