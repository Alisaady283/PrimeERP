using System;
using PrimeERP.Application.Services;
using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services.Parties
{
    /// <summary>المنطق المشترك بين العملاء والموردين حول ربط الحساب: إنشاء حساب فرعي عند الإنشاء، الاتجاه
    /// المعاكس (CreateFromAccount/DeleteByAccountCode/UpdateNameFromAccount يستدعيها AccountService)، وإعادة
    /// حساب الرصيد وكشف الحساب من IAccountService دائماً (لا حساب مزدوج).</summary>
    public abstract class PartyServiceBase<TEntity, TDto, TFilter> : CrudServiceBase<TEntity, TDto, TFilter> where TEntity : BaseModel
    {
        protected abstract string AccountSettingKey { get; }
        protected abstract string SequenceKey { get; }
        protected abstract IPartyRepository<TEntity> Repository { get; }
        protected abstract IValidator<TEntity> Validator { get; }
        protected abstract string AccountCodeOf(TEntity entity);
        protected abstract TEntity BuildFromAccount(string code, string name, string accountCode);

        protected readonly IAccountService Accounts;
        protected readonly INumberSequenceService Numbers;
        private readonly IAccountRepository _accountRepo;

        protected PartyServiceBase(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
                                    IAuditLogger audit, IAccountService accounts, INumberSequenceService numbers, IAccountRepository accountRepo)
            : base(permissions, settings, localization, audit)
        {
            Accounts = accounts;
            Numbers = numbers;
            _accountRepo = accountRepo;
        }

        protected Result<Account> GetParentAccount(DbConnection conn = null, DbTransaction tx = null)
        {
            var code = Setting(AccountSettingKey, "");
            if (string.IsNullOrWhiteSpace(code)) return Fail<Account>("AccountNotConfigured");

            var parent = conn != null ? _accountRepo.GetByCode(code, conn, tx) : _accountRepo.GetByCode(code);
            if (parent == null) return Fail<Account>("AccountParentNotFound");
            if (parent.IsLeaf) return Fail<Account>("AccountParentIsLeaf");

            return Ok(parent);
        }

        protected Result<string> CreateLinkedAccount(DbConnection conn, DbTransaction tx, int parentId, string name)
        {
            var result = Accounts.Create(conn, tx, new CreateAccountDto
            {
                ParentId = parentId,
                Name = name,
                IsLeaf = true,
                SkipAutoLink = true // ⚠️ إلزامي — يمنع AccountService.Create من استدعاء CreateFromAccount ثانية (حلقة لا نهائية)
            });

            return result.IsSuccess ? Result.Ok(result.Value.Code) : Result.Fail<string>(result.ErrorMessage, result.ErrorCode);
        }

        /// <summary>الاتجاه المعاكس — يستدعيه AccountService.Create عند الربط التلقائي (حساب أُنشئ بالفعل). لا ينشئ حساباً، ينشئ السجل فقط بالكود الممرَّر. بلا Audit مستقل (AccountService.Create سجّلت العملية).</summary>
        public virtual Result<TDto> CreateFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name)
        {
            var code = Numbers.Next(conn, tx, SequenceKey);
            var entity = BuildFromAccount(code, name, accountCode);

            var validation = Validator.Validate(entity);
            if (!validation.IsValid)
                return Result.Fail<TDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            entity.Id = Repository.Insert(entity, conn, tx);
            return Result.Ok(ToDto(entity));
        }

        /// <summary>الاتجاه المعاكس — يستدعيه AccountService.Delete عند حذف الحساب مباشرة. idempotent: لا سجل مرتبط = لا خطأ.</summary>
        public virtual Result DeleteByAccountCode(DbConnection conn, DbTransaction tx, string accountCode)
        {
            var entity = Repository.GetByAccountCode(accountCode, conn, tx);
            if (entity == null) return Result.Ok();

            Repository.Delete(entity.Id, CurrentUser, conn, tx);
            return Result.Ok();
        }

        /// <summary>الاتجاه المعاكس — يستدعيه AccountService.Update عند تعديل اسم الحساب مباشرة. بلا مزامنة عكسية (يمنع حلقة ping-pong).</summary>
        public virtual Result UpdateNameFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name)
        {
            Repository.UpdateNameByAccountCode(conn, tx, accountCode, name);
            return Result.Ok();
        }

        public virtual Result RecalculateBalance(int id)
        {
            if (!Can("Edit")) return Fail("PermissionDenied");

            var entity = FindById(id);
            if (entity == null) return Fail("NotFound");

            var accountCode = AccountCodeOf(entity);
            if (string.IsNullOrWhiteSpace(accountCode)) return Fail("AccountNotConfigured");

            var balanceResult = Accounts.GetBalanceAsOf(accountCode, DateTime.Today);
            if (!balanceResult.IsSuccess) return Result.Fail(balanceResult.ErrorMessage, balanceResult.ErrorCode);

            Db.RunTransaction((conn, tx) => Repository.SetBalance(id, balanceResult.Value, conn, tx));
            return Result.Ok();
        }

        public virtual Result<List<AccountStatementLine>> GetStatement(int id, DateTime from, DateTime to)
        {
            if (!Can("View")) return Fail<List<AccountStatementLine>>("PermissionDenied");

            var entity = FindById(id);
            if (entity == null) return Fail<List<AccountStatementLine>>("NotFound");

            var accountCode = AccountCodeOf(entity);
            if (string.IsNullOrWhiteSpace(accountCode)) return Fail<List<AccountStatementLine>>("AccountNotConfigured");

            return Accounts.GetStatement(accountCode, from, to);
        }

        protected (StatusVariant Variant, string StatusKey) ComputeStatus(bool isActive, bool isOverLimit) =>
            !isActive ? (StatusVariant.Neutral, "Inactive")
            : isOverLimit ? (StatusVariant.Danger, "OverLimit")
            : (StatusVariant.Success, "Active");

        protected bool CanDeleteWith(bool hasTransactions) => Can("Delete") && !hasTransactions;
    }
}
