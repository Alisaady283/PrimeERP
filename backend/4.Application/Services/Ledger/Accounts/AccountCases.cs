using PrimeERP.Platform.Localization;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>حالات الحساب التي يستدعيها الكيان</summary>
    public sealed record AccountCases(AddEntityAccount Add, RenameAccount Rename, CloseAccount Close, Guards Guards,
        IAccountRepository Rows)
    {
        public Account Find(PrimeDbContext db, string code) => string.IsNullOrWhiteSpace(code) ? null : Rows.GetByCode(code, db);

        /// <summary>جذرٌ موجودٌ يقبل الأبناء</summary>
        public Result<Account> Root(PrimeDbContext db, string code, string missingKey, string leafKey = null, params object[] args)
        {
            var root = Find(db, code);
            if (root == null) return Result.Fail<Account>(LocalizationService.Get(missingKey, args), ErrorCode.ValidationFailed);
            return leafKey != null && root.IsLeaf
                ? Result.Fail<Account>(LocalizationService.Get(leafKey, args), ErrorCode.ValidationFailed)
                : Result.Ok(root);
        }
    }
}
