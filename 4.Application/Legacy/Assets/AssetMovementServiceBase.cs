using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Legacy.Admin;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Legacy.Accounting;
using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Assets
{
    /// <summary>أساس حركات الأصول</summary>
    public abstract class AssetMovementServiceBase<TEntity, TDto, TFilter>
        : CrudServiceBase<TEntity, TDto, TFilter> where TEntity : BaseModel
    {
        protected readonly Entries Journals;
        protected readonly AccountOf AccountsOf;

        protected AssetMovementServiceBase(IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit,
            Entries journals, AccountOf accountsOf)
            : base(permissions, settings, localization, audit)
        {
            Journals = journals;
            AccountsOf = accountsOf;
        }

        protected override string PermissionPrefix => "Assets";
        protected override string StringPrefix => "Str.Asset";

        protected abstract int? EntryOf(TEntity entity);

        /// <summary>عكس الحركة داخل المعاملة</summary>
        protected abstract Result Undo(PrimeDbContext db, TEntity entity);

        protected virtual object AuditOf(TEntity entity) => null;

        /// <summary>حارس الحذف قبل المعاملة</summary>
        protected virtual Result Guard(TEntity entity) => Result.Ok();

        /// <summary>إنشاءٌ في معاملة</summary>
        protected Result<TDto> Record(Func<PrimeDbContext, Result<TEntity>> write)
        {
            if (!Can("Create")) return FailDenied<TDto>();

            var created = Commit(write);
            if (created.IsFailure) return created.As<TDto>();

            Audit.Log(EntityName, created.Value.Id, AuditAction.Insert, newValue: AuditOf(created.Value));
            return Result.Ok(ToDto(created.Value));
        }

        /// <summary>عكسٌ وإنشاءٌ في معاملةٍ واحدة</summary>
        protected Result Replace(int id, Func<PrimeDbContext, Result<TEntity>> write)
        {
            if (!Can("Edit")) return FailDenied();

            var old = FindById(id);
            if (old == null) return Fail("NotFound", ErrorCode.NotFound);

            var replaced = EnsureReversible(EntryOf(old))
                .Then(() => Commit(db => Undo(db, old).Then(() => write(db))));
            if (replaced.IsFailure) return replaced;

            Audit.Log(EntityName, replaced.Value.Id, AuditAction.Update, oldValue: AuditOf(old), newValue: AuditOf(replaced.Value));
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var entity = FindById(id);
            if (entity == null) return Fail("NotFound", ErrorCode.NotFound);

            var removed = Guard(entity)
                .Then(() => EnsureReversible(EntryOf(entity)))
                .Then(() => Commit(db => Undo(db, entity)));
            if (removed.IsFailure) return removed;

            Audit.Log(EntityName, id, AuditAction.Delete, oldValue: AuditOf(entity));
            return Result.Ok();
        }

        protected Result<string> Account(string key) => AccountsOf.Setting(key, $"{StringPrefix}.AccountsMissing");

        protected int PostEntry(PrimeDbContext db, DateTime date, string description,
            string debitAccount, string creditAccount, decimal amount, string lineNote = null) =>
            Posting.Entry(Journals, db, date, description, EntityName, debitAccount, creditAccount, amount, lineNote);

        protected int PostEntry(PrimeDbContext db, DateTime date, string description,
            List<CreateJournalLineDto> lines) =>
            Posting.Entry(Journals, db, date, description, EntityName, lines);

        protected Result<string> Required(string accountCode, string messageKey) =>
            string.IsNullOrWhiteSpace(accountCode)
                ? Result.Fail<string>(Msg(messageKey), ErrorCode.ValidationFailed)
                : Result.Ok(accountCode);

        protected Result EnsureReversible(int? entryId) => Posting.EnsureReversible(Journals, entryId);

        protected void ReverseEntry(PrimeDbContext db, int? entryId) => Posting.Reverse(Journals, db, entryId);

        /// <summary>أصول الصفحة بضمّةٍ واحدة</summary>
        protected static List<TDto> WithAssets(List<TEntity> entities, IAssetRepository assets, Func<TEntity, int> assetId,
            Func<TEntity, Asset, TDto> map)
        {
            var byId = assets.GetByIds(entities.Select(assetId)).ToDictionary(a => a.Id);
            return entities.Select(e => map(e, byId.GetValueOrDefault(assetId(e)))).ToList();
        }
    }
}
