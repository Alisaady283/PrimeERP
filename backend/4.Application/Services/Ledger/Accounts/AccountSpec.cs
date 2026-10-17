using System;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>حساب كيانٍ بمعاملاته</summary>
    public sealed record AccountSpec<T>(
        Func<PrimeDbContext, T, Result<Account>> Parent,
        Func<T, string> Name,
        Func<T, string> Get,
        Action<T, string> Set,
        bool Leaf = true,
        bool Optional = false);
}
