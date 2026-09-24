using PrimeERP.Data.Core;
using System;
using PrimeERP.Application.Services;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;
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

namespace PrimeERP.Application.Services.Parties
{
    /// <summary>المنطق المشترك بين العملاء والموردين</summary>
    public abstract class PartyServiceBase<TEntity, TDto, TFilter> : CrudServiceBase<TEntity, TDto, TFilter>, IAccountLinkedService where TEntity : BaseModel
    {
        protected abstract string AccountSettingKey { get; }
        protected abstract string SequenceKey { get; }
        protected abstract IPartyRepository<TEntity> Repository { get; }
        protected abstract IValidator<TEntity> Validator { get; }
        protected abstract string AccountCodeOf(TEntity entity);
        protected abstract decimal CreditLimitOf(TEntity entity);
        protected abstract decimal BalanceOf(TEntity entity);
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

        protected Result<Account> GetParentAccount(PrimeDbContext db = null)
        {
            var code = Setting(AccountSettingKey, "");
            if (string.IsNullOrWhiteSpace(code)) return Fail<Account>("AccountNotConfigured");

            var parent = _accountRepo.GetByCode(code, db);
            if (parent == null) return Fail<Account>("AccountParentNotFound", ErrorCode.NotFound);
            if (parent.IsLeaf) return Fail<Account>("AccountParentIsLeaf", ErrorCode.ValidationFailed);

            return Ok(parent);
        }

        protected Result<string> CreateLinkedAccount(PrimeDbContext db, int parentId, string name)
        {
            var result = Accounts.Create(db, new CreateAccountDto
            {
                ParentId = parentId,
                Name = name,
                IsLeaf = true,
                SkipAutoLink = true // ⚠️ required: prevents an endless link loop
            });

            return result.IsSuccess ? Result.Ok(result.Value.Code) : Result.Fail<string>(result.ErrorMessage, result.ErrorCode);
        }

        Result IAccountLinkedService.CreateFromAccount(PrimeDbContext db, string accountCode, string name, string rootCode) =>
            CreateFromAccount(db, accountCode, name);

        public virtual Result<TDto> CreateFromAccount(PrimeDbContext db, string accountCode, string name)
        {
            var code = Numbers.Next(db, SequenceKey);
            var entity = BuildFromAccount(code, name, accountCode);

            var check = Check(Validator, entity);
            if (check.IsFailure) return check.As<TDto>();

            entity.Id = Repository.Insert(entity, db);
            return Result.Ok(ToDto(entity));
        }

        public virtual Result DeleteByAccountCode(PrimeDbContext db, string accountCode)
        {
            var entity = Repository.GetByAccountCode(accountCode, db);
            if (entity == null) return Result.Ok();

            Repository.Delete(entity.Id, CurrentUser, db);
            return Result.Ok();
        }

        public virtual Result UpdateNameFromAccount(PrimeDbContext db, string accountCode, string name)
        {
            Repository.UpdateNameByAccountCode(db, accountCode, name);
            return Result.Ok();
        }

        public virtual Result RecalculateBalance(int id)
        {
            if (!Can("Edit")) return FailDenied();

            var entity = FindById(id);
            if (entity == null) return Fail("NotFound", ErrorCode.NotFound);

            var accountCode = AccountCodeOf(entity);
            if (string.IsNullOrWhiteSpace(accountCode)) return Fail("AccountNotConfigured");

            var balanceResult = Accounts.GetBalanceAsOf(accountCode, DateTime.Today);
            if (!balanceResult.IsSuccess) return Result.Fail(balanceResult.ErrorMessage, balanceResult.ErrorCode);

            Tx(db => Repository.SetBalance(id, balanceResult.Value, db));
            return Result.Ok();
        }

        public virtual Result<List<AccountStatementLine>> GetStatement(int id, DateTime from, DateTime to)
        {
            if (!Can("View")) return FailDenied<List<AccountStatementLine>>();

            var entity = FindById(id);
            if (entity == null) return Fail<List<AccountStatementLine>>("NotFound", ErrorCode.NotFound);

            var accountCode = AccountCodeOf(entity);
            if (string.IsNullOrWhiteSpace(accountCode)) return Fail<List<AccountStatementLine>>("AccountNotConfigured");

            var statement = Accounts.GetStatement(accountCode, from, to);
            if (statement.IsFailure || Cheques == null) return statement;

            var open = Cheques.GetOpenForParty(id, from, to);
            if (open.IsFailure || open.Value.Count == 0) return statement;

            var running = statement.Value.Count > 0 ? statement.Value[^1].RunningBalance : 0m;
            foreach (var cheque in open.Value)
                statement.Value.Add(new AccountStatementLine
                {
                    Date = cheque.DueDate.ToString("yyyy-MM-dd"),
                    EntryNo = cheque.ChequeNo,
                    Description = $"شيك {cheque.ChequeNo} — {cheque.BankName} ({cheque.StatusName})",
                    Debit = 0, Credit = 0,
                    MemoAmount = cheque.Amount,
                    RunningBalance = running,
                    SourceType = "Cheque"
                });

            return statement;
        }

        protected virtual PrimeERP.Application.Services.Cheques.IChequeService Cheques => null;

        public virtual Result RecalculateAllBalances()
        {
            if (!Can("Edit")) return FailDenied();

            var entities = Repository.GetAll(activeOnly: false).Where(e => !string.IsNullOrWhiteSpace(AccountCodeOf(e))).ToList();

            var balances = new List<(int Id, decimal Balance)>();
            foreach (var e in entities)
            {
                var balanceResult = Accounts.GetBalanceAsOf(AccountCodeOf(e), DateTime.Today);
                if (balanceResult.IsSuccess) balances.Add((e.Id, balanceResult.Value));
            }

            Tx(db =>
            {
                foreach (var (id, balance) in balances)
                    Repository.SetBalance(id, balance, db);
            });

            return Result.Ok();
        }

        public virtual Result<CreditCheckResult> CheckCreditLimit(int id, decimal additional)
        {
            if (!Can("View")) return FailDenied<CreditCheckResult>();

            var entity = FindById(id);
            if (entity == null) return Fail<CreditCheckResult>("NotFound", ErrorCode.NotFound);

            var creditLimit = CreditLimitOf(entity);
            var balance = BalanceOf(entity);

            if (creditLimit <= 0)
                return Result.Ok(new CreditCheckResult
                {
                    IsAllowed = true, CurrentBalance = balance, CreditLimit = 0,
                    AvailableCredit = decimal.MaxValue, ExceededBy = 0m // 0 = لا حد؛ AvailableCredit بلا سقف فعلي
                });

            var projectedBalance = balance + additional;
            var exceededBy = projectedBalance > creditLimit ? projectedBalance - creditLimit : 0m;

            var result = new CreditCheckResult
            {
                IsAllowed       = exceededBy == 0m,
                CurrentBalance  = balance,
                CreditLimit     = creditLimit,
                AvailableCredit = creditLimit - balance,
                ExceededBy      = exceededBy
            };

            return exceededBy > 0m
                ? Result.Fail<CreditCheckResult>($"{Msg("CreditLimitExceeded")}: {exceededBy:N2}", ErrorCode.ValidationFailed)
                : Result.Ok(result);
        }

        protected (StatusVariant Variant, string StatusKey) ComputeStatus(bool isActive, bool isOverLimit) =>
            !isActive ? (StatusVariant.Neutral, "Inactive")
            : isOverLimit ? (StatusVariant.Danger, "OverLimit")
            : (StatusVariant.Success, "Active");

        protected bool CanDeleteWith(bool hasTransactions) => Can("Delete") && !hasTransactions;
    }
}
