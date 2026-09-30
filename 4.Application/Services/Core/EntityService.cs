using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Ledger.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
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
        where TEntity : BaseModel
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
        protected virtual Result CanErase(TEntity entity) => Result.Ok();
        protected virtual Result OnSaved(PrimeDbContext db, TEntity entity) => Result.Ok();
        protected virtual object AuditValue(TEntity entity) => null;
        protected virtual string CreateDetails(TEntity entity) => null;
        protected virtual string DeleteDetails(TEntity entity) => null;

        public virtual Result<TDto> Create(TCreate dto) => Can("Create") ? Add(dto) : FailDenied<TDto>();

        /// <summary>الإنشاء بلا صلاحية للبذر</summary>
        protected Result<TDto> Add(TCreate dto)
        {
            var entity = New(dto);
            if (SequenceKey != null) Number(entity, Numbers.Next(SequenceKey));

            var saved = Valid(entity).Then(() => Commit(db => CreateCore(db, entity)));
            if (saved.IsFailure) return saved.As<TDto>();

            Audit.Log(EntityName, entity.Id, AuditAction.Insert, newValue: AuditValue(entity), details: CreateDetails(entity));
            return Ok(ToDto(entity));
        }

        /// <summary>الإنشاء في معاملة المستدعي</summary>
        public Result<TDto> Create(PrimeDbContext db, TCreate dto)
        {
            var entity = New(dto);
            if (SequenceKey != null) Number(entity, Numbers.Next(db, SequenceKey));
            return Valid(entity).Then(() => CreateCore(db, entity)).Then(() => Result.Ok(ToDto(entity)));
        }

        /// <summary>الإنشاء داخل معاملةٍ قائمة</summary>
        protected Result CreateCore(PrimeDbContext db, TEntity entity) =>
            OpenAccounts(db, entity).Then(() =>
            {
                entity.Id = Insert(db, entity);
                return OnSaved(db, entity);
            });

        public virtual Result Update(TUpdate dto)
        {
            if (!Can("Edit")) return FailDenied();

            var stored = FindById(IdOf(dto));
            if (stored == null) return Fail("NotFound", ErrorCode.NotFound);

            var names = AddEntityAccount.Names(stored, Accounts);
            var entity = Edited(stored, dto);

            var saved = Valid(entity).Then(() => Commit(db => OpenAccounts(db, entity).Then(() =>
            {
                Save(db, entity);
                Tree?.Rename.Run(db, entity, Accounts, names);
                return OnSaved(db, entity);
            })));
            if (saved.IsFailure) return saved;

            Audit.Log(EntityName, entity.Id, AuditAction.Update, newValue: AuditValue(entity));
            return Result.Ok();
        }

        public virtual Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var entity = FindById(id);
            if (entity == null) return Fail("NotFound", ErrorCode.NotFound);

            if (Accounts.Count > 0 && Tree.Guards.HasEntries(AddEntityAccount.Codes(entity, Accounts)))
                return Fail("HasTransactions", ErrorCode.ValidationFailed);

            var erased = CanErase(entity).Then(() => Commit(db =>
            {
                Tree?.Close.Run(db, entity, Accounts);
                Erase(db, entity);
                return Result.Ok();
            }));
            if (erased.IsFailure) return erased;

            Audit.Log(EntityName, id, AuditAction.Delete, details: DeleteDetails(entity));
            return Result.Ok();
        }

        /// <summary>المُدخل كياناً بحسابات المخزَّن</summary>
        private TEntity Edited(TEntity stored, TUpdate dto)
        {
            if (dto is not TEntity input)
            {
                Apply(stored, dto);
                return stored;
            }

            foreach (var account in Accounts.Where(a => string.IsNullOrWhiteSpace(a.Get(input))))
                account.Set(input, account.Get(stored));
            return input;
        }

        private Result OpenAccounts(PrimeDbContext db, TEntity entity) =>
            Accounts.Count == 0 ? Result.Ok() : Tree.Add.Run(db, entity, Accounts);

        private Result Valid(TEntity entity) => Check.Valid(entity, Fields);
    }
}
