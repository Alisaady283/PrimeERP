using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using PrimeERP.Platform.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Domain.Entities;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Platform.Audit;
using AuditAction = PrimeERP.Domain.Enums.AuditAction;

namespace PrimeERP.Application.Legacy.Accounting
{
    /// <summary>صفحة شجرة الحسابات</summary>
    public class AccountService : ServiceBase, IAccountService
    {
        protected override string PermissionPrefix => "Accounts";
        protected override string StringPrefix => "Str.Accounts";
        protected override string EntityName => "Accounts";

        private readonly IAccountRepository _accounts;
        private readonly IJournalRepository _journal;
        private readonly AccountBalances _balances;
        private readonly Statement _statement;
        private readonly Guards _guards;
        private readonly LinkedAccounts _linked;
        private readonly AddTreeAccount _addTree;
        private readonly AddLinkedAccount _addLinked;
        private readonly EditLinkedAccount _editLinked;
        private readonly CloseLinkedAccount _closeLinked;

        public AccountService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IAccountRepository accounts, IJournalRepository journal, AccountBalances balances, Statement statement, Guards guards,
            LinkedAccounts linked, AddTreeAccount addTree, AddLinkedAccount addLinked, EditLinkedAccount editLinked,
            CloseLinkedAccount closeLinked)
            : base(permissions, settings, localization, audit)
        {
            _accounts = accounts;
            _journal = journal;
            _balances = balances;
            _statement = statement;
            _guards = guards;
            _linked = linked;
            _addTree = addTree;
            _addLinked = addLinked;
            _editLinked = editLinked;
            _closeLinked = closeLinked;
        }


        public Result<List<AccountTreeNode>> GetTree(AccountTreeFilter filter = null)
        {
            if (!Can("View")) return FailDenied<List<AccountTreeNode>>();

            filter ??= new AccountTreeFilter();
            var all = _accounts.GetAll(filter.IncludeInactive);
            var keep = Tree.WithAncestors(all, Find(filter), a => a.Code, a => a.ParentCode);
            var kept = all.Where(a => keep.Contains(a.Code)).ToList();
            var facts = FactsOf(kept, filter.IncludeInactive);

            return Result.Ok(Tree.Build<Account, string, AccountTreeNode>(kept, a => a.Code, a => a.ParentCode,
                (a, children) => Rows.Copy(ToDto(a, facts), new AccountTreeNode { Children = children })));
        }

        public Result<PagedResult<AccountDto>> GetPaged(int page, int pageSize, AccountTreeFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<AccountDto>>();

            filter ??= new AccountTreeFilter();
            var (items, total) = _accounts.GetPaged(page, pageSize, filter.SearchText, filter.LevelFilter,
                (int?)filter.TypeFilter, filter.LeafOnly, filter.IncludeInactive);
            var facts = FactsOf(items, filter.IncludeInactive);

            return Result.Ok(Paged(items, total, page, pageSize, rows => rows.Select(a => ToDto(a, facts)).ToList()));
        }

        public Result<List<AccountDto>> GetLeaves(AccountType? type = null)
        {
            if (!Can("View")) return FailDenied<List<AccountDto>>();

            var leaves = _accounts.Find(null, null, (int?)type, leafOnly: true, includeInactive: false);
            var facts = FactsOf(leaves, includeInactive: false);
            return Result.Ok(leaves.Select(a => ToDto(a, facts)).ToList());
        }

        public Result<AccountDto> GetById(int id) => Can("View") ? One(_accounts.GetById(id)) : FailDenied<AccountDto>();

        public Result<AccountDto> GetByCode(string code) => Can("View") ? One(_accounts.GetByCode(code)) : FailDenied<AccountDto>();

        public Result<string> GenerateChildCode(int parentId)
        {
            if (!Can("Create")) return FailDenied<string>();

            var parent = _accounts.GetById(parentId);
            if (parent == null)
                return Result.Fail<string>(Msg("ParentNotFound"), ErrorCode.NotFound);

            return _addTree.ChildCode(null, parent);
        }


        public Result<AccountDto> Create(CreateAccountDto dto)
        {
            if (!Can("Create")) return FailDenied<AccountDto>();

            var parent = _accounts.GetById(dto.ParentId);
            if (parent == null) return Result.Fail<AccountDto>(Msg("ParentNotFound"), ErrorCode.NotFound);
            if (!dto.SkipAutoLink && _guards.IsManaged(parent.Code)) return FailManaged().As<AccountDto>();

            var link = _linked.Of(parent.Code, dto.SkipAutoLink);
            var created = Commit(db => _addLinked.Run(db, parent, dto.Name, link.Linked, link.Root, dto.IsLeaf, dto.Notes, dto.IsActive));
            if (created.IsFailure) return created.As<AccountDto>();

            Audit.Log(EntityName, created.Value.Id, AuditAction.Insert, newValue: new { created.Value.Code, created.Value.Name });
            return One(_accounts.GetById(created.Value.Id));
        }

        public Result Update(UpdateAccountDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var account = _accounts.GetById(dto.Id);
            if (account == null) return NotFound();
            if (_guards.IsManaged(account.Code)) return FailManaged();
            if (_guards.IsSystem(account.Code) && !Permissions.Can(PermissionKeys.Settings.System))
                return Result.Fail(Localization.Get("Str.Settings.SystemPermissionDenied"), ErrorCode.Unauthorized);

            var link = _linked.Of(account.ParentCode).Linked;
            var edited = Commit(db => _editLinked.Run(db, account, dto.Name, dto.Notes, dto.IsActive, link));
            if (edited.IsFailure) return edited;

            Audit.Log(EntityName, account.Id, AuditAction.Update, newValue: new { account.Name, account.IsLeaf, account.IsActive });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var account = _accounts.GetById(id);
            if (account == null) return NotFound();
            if (_guards.IsManaged(account.Code)) return FailManaged();
            if (_guards.IsSystem(account.Code)) return Result.Fail(Msg("SystemCannotDelete"), ErrorCode.ValidationFailed);
            if (_guards.HasChildren(account.Code)) return Result.Fail(Msg("HasChildren"), ErrorCode.ValidationFailed);
            if (_guards.HasEntries(account.Code)) return Result.Fail(Msg("HasEntries"), ErrorCode.ValidationFailed);

            var link = _linked.Of(account.ParentCode).Linked;
            var closed = Commit(db => _closeLinked.Run(db, account.Code, link));
            if (closed.IsFailure) return closed;

            Audit.Log(EntityName, account.Id, AuditAction.Delete, details: account.Code);
            return Result.Ok();
        }

        public Result RecalculateBalance(string code)
        {
            if (!Can("Edit")) return FailDenied();

            var account = _accounts.GetByCode(code);
            if (account == null) return NotFound();

            var balance = Tx(db => _balances.Refresh(db, code));
            Audit.Log(EntityName, account.Id, AuditAction.Update, details: Msg("Recalculated", code, balance));
            return Result.Ok();
        }

        public Result RecalculateBalance(PrimeDbContext db, string code)
        {
            _balances.Refresh(db, code);
            return Result.Ok();
        }

        public Result RecalculateAllBalances()
        {
            if (!Can("Edit")) return FailDenied();

            var all = _accounts.GetAll(includeInactive: true);
            Tx(db => _balances.RefreshAll(db, all));

            Audit.Log(EntityName, 0, AuditAction.Update, details: Msg("RecalculatedAll", all.Count));
            return Result.Ok();
        }

        public Result<decimal> GetBalanceAsOf(string code, DateTime date)
        {
            if (!Can("View")) return FailDenied<decimal>();
            if (_accounts.GetByCode(code) == null) return NotFound().As<decimal>();

            return Result.Ok(_journal.SumPosted(code, null, date));
        }

        public Result<List<AccountStatementLine>> GetStatement(string code, DateTime from, DateTime to)
        {
            if (!Can("View")) return FailDenied<List<AccountStatementLine>>();
            if (_accounts.GetByCode(code) == null) return NotFound().As<List<AccountStatementLine>>();

            return Result.Ok(_statement.Of(code, from, to));
        }

        public bool IsLinkedRoot(string accountCode) => _linked.IsRoot(accountCode);

        public Result<bool> CanAcceptEntries(string code) =>
            _accounts.GetByCode(code) is { } account ? Result.Ok(Guards.EntryRefusal(account) == null) : NotFound().As<bool>();

        public Result<bool> CanHaveChildren(string code) =>
            _accounts.GetByCode(code) != null ? Result.Ok(!_guards.HasEntries(code)) : NotFound().As<bool>();

        public Result<AccountType> GetTypeOf(string code) =>
            _accounts.GetByCode(code) is { } account ? Result.Ok((AccountType)account.Type) : NotFound().As<AccountType>();


        private List<Account> Find(AccountTreeFilter filter) =>
            _accounts.Find(filter.SearchText, filter.LevelFilter, (int?)filter.TypeFilter, filter.LeafOnly, filter.IncludeInactive);

        private Result<AccountDto> One(Account account) => account == null
            ? NotFound().As<AccountDto>()
            : Result.Ok(ToDto(account, FactsOf(new List<Account> { account }, includeInactive: true)));

        private Result NotFound() => Result.Fail(Localization.Get("Str.Journal.AccountNotFound"), ErrorCode.NotFound);

        private static Result FailManaged() =>
            Result.Fail(LocalizationService.Get("Str.Accounts.AssetManaged"), ErrorCode.ValidationFailed);

        /// <summary>ما يحتاجه عرض الحسابات</summary>
        private sealed record AccountFacts(IReadOnlyDictionary<string, Account> ByCode, HashSet<string> WithChildren,
                                           HashSet<string> WithLines);

        private AccountFacts FactsOf(List<Account> shown, bool includeInactive)
        {
            var codes = shown.Select(a => a.Code).ToList();
            var parents = _accounts.GetByCodes(shown.Select(a => a.ParentCode).Where(c => !string.IsNullOrEmpty(c)));
            return new(parents.ToDictionary(a => a.Code), _accounts.CodesWithChildren(codes, includeInactive),
                       _journal.AccountsWithLines(codes));
        }

        private AccountDto ToDto(Account a, AccountFacts facts)
        {
            var parent = a.ParentCode != null && facts.ByCode.TryGetValue(a.ParentCode, out var p) ? p : null;
            var (variant, status) = Rows.Active(a.IsActive);

            var dto = Rows.Copy(a, new AccountDto());
            dto.ParentId        = parent?.Id;
            dto.ParentName      = parent?.Name;
            dto.TypeName        = LocalizationService.Get("Str.AccountType." + dto.Type);
            dto.HasChildren     = facts.WithChildren.Contains(a.Code);
            dto.HasTransactions = facts.WithLines.Contains(a.Code);
            dto.IsSystem        = _guards.IsSystem(a.Code);
            dto.StatusVariant   = variant;
            dto.StatusText      = status;
            return dto;
        }
    }
}
