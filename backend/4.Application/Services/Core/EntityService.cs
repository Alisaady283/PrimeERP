using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Ledger.Accounts;
using System;
using System.Collections.Generic;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Core
{
    /// <summary>إضافة الكيان وتعديله وحذفه</summary>
    public abstract class EntityService<TEntity, TDto, TCreate, TUpdate, TFilter> : CrudServiceBase<TEntity, TDto, TFilter>
        where TEntity : class, IEntity
    {
        protected readonly INumberSequenceService Numbers;
        protected readonly AccountCases Tree;

        protected EntityService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, INumberSequenceService numbers = null, AccountCases tree = null)
            : base(permissions, settings, localization, audit)
        {
            Numbers = numbers;
            Tree = tree;
        }

        protected virtual TEntity New(TCreate dto) => dto as TEntity;
        protected virtual void Apply(TEntity entity, TUpdate dto) { }
        protected virtual int IdOf(TUpdate dto) => (dto as TEntity)?.Id ?? 0;
        protected abstract int Insert(PrimeDbContext db, TEntity entity);
        protected abstract void Save(PrimeDbContext db, TEntity entity);
        protected abstract void Erase(PrimeDbContext db, TEntity entity);

        protected virtual Field<TEntity>[] Fields => Array.Empty<Field<TEntity>>();
        protected virtual string SequenceKey => null;
        protected virtual void Number(TEntity entity, string code) { }
        protected virtual IReadOnlyList<AccountSpec<TEntity>> Accounts => Array.Empty<AccountSpec<TEntity>>();

        /// <summary>حسابات السجل بعينه</summary>
        protected virtual IReadOnlyList<AccountSpec<TEntity>> AccountsOf(TEntity entity) => Accounts;

        protected virtual Result CanErase(TEntity entity) => Result.Ok();
        protected virtual int? OwnEntry(TEntity entity) => null;
        protected virtual Result Prepare(TEntity entity, TEntity stored) => Result.Ok();
        protected virtual Result OnSaved(PrimeDbContext db, TEntity entity, TEntity stored) => Result.Ok();
        protected virtual object AuditValue(TEntity entity) => null;
        protected virtual string CreateDetails(TEntity entity) => null;
        protected virtual string DeleteDetails(TEntity entity) => null;

        public virtual Result<TDto> Create(TCreate dto)
        {
            var entity = New(dto);
            return CanOn(entity, "Create") ? Store(entity) : FailDenied<TDto>();
        }

        /// <summary>الإنشاء بلا صلاحية للبذر</summary>
        protected Result<TDto> Add(TCreate dto) => Store(New(dto));

        /// <summary>الإنشاء في معاملة المستدعي</summary>
        public Result<TDto> Create(PrimeDbContext db, TCreate dto)
        {
            var entity = New(dto);
            return CreateIn(db, entity).Then(() => Result.Ok(ToDto(entity)));
        }

        /// <summary>كيانٌ جديد في معاملةٍ قائمة</summary>
        protected Result CreateIn(PrimeDbContext db, TEntity entity)
        {
            if (SequenceKey != null) Number(entity, Numbers.Next(db, SequenceKey));
            return Valid(entity).Then(() => Prepare(entity, null)).Then(() => CreateCore(db, entity));
        }

        /// <summary>الإنشاء داخل معاملةٍ قائمة</summary>
        protected Result CreateCore(PrimeDbContext db, TEntity entity) =>
            OpenAccounts(db, entity).Then(() =>
            {
                entity.Id = Insert(db, entity);
                return OnSaved(db, entity, null);
            });

        public virtual Result Update(TUpdate dto)
        {
            var stored = FindById(IdOf(dto));
            if (stored == null) return Can("Edit") ? Fail("NotFound", ErrorCode.NotFound) : FailDenied();
            if (!CanOn(stored, "Edit")) return FailDenied();

            var names = AddEntityAccount.Names(stored, AccountsOf(stored));
            var entity = Edited(stored, dto);

            var saved = Valid(entity).Then(() => Prepare(entity, stored)).Then(() => Commit(db => OpenAccounts(db, entity).Then(() =>
            {
                Save(db, entity);
                Tree?.Rename.Run(db, entity, AccountsOf(entity), names);
                return OnSaved(db, entity, stored);
            })));
            if (saved.IsFailure) return saved;

            Audit.Log(EntityName, entity.Id, AuditAction.Update, newValue: AuditValue(entity));
            return Result.Ok();
        }

        public virtual Result Delete(int id)
        {
            var entity = FindById(id);
            if (entity == null) return Can("Delete") ? Fail("NotFound", ErrorCode.NotFound) : FailDenied();
            if (!CanOn(entity, "Delete")) return FailDenied();

            var accounts = AccountsOf(entity);
            if (accounts.Count > 0 && Tree.Guards.HasEntries(AddEntityAccount.Codes(entity, accounts), OwnEntry(entity)))
                return Fail("HasTransactions", ErrorCode.ValidationFailed);

            var erased = CanErase(entity).Then(() => Commit(db =>
            {
                Erase(db, entity);
                Tree?.Close.Run(db, entity, accounts);
                return Result.Ok();
            }));
            if (erased.IsFailure) return erased;

            Audit.Log(EntityName, id, AuditAction.Delete, details: DeleteDetails(entity));
            return Result.Ok();
        }

        private Result<TDto> Store(TEntity entity)
        {
            if (SequenceKey != null) Number(entity, Numbers.Next(SequenceKey));

            var saved = Valid(entity).Then(() => Prepare(entity, null)).Then(() => Commit(db => CreateCore(db, entity)));
            if (saved.IsFailure) return saved.As<TDto>();

            Audit.Log(EntityName, entity.Id, AuditAction.Insert, newValue: AuditValue(entity), details: CreateDetails(entity));
            return Ok(ToDto(entity));
        }

        /// <summary>المُدخل كياناً بحسابات المخزَّن</summary>
        private TEntity Edited(TEntity stored, TUpdate dto)
        {
            if (dto is not TEntity input)
            {
                Apply(stored, dto);
                return stored;
            }

            AddEntityAccount.Keep(input, stored, AccountsOf(stored));
            return input;
        }

        private Result OpenAccounts(PrimeDbContext db, TEntity entity)
        {
            var accounts = AccountsOf(entity);
            return accounts.Count == 0 ? Result.Ok() : Tree.Add.Run(db, entity, accounts);
        }

        private Result Valid(TEntity entity) => Check.Valid(entity, Fields);
    }
}
