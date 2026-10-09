using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.PageServices.Accounting;
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

namespace PrimeERP.Application.PageServices.Assets
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

        /// <summary>إنشاءٌ في معاملة</summary>
        protected Result<TDto> Record(Func<PrimeDbContext, Result<TEntity>> write)
        {
            if (!Can("Create")) return FailDenied<TDto>();

            var created = Commit(write);
            if (created.IsFailure) return created.As<TDto>();

            Audit.Log(EntityName, created.Value.Id, AuditAction.Insert, newValue: AuditOf(created.Value));
            return Result.Ok(ToDto(FindById(created.Value.Id)));
        }

        /// <summary>عكسٌ وإنشاءٌ في معاملةٍ واحدة</summary>
        protected Result Replace(TEntity input, Func<PrimeDbContext, TEntity, Result<TEntity>> write)
        {
            if (!Can("Edit")) return FailDenied();

            var old = FindById(input.Id);
            if (old == null) return Fail("NotFound", ErrorCode.NotFound);

            var replaced = Posting.EnsureReversible(Journals, EntryOf(old))
                .Then(() => Commit(db => Undo(db, old).Then(() => write(db, Fresh(input)))));
            if (replaced.IsFailure) return replaced;

            Audit.Log(EntityName, replaced.Value.Id, AuditAction.Update, oldValue: AuditOf(old), newValue: AuditOf(replaced.Value));
            return Result.Ok();
        }

        /// <summary>البديل صفٌّ جديد</summary>
        private static TEntity Fresh(TEntity entity)
        {
            (entity.Id, entity.CreatedAt, entity.CreatedBy, entity.UpdatedAt, entity.UpdatedBy) = (0, default, null, default, null);
            return entity;
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var entity = FindById(id);
            if (entity == null) return Fail("NotFound", ErrorCode.NotFound);

            var removed = Posting.EnsureReversible(Journals, EntryOf(entity))
                .Then(() => Commit(db => Undo(db, entity)));
            if (removed.IsFailure) return removed;

            Audit.Log(EntityName, id, AuditAction.Delete, oldValue: AuditOf(entity));
            return Result.Ok();
        }
    }
}
