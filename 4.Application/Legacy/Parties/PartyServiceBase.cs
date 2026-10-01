using PrimeERP.Application.Validation;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Parties
{
    /// <summary>المنطق المشترك بين العملاء والموردين</summary>
    public abstract class PartyServiceBase<TEntity, TFilter> : LinkedEntityService<TEntity, TFilter>
        where TEntity : PartyBase, new()
    {
        protected abstract string AccountSettingKey { get; }
        protected abstract IPartyRepository<TEntity> Repository { get; }
        protected virtual PrimeERP.Application.Legacy.Cheques.IChequeService Cheques => null;

        private readonly Statement _statement;
        private readonly AccountBalances _balances;
        private readonly AccountSpec<TEntity>[] _account;

        protected PartyServiceBase(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
                                    IAuditLogger audit, Statement statement, INumberSequenceService numbers,
                                    AccountBalances balances, AccountCases tree)
            : base(permissions, settings, localization, audit, numbers, tree)
        {
            _statement = statement;
            _balances = balances;
            _account = new[] { new AccountSpec<TEntity>((db, _) => ParentAccount(db), e => e.Name, e => e.AccountCode, (e, code) => e.AccountCode = code) };
        }

        protected override IReadOnlyList<AccountSpec<TEntity>> Accounts => _account;

        /// <summary>شروط الطرف بعنوانيه</summary>
        protected static Field<TEntity>[] RulesOf(string codeLabel, string nameLabel) => new Field<TEntity>[]
        {
            new(x => x.Code, codeLabel, Required: true),
            new(x => x.Name, nameLabel, Required: true, Max: 150),
            new(x => x.Phone, "Str.Field.Phone", Format: FieldFormat.Phone),
            new(x => x.Email, "Str.Email", Format: FieldFormat.Email),
            new(x => x.CreditLimit, "Str.CreditLimit", From: 0),
        };

        private Result<Account> ParentAccount(PrimeDbContext db) =>
            Tree.Root(db, Setting(AccountSettingKey, ""), "Str.Party.AccountParentNotFound", "Str.Party.AccountParentIsLeaf");

        protected override TEntity FindById(int id) => Repository.GetById(id);
        protected override List<TEntity> FindSearch(string term, int maxResults) => Repository.Search(term, maxResults);

        protected override void Number(TEntity entity, string code) => entity.Code = code;
        protected override int Insert(PrimeDbContext db, TEntity entity) => Repository.Insert(entity, db);
        protected override void Save(PrimeDbContext db, TEntity entity) => Repository.Update(entity, db);
        protected override void Erase(PrimeDbContext db, TEntity entity) => Repository.Delete(entity.Id, CurrentUser, db);

        protected override object AuditValue(TEntity entity) => new { entity.Code, entity.Name };
        protected override string DeleteDetails(TEntity entity) => entity.Code;

        protected override TEntity ToDto(TEntity entity) => entity;

        public Result<TEntity> GetByCode(string code)
        {
            if (!Can("View")) return FailDenied<TEntity>();

            var entity = Repository.GetByCode(code);
            return entity == null ? Fail<TEntity>("NotFound", ErrorCode.NotFound) : Ok(entity);
        }

        public override string[] RootKeys => new[] { AccountSettingKey };

        protected override TEntity FromAccount(string accountCode, string name, string rootCode) =>
            new() { Name = name, AccountCode = accountCode, IsActive = true };

        protected override TEntity FindByAccount(PrimeDbContext db, string accountCode) => Repository.GetByAccountCode(accountCode, db);

        protected override void RenameByAccount(PrimeDbContext db, string accountCode, string name) =>
            Repository.UpdateNameByAccountCode(db, accountCode, name);

        public virtual Result RecalculateBalance(int id)
        {
            if (!Can("Edit")) return FailDenied();

            var entity = FindById(id);
            if (entity == null) return Fail("NotFound", ErrorCode.NotFound);
            var account = AccountOf.Required(entity.AccountCode, "Str.Party.AccountNotConfigured");
            if (account.IsFailure) return account;

            Tx(db => _balances.Refresh(db, account.Value));
            return Result.Ok();
        }

        public virtual Result RecalculateAllBalances()
        {
            if (!Can("Edit")) return FailDenied();

            var all = Tree.Rows.GetAll(includeInactive: true);
            Tx(db => _balances.RefreshAll(db, all));

            return Result.Ok();
        }

        public virtual Result<List<AccountStatementLine>> GetStatement(int id, DateTime from, DateTime to)
        {
            if (!Can("View")) return FailDenied<List<AccountStatementLine>>();

            var entity = FindById(id);
            if (entity == null) return Fail<List<AccountStatementLine>>("NotFound", ErrorCode.NotFound);
            var account = AccountOf.Required(entity.AccountCode, "Str.Party.AccountNotConfigured");
            if (account.IsFailure) return account.As<List<AccountStatementLine>>();

            var lines = _statement.Of(account.Value, from, to);
            var open = Cheques?.GetOpenForParty(id, from, to);
            return Result.Ok(open is { IsSuccess: true } ? Statement.WithCheques(lines, open.Value) : lines);
        }

        public virtual Result<CreditCheckResult> CheckCreditLimit(int id, decimal additional)
        {
            if (!Can("View")) return FailDenied<CreditCheckResult>();

            var entity = FindById(id);
            if (entity == null) return Fail<CreditCheckResult>("NotFound", ErrorCode.NotFound);

            var exceededBy = PartyCalc.Exceeding(entity.Balance, additional, entity.CreditLimit);
            return exceededBy > 0
                ? Result.Fail<CreditCheckResult>(Localization.Get("Str.Party.CreditLimitExceeded", exceededBy), ErrorCode.ValidationFailed)
                : Result.Ok(new CreditCheckResult
                {
                    IsAllowed = true, CurrentBalance = entity.Balance, CreditLimit = entity.CreditLimit > 0 ? entity.CreditLimit : 0,
                    AvailableCredit = PartyCalc.Available(entity.Balance, entity.CreditLimit), ExceededBy = 0m
                });
        }
    }
}
