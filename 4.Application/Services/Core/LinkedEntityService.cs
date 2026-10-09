using System.Linq;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Core
{
    /// <summary>كيانٌ يعيش ورقةً في الشجرة</summary>
    public abstract class LinkedEntityService<TEntity, TFilter>
        : EntityService<TEntity, TEntity, TEntity, TEntity, TFilter>, IAccountLinkedService
        where TEntity : class, IEntity
    {
        protected LinkedEntityService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, INumberSequenceService numbers, AccountCases tree)
            : base(permissions, settings, localization, audit, numbers, tree) { }

        public abstract string[] RootKeys { get; }

        /// <summary>الكيان من حسابه</summary>
        protected abstract TEntity FromAccount(string accountCode, string name, string rootCode);

        protected abstract TEntity FindByAccount(PrimeDbContext db, string accountCode);

        protected abstract void RenameByAccount(PrimeDbContext db, string accountCode, string name);

        public Result CreateFromAccount(PrimeDbContext db, string accountCode, string name, string rootCode) =>
            FindByAccount(db, accountCode) != null ? Result.Ok() : CreateIn(db, FromAccount(accountCode, name, rootCode));

        public Result UpdateNameFromAccount(PrimeDbContext db, string accountCode, string name)
        {
            RenameByAccount(db, accountCode, name);
            return Result.Ok();
        }

        public Result RepairMissingEntities()
        {
            foreach (var root in RootKeys.Select(key => Setting(key, "")).Where(code => !string.IsNullOrWhiteSpace(code)))
                foreach (var account in Tree.Rows.GetAllChildren(root).Where(a => a.IsLeaf && a.IsActive))
                    Commit(db => CreateFromAccount(db, account.Code, account.Name, root));
            return Result.Ok();
        }

        public Result DeleteByAccountCode(PrimeDbContext db, string accountCode)
        {
            if (FindByAccount(db, accountCode) is { } entity) Erase(db, entity);
            return Result.Ok();
        }
    }
}
