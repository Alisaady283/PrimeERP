using System;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>حسابٌ ومجمّعه لأي كيان</summary>
    public static class AddMirroredAccount
    {
        public static string MirrorName(string name) => LocalizationService.Get("Str.Category.MirrorName", name);

        public static AccountSpec<T>[] Specs<T>(
            Func<PrimeDbContext, T, Result<Account>> parent, Func<PrimeDbContext, T, Result<Account>> mirrorParent,
            Func<T, string> name, Func<T, string> get, Action<T, string> set,
            Func<T, string> getMirror, Action<T, string> setMirror, bool leaf = true) => new[]
        {
            new AccountSpec<T>(parent, name, get, set, leaf),
            new AccountSpec<T>(mirrorParent, e => MirrorName(name(e)), getMirror, setMirror, leaf)
        };
    }
}
