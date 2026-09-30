using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Validation;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Parties
{
    /// <summary>المنطق المشترك بين العملاء والموردين</summary>
    public abstract class PartyServiceBase<TEntity, TDto, TCreate, TUpdate, TFilter>
        : EntityService<TEntity, TDto, TCreate, TUpdate, TFilter>, IAccountLinkedService
        where TEntity : PartyBase, new()
        where TDto : PartyDto, new()
        where TCreate : IPartyInput
        where TUpdate : IPartyInput
    {
        protected abstract string AccountSettingKey { get; }
        protected abstract IPartyRepository<TEntity> Repository { get; }
        protected virtual PrimeERP.Application.Legacy.Cheques.IChequeService Cheques => null;

        private readonly Statement _statement;
        private readonly IAccountRepository _accountRepo;
        private readonly IJournalRepository _journalRepo;
        private readonly AccountSpec<TEntity>[] _account;

        protected PartyServiceBase(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
                                    IAuditLogger audit, Statement statement, INumberSequenceService numbers, IAccountRepository accountRepo,
                                    IJournalRepository journalRepo, AccountCases tree)
            : base(permissions, settings, localization, audit, numbers, tree)
        {
            _statement = statement;
            _accountRepo = accountRepo;
            _journalRepo = journalRepo;
            _account = new[] { new AccountSpec<TEntity>((db, _) => ParentAccount(db), e => e.Name, e => e.AccountCode, (e, code) => e.AccountCode = code) };
        }

        protected override IReadOnlyList<AccountSpec<TEntity>> Accounts => _account;

        private Result<Account> ParentAccount(PrimeDbContext db) =>
            Tree.Root(db, Setting(AccountSettingKey, ""), "Str.Party.AccountParentNotFound", "Str.Party.AccountParentIsLeaf");

        protected override TEntity FindById(int id) => Repository.GetById(id);
        protected override List<TEntity> FindSearch(string term, int maxResults) => Repository.Search(term, maxResults);

        protected override TEntity New(TCreate dto) => Rows.Copy(dto, new TEntity());

        protected override void Apply(TEntity entity, TUpdate dto) => Rows.Copy(dto, entity);

        protected override void Number(TEntity entity, string code) => entity.Code = code;
        protected override int Insert(PrimeDbContext db, TEntity entity) => Repository.Insert(entity, db);
        protected override void Save(PrimeDbContext db, TEntity entity) => Repository.Update(entity, db);
        protected override void Erase(PrimeDbContext db, TEntity entity) => Repository.Delete(entity.Id, CurrentUser, db);

        protected override object AuditValue(TEntity entity) => new { entity.Code, entity.Name };
        protected override string DeleteDetails(TEntity entity) => entity.Code;

        public Result<TDto> GetByCode(string code)
        {
            if (!Can("View")) return FailDenied<TDto>();

            var entity = Repository.GetByCode(code);
            return entity == null ? Fail<TDto>("NotFound", ErrorCode.NotFound) : Ok(ToDto(entity));
        }

        string[] IAccountLinkedService.RootKeys => new[] { AccountSettingKey };

        Result IAccountLinkedService.CreateFromAccount(PrimeDbContext db, string accountCode, string name, string rootCode) =>
            CreateFromAccount(db, accountCode, name);

        public virtual Result<TDto> CreateFromAccount(PrimeDbContext db, string accountCode, string name)
        {
            var entity = new TEntity { Code = Numbers.Next(db, SequenceKey), Name = name, AccountCode = accountCode, IsActive = true };

            var check = Check.Valid(entity, Fields);
            if (check.IsFailure) return check.As<TDto>();

            entity.Id = Repository.Insert(entity, db);
            return Result.Ok(ToDto(entity));
        }

        public virtual Result DeleteByAccountCode(PrimeDbContext db, string accountCode)
        {
            var entity = Repository.GetByAccountCode(accountCode, db);
            if (entity != null) Repository.Delete(entity.Id, CurrentUser, db);
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
            if (string.IsNullOrWhiteSpace(entity.AccountCode)) return Result.Fail(Localization.Get("Str.Party.AccountNotConfigured"));

            Tx(db => PartyBalance.Refresh(Repository, _journalRepo, db, id));
            return Result.Ok();
        }

        public virtual Result RecalculateAllBalances()
        {
            if (!Can("Edit")) return FailDenied();

            Tx(db => PartyBalance.RefreshAll(Repository, _journalRepo, db));

            return Result.Ok();
        }

        public virtual Result<List<AccountStatementLine>> GetStatement(int id, DateTime from, DateTime to)
        {
            if (!Can("View")) return FailDenied<List<AccountStatementLine>>();

            var entity = FindById(id);
            if (entity == null) return Fail<List<AccountStatementLine>>("NotFound", ErrorCode.NotFound);
            if (string.IsNullOrWhiteSpace(entity.AccountCode))
                return Result.Fail<List<AccountStatementLine>>(Localization.Get("Str.Party.AccountNotConfigured"));

            var lines = _statement.Of(entity.AccountCode, from, to);
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

        /// <summary>حقائق حسابات الأطراف</summary>
        protected sealed record AccountFacts(HashSet<string> WithLines, IReadOnlyDictionary<string, string> Names)
        {
            public bool HasLines(string code) => !string.IsNullOrWhiteSpace(code) && WithLines.Contains(code);
            public string NameOf(string code) => string.IsNullOrWhiteSpace(code) ? null : Names.GetValueOrDefault(code);
        }

        private AccountFacts FactsOf(IEnumerable<TEntity> entities)
        {
            var codes = entities.Select(e => e.AccountCode).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
            return new AccountFacts(_journalRepo.AccountsWithLines(codes),
                                    _accountRepo.GetByCodes(codes).ToDictionary(a => a.Code, a => a.Name));
        }

        protected override TDto ToDto(TEntity entity) => ToDto(entity, FactsOf(new[] { entity }));

        protected override List<TDto> ToDtos(List<TEntity> entities)
        {
            var facts = FactsOf(entities);
            return entities.Select(e => ToDto(e, facts)).ToList();
        }

        protected virtual TDto ToDto(TEntity p, AccountFacts facts)
        {
            var isOverLimit = PartyCalc.IsOverCreditLimit(p.Balance, p.CreditLimit);
            var hasTransactions = facts.HasLines(p.AccountCode);
            var (variant, status) = Rows.State(
                (!p.IsActive, StatusVariant.Neutral, "Str.Party.Status.Inactive"),
                (isOverLimit, StatusVariant.Danger, "Str.Party.Status.OverLimit"),
                (true, StatusVariant.Success, "Str.Party.Status.Active"));

            return Rows.Copy(p, new TDto(), row =>
            {
                row.AccountName = facts.NameOf(p.AccountCode);
                row.IsOverCreditLimit = isOverLimit;
                row.AvailableCredit = p.CreditLimit - p.Balance;
                row.StatusVariant = variant;
                row.StatusText = status;
                row.CanEdit = Can("Edit");
                row.CanDelete = Can("Delete") && !hasTransactions;
                row.HasTransactions = hasTransactions;
            });
        }
    }
}
