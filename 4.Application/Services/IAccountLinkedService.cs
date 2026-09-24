using PrimeERP.Data.Core;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services
{
    /// <summary>كيان يعيش ورقةً في شجرة</summary>
    public interface IAccountLinkedService
    {
        Result CreateFromAccount(PrimeDbContext db, string accountCode, string name, string rootCode);
        Result UpdateNameFromAccount(PrimeDbContext db, string accountCode, string name);
        Result DeleteByAccountCode(PrimeDbContext db, string accountCode);
    }
}
