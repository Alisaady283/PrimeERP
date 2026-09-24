using PrimeERP.Data.Core;
using PrimeERP.Platform.Localization;
using PrimeERP.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Domain.Entities;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Platform.Audit;
using AuditAction = PrimeERP.Domain.Enums.AuditAction;

namespace PrimeERP.Application.Services.Accounting
{
    /// <summary>المالك الوحيد لمنطق شجرة الحسابات</summary>
    public class AccountService : ServiceBase, IAccountService
    {
        protected override string PermissionPrefix => "Accounts";
        protected override string StringPrefix => "Str.Accounts";
        protected override string EntityName => "Accounts";

        private readonly IAccountRepository _accounts;
        private readonly IJournalRepository _journal;

        private readonly IServiceProvider _services;

        public AccountService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IAccountRepository accounts, IJournalRepository journal, IServiceProvider services)
            : base(permissions, settings, localization, audit)
        {
            _accounts = accounts;
            _journal = journal;
            _services = services;
        }


        public Result<List<AccountTreeNode>> GetTree(AccountTreeFilter filter = null)
        {
            if (!Can("View")) return FailDenied<List<AccountTreeNode>>();

            filter ??= new AccountTreeFilter();
            var all = _accounts.GetAll(filter.IncludeInactive);
            var matched = ApplyFlatFilter(all, filter);

            var keepCodes = new HashSet<string>(matched.Select(a => a.Code));
            foreach (var code in matched.Select(a => a.Code).ToList())
            {
                var current = all.FirstOrDefault(a => a.Code == code);
                while (current != null && !string.IsNullOrEmpty(current.ParentCode))
                {
                    keepCodes.Add(current.ParentCode);
                    current = all.FirstOrDefault(a => a.Code == current.ParentCode);
                }
            }

            return Result.Ok(BuildTree(all, keepCodes));
        }

        public Result<PagedResult<AccountDto>> GetPaged(int page, int pageSize, AccountTreeFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<AccountDto>>();

            filter ??= new AccountTreeFilter();
            var all = _accounts.GetAll(filter.IncludeInactive);
            var filtered = ApplyFlatFilter(all, filter);

            var items = filtered
                .Skip(Math.Max(0, page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => ToDto(a, all))
                .ToList();

            return Result.Ok(new PagedResult<AccountDto>
            {
                Items = items,
                TotalCount = filtered.Count,
                Page = page,
                PageSize = pageSize
            });
        }

        public Result<List<AccountDto>> GetLeaves(AccountType? type = null)
        {
            if (!Can("View")) return FailDenied<List<AccountDto>>();

            var all = _accounts.GetAll();
            var leaves = all.Where(a => a.IsLeaf && (type == null || a.Type == (int)type.Value))
                            .OrderBy(a => a.Code).ToList();

            return Result.Ok(leaves.Select(a => ToDto(a, all)).ToList());
        }

        public Result<AccountDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<AccountDto>();

            var account = _accounts.GetById(id);
            if (account == null)
                return Result.Fail<AccountDto>("الحساب غير موجود", ErrorCode.NotFound);

            return Result.Ok(ToDto(account, _accounts.GetAll(true)));
        }

        public Result<AccountDto> GetByCode(string code)
        {
            if (!Can("View")) return FailDenied<AccountDto>();

            var account = _accounts.GetByCode(code);
            if (account == null)
                return Result.Fail<AccountDto>("الحساب غير موجود", ErrorCode.NotFound);

            return Result.Ok(ToDto(account, _accounts.GetAll(true)));
        }

        public Result<string> GenerateChildCode(int parentId)
        {
            if (!Can("Create")) return FailDenied<string>();

            var parent = _accounts.GetById(parentId);
            if (parent == null)
                return Result.Fail<string>("الحساب الأب غير موجود", ErrorCode.NotFound);

            return GenerateChildCodeInternal(parent.Code);
        }


        public Result<AccountDto> Create(CreateAccountDto dto)
        {
            if (!Can("Create")) return FailDenied<AccountDto>();

            var parent = _accounts.GetById(dto.ParentId);
            if (parent == null)
                return Result.Fail<AccountDto>("الحساب الأب غير موجود", ErrorCode.NotFound);

            if (!dto.SkipAutoLink && IsAssetManaged(parent.Code))
                return Result.Fail<AccountDto>(LocalizationService.Get("Str.Accounts.AssetManaged"), ErrorCode.ValidationFailed);

            if (parent.IsLeaf && _journal.HasLinesForAccount(parent.Code))
                return Result.Fail<AccountDto>($"الحساب «{parent.Name}» مسجَّل به قيود فلا يقبل حسابات فرعية — احذف قيوده أولاً", ErrorCode.ValidationFailed);

            var codeResult = GenerateChildCodeInternal(parent.Code);
            if (!codeResult.IsSuccess)
                return Result.Fail<AccountDto>(codeResult.ErrorMessage, codeResult.ErrorCode);
            var account = BuildNewAccount(dto, parent, codeResult.Value);

            var check = Check(new AccountValidator(_accounts, isEdit: false), account);
            if (check.IsFailure) return check.As<AccountDto>();

            var link = ResolveAutoLink(dto.SkipAutoLink, parent.Code);
            if (!link.IsSuccess)
                return Result.Fail<AccountDto>(link.ErrorMessage, link.ErrorCode);

            var newId = Tx(db =>
            {
                var id = _accounts.Insert(account, db);
                account.Id = id;

                if (parent.IsLeaf) _accounts.SetIsLeaf(parent.Code, false, db);

                link.Value.Linked?.CreateFromAccount(db, account.Code, account.Name, link.Value.RootCode);

                return id;
            });

            Audit.Log(EntityName, newId, AuditAction.Insert, newValue: new { account.Code, account.Name });

            var created = _accounts.GetById(newId);
            return Result.Ok(ToDto(created, _accounts.GetAll(true)));
        }

        public Result<AccountDto> Create(PrimeDbContext db, CreateAccountDto dto)
        {
            var parent = _accounts.GetById(dto.ParentId, db);
            if (parent == null)
                return Result.Fail<AccountDto>("الحساب الأب غير موجود", ErrorCode.NotFound);

            if (parent.IsLeaf)
                return Result.Fail<AccountDto>("لا يمكن إضافة حساب فرعي تحت حساب يقبل قيوداً مباشرة (Leaf) — حوّله لأب أولاً عبر التعديل", ErrorCode.ValidationFailed);

            var codeResult = GenerateChildCodeInternal(db, parent.Code);
            if (!codeResult.IsSuccess)
                return Result.Fail<AccountDto>(codeResult.ErrorMessage, codeResult.ErrorCode);
            var account = BuildNewAccount(dto, parent, codeResult.Value);

            var check = Check(new AccountValidator(_accounts, isEdit: false, checkUniqueness: false), account);
            if (check.IsFailure) return check.As<AccountDto>();

            var id = _accounts.Insert(account, db);
            account.Id = id;


            return Result.Ok(BuildFreshAccountDto(account, parent));
        }

        public Result Update(UpdateAccountDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var account = _accounts.GetById(dto.Id);
            if (account == null)
                return Result.Fail("الحساب غير موجود", ErrorCode.NotFound);

            if (IsAssetManaged(account.Code)) return FailAssetManaged();

            if (IsSystemAccount(account.Code) && !Permissions.Can(PermissionKeys.Settings.System))
                return Result.Fail(Localization.Get("Str.Settings.SystemPermissionDenied"), ErrorCode.Unauthorized);

            var nameChanged = account.Name != dto.Name; // قبل الاستبدال أدناه

            account.Name     = dto.Name;
            account.Notes    = dto.Notes;
            account.IsActive = dto.IsActive;

            account.IsLeaf   = !_accounts.HasChildren(account.Code);

            var validation = new AccountValidator(_accounts, isEdit: true).Validate(account);
            if (!validation.IsValid)
                return Result.Fail(validation.Errors.Values.ToList(), ErrorCode.ValidationFailed);

            Result<AutoLinkResolution> link = null;
            if (nameChanged)
            {
                link = ResolveAutoLink(skipAutoLink: false, account.ParentCode);
                if (!link.IsSuccess)
                    return Result.Fail(link.ErrorMessage, link.ErrorCode);
            }

            Tx(db =>
            {
                _accounts.Update(account, db);

                if (nameChanged) link.Value.Linked?.UpdateNameFromAccount(db, account.Code, account.Name);
            });

            Audit.Log(EntityName, account.Id, AuditAction.Update, newValue: new { account.Name, account.IsLeaf, account.IsActive });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var account = _accounts.GetById(id);
            if (account == null)
                return Result.Fail("الحساب غير موجود", ErrorCode.NotFound);

            if (IsAssetManaged(account.Code)) return FailAssetManaged();

            if (IsSystemAccount(account.Code))
                return Result.Fail("لا يمكن حذف حساب نظامي", ErrorCode.ValidationFailed);

            if (_accounts.HasChildren(account.Code))
                return Result.Fail("لا يمكن حذف حساب له حسابات أبناء", ErrorCode.ValidationFailed);

            if (_journal.HasLinesForAccount(account.Code))
                return Result.Fail("لا يمكن حذف حساب له قيود مسجَّلة", ErrorCode.ValidationFailed);

            var link = ResolveAutoLink(skipAutoLink: false, account.ParentCode);
            if (!link.IsSuccess)
                return Result.Fail(link.ErrorMessage, link.ErrorCode);

            Tx(db =>
            {
                _accounts.Delete(account.Code, db);

                link.Value.Linked?.DeleteByAccountCode(db, account.Code);

                if (!string.IsNullOrWhiteSpace(account.ParentCode) && !_accounts.HasChildren(account.ParentCode, db))
                    _accounts.SetIsLeaf(account.ParentCode, true, db);
            });

            Audit.Log(EntityName, account.Id, AuditAction.Delete, details: account.Code);
            return Result.Ok();
        }

        public Result Delete(PrimeDbContext db, string accountCode)
        {
            _accounts.Delete(accountCode, db);
            return Result.Ok();
        }

        public Result UpdateName(PrimeDbContext db, string accountCode, string name)
        {
            _accounts.UpdateName(db, accountCode, name);
            return Result.Ok();
        }


        public Result RecalculateBalance(string code)
        {
            if (!Can("Edit")) return FailDenied();

            var account = _accounts.GetByCode(code);
            if (account == null)
                return Result.Fail("الحساب غير موجود", ErrorCode.NotFound);

            var balance = ComputeBalance(code);
            Tx(db => _accounts.UpdateBalance(code, balance, db));

            Audit.Log(EntityName, account.Id, AuditAction.Update, details: $"إعادة حساب رصيد {code}: {balance:N2}");
            return Result.Ok();
        }

        public Result RecalculateBalance(PrimeDbContext db, string code)
        {
            _accounts.UpdateBalance(code, ComputeBalance(db, code), db);
            return Result.Ok();
        }

        private decimal ComputeBalance(string code) =>
            _journal.GetPostedLinesForAccount(code, null, null).Sum(l => l.Debit - l.Credit);

        private decimal ComputeBalance(PrimeDbContext db, string code) =>
            _journal.GetPostedLinesForAccount(code, null, null, db).Sum(l => l.Debit - l.Credit);

        public Result RecalculateAllBalances()
        {
            if (!Can("Edit")) return FailDenied();

            var all = _accounts.GetAll(includeInactive: true);

            Tx(db =>
            {
                var balances = new Dictionary<string, decimal>();

                foreach (var account in all.Where(a => a.IsLeaf))
                    balances[account.Code] = ComputeBalance(db, account.Code);

                foreach (var account in all.Where(a => !a.IsLeaf).OrderByDescending(a => a.Level))
                    balances[account.Code] = all
                        .Where(child => child.ParentCode == account.Code)
                        .Sum(child => balances.TryGetValue(child.Code, out var balance) ? balance : 0m);

                foreach (var pair in balances)
                    _accounts.UpdateBalance(pair.Key, pair.Value, db);
            });

            Audit.Log(EntityName, 0, AuditAction.Update, details: $"إعادة حساب كل الأرصدة ({all.Count} حساب)");
            return Result.Ok();
        }

        public Result<decimal> GetBalanceAsOf(string code, DateTime date)
        {
            if (!Can("View")) return FailDenied<decimal>();

            var account = _accounts.GetByCode(code);
            if (account == null)
                return Result.Fail<decimal>("الحساب غير موجود", ErrorCode.NotFound);

            var lines = _journal.GetPostedLinesForAccount(code, null, date);
            return Result.Ok(lines.Sum(l => l.Debit - l.Credit));
        }

        public Result<List<AccountStatementLine>> GetStatement(string code, DateTime from, DateTime to)
        {
            if (!Can("View")) return FailDenied<List<AccountStatementLine>>();

            var account = _accounts.GetByCode(code);
            if (account == null)
                return Result.Fail<List<AccountStatementLine>>("الحساب غير موجود", ErrorCode.NotFound);

            var openingLines = _journal.GetPostedLinesForAccount(code, null, from.AddDays(-1));
            var running = openingLines.Sum(l => l.Debit - l.Credit);

            var result = new List<AccountStatementLine>
            {
                new()
                {
                    Date = from.ToString("yyyy-MM-dd"),
                    EntryNo = "",
                    Description = "رصيد افتتاحي",
                    Debit = 0,
                    Credit = 0,
                    RunningBalance = running,
                    SourceType = "Opening"
                }
            };

            foreach (var line in _journal.GetPostedLinesForAccount(code, from, to))
            {
                running += line.Debit - line.Credit;
                result.Add(new AccountStatementLine
                {
                    Date           = line.EntryDate,
                    EntryNo        = line.EntryNo,
                    Description    = line.Description,
                    Debit          = line.Debit,
                    Credit         = line.Credit,
                    RunningBalance = running,
                    SourceType     = "Journal"
                });
            }

            return Result.Ok(result);
        }


        public Result<bool> CanAcceptEntries(string code)
        {
            var account = _accounts.GetByCode(code);
            if (account == null)
                return Result.Fail<bool>("الحساب غير موجود", ErrorCode.NotFound);

            return Result.Ok(account.IsLeaf && account.IsActive);
        }

        public Result<bool> CanHaveChildren(string code)
        {
            var account = _accounts.GetByCode(code);
            if (account == null)
                return Result.Fail<bool>("الحساب غير موجود", ErrorCode.NotFound);

            return Result.Ok(!_journal.HasLinesForAccount(code));
        }

        public Result<AccountType> GetTypeOf(string code)
        {
            var account = _accounts.GetByCode(code);
            if (account == null)
                return Result.Fail<AccountType>("الحساب غير موجود", ErrorCode.NotFound);

            return Result.Ok((AccountType)account.Type);
        }


        private static Account BuildNewAccount(CreateAccountDto dto, Account parent, string code) => new()
        {
            Code       = code,
            Name       = dto.Name,
            ParentCode = parent.Code,
            Level      = parent.Level + 1,
            IsLeaf     = dto.IsLeaf,
            Type       = parent.Type,
            Notes      = dto.Notes,
            IsActive   = dto.IsActive
        };

        /// <summary>وضع الربط التلقائي</summary>
        private class AutoLinkResolution
        {
            public bool AutoLinkEnabled;

            public IAccountLinkedService Linked;

            public string RootCode;
        }

        public bool IsLinkedRoot(string accountCode) =>
            !string.IsNullOrWhiteSpace(accountCode) &&
            SettingKeys.Accounts.LinkedRoots.Any(key => Setting(key, "") == accountCode);

        private static readonly (string SettingKey, Type ServiceType)[] LinkedRoots =
        {
            (SettingKeys.Accounts.Customers, typeof(ICustomerService)),
            (SettingKeys.Accounts.Suppliers, typeof(ISupplierService)),
            (SettingKeys.Accounts.Cash,      typeof(PrimeERP.Application.Services.Treasury.ITreasuryService)),
            (SettingKeys.Accounts.Bank,      typeof(PrimeERP.Application.Services.Treasury.ITreasuryService)),
            (SettingKeys.Accounts.EmployeeAdvances, typeof(PrimeERP.Application.Services.HR.IEmployeeService)),
        };

        private Result<AutoLinkResolution> ResolveAutoLink(bool skipAutoLink, string parentOrAccountCode)
        {
            if (skipAutoLink)
                return Result.Ok(new AutoLinkResolution());

            var autoLinkEnabled = Setting(SettingKeys.Accounts.AutoLinkEnabled, true);
            var resolution = new AutoLinkResolution { AutoLinkEnabled = autoLinkEnabled };
            if (!autoLinkEnabled) return Result.Ok(resolution);

            foreach (var (settingKey, serviceType) in LinkedRoots)
            {
                var root = Setting(settingKey, "");
                if (string.IsNullOrEmpty(root) || parentOrAccountCode != root) continue;

                resolution.Linked = _services.GetService(serviceType) as IAccountLinkedService;
                if (resolution.Linked == null)
                    return Fail<AutoLinkResolution>("AccountLinkUnavailable");

                resolution.RootCode = root;

                break;
            }

            return Result.Ok(resolution);
        }

        private const int MaxChildSuffix = 9999;

        private Result<string> GenerateChildCodeInternal(string parentCode) =>
            BuildChildCode(_accounts.GetByCode(parentCode), _accounts.GetAllChildren(parentCode));

        private Result<string> GenerateChildCodeInternal(PrimeDbContext db, string parentCode) =>
            BuildChildCode(_accounts.GetByCode(parentCode, db), _accounts.GetAllChildren(parentCode, db));

        private static Result<string> BuildChildCode(Account parent, List<Account> children)
        {
            var parentCode = parent.Code;
            var width = parent.Level;

            int next = 1;
            if (children.Count > 0)
            {
                int max = 0;
                foreach (var acc in children)
                {
                    var suffix = acc.Code.Length > parentCode.Length ? acc.Code.Substring(parentCode.Length) : "";
                    if (int.TryParse(suffix, out int n) && n > max)
                        max = n;
                }
                next = max + 1;
            }

            if (next > MaxChildSuffix)
                return Result.Fail<string>($"تجاوز '{parentCode}' الحد الأقصى لعدد الأبناء ({MaxChildSuffix})", ErrorCode.ValidationFailed);

            return Result.Ok(parentCode + next.ToString("D" + width));
        }

        private static AccountDto BuildFreshAccountDto(Account a, Account parent) => new()
        {
            Id               = a.Id,
            Code             = a.Code,
            Name             = a.Name,
            ParentId         = parent.Id,
            ParentCode       = parent.Code,
            ParentName       = parent.Name,
            Level            = a.Level,
            IsLeaf           = a.IsLeaf,
            IsActive         = a.IsActive,
            Type             = (AccountType)a.Type,
            TypeName         = TypeName((AccountType)a.Type),
            Balance          = 0m,
            LinkedEntityType = LinkedEntityType.None,
            LinkedEntityId   = null,
            HasChildren      = false,
            HasTransactions  = false,
            IsSystem         = false,
            StatusVariant    = a.IsActive ? StatusVariant.Success : StatusVariant.Danger,
            StatusText       = a.IsActive ? LocalizationService.Get("Str.Active") : LocalizationService.Get("Str.Inactive"),
            CreatedAt        = DateTime.Now,
            UpdatedAt        = DateTime.Now
        };

        private bool IsSystemAccount(string code) =>
            Settings.GetSection("Accounts").Values.Contains(code);

        private (string Setting, string Root) _assetRoot;

        private string AssetRootCode()
        {
            var cost = Setting(SettingKeys.Accounts.FixedAssets, "");
            if (string.IsNullOrWhiteSpace(cost)) return null;
            if (_assetRoot.Setting == cost) return _assetRoot.Root;

            var root = _accounts.GetByCode(cost)?.ParentCode;
            if (string.IsNullOrWhiteSpace(root)) root = cost;

            _assetRoot = (cost, root);
            return root;
        }

        private bool IsAssetManaged(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;

            var roots = new[] { AssetRootCode(), Setting(SettingKeys.Accounts.Inventory, "") };
            return roots.Any(root => !string.IsNullOrWhiteSpace(root) && code.StartsWith(root, StringComparison.Ordinal));
        }

        private static Result FailAssetManaged() =>
            Result.Fail(LocalizationService.Get("Str.Accounts.AssetManaged"), ErrorCode.ValidationFailed);

        private static List<Account> ApplyFlatFilter(List<Account> accounts, AccountTreeFilter filter)
        {
            IEnumerable<Account> query = accounts;

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var term = filter.SearchText.Trim();
                query = query.Where(a =>
                    a.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    a.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.LevelFilter.HasValue)
                query = query.Where(a => a.Level == filter.LevelFilter.Value);

            if (filter.TypeFilter.HasValue)
                query = query.Where(a => a.Type == (int)filter.TypeFilter.Value);

            if (filter.LeafOnly)
                query = query.Where(a => a.IsLeaf);

            return query.OrderBy(a => a.Code).ToList();
        }

        private List<AccountTreeNode> BuildTree(List<Account> allAccounts, HashSet<string> keepCodes)
        {
            var byParent = allAccounts
                .Where(a => keepCodes.Contains(a.Code))
                .GroupBy(a => a.ParentCode ?? "")
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.Code).ToList());

            List<AccountTreeNode> BuildLevel(string parentCode)
            {
                if (!byParent.TryGetValue(parentCode ?? "", out var children))
                    return new List<AccountTreeNode>();

                return children.Select(a =>
                {
                    var node = ToTreeNode(a, allAccounts);
                    node.Children = BuildLevel(a.Code);
                    return node;
                }).ToList();
            }

            return BuildLevel(null);
        }

        private AccountDto ToDto(Account a, List<Account> allAccounts)
        {
            var parent = string.IsNullOrEmpty(a.ParentCode) ? null : allAccounts.FirstOrDefault(x => x.Code == a.ParentCode);

            return new AccountDto
            {
                Id               = a.Id,
                Code             = a.Code,
                Name             = a.Name,
                ParentId         = parent?.Id,
                ParentCode       = a.ParentCode,
                ParentName       = parent?.Name,
                Level            = a.Level,
                IsLeaf           = a.IsLeaf,
                IsActive         = a.IsActive,
                Type             = (AccountType)a.Type,
                TypeName         = TypeName((AccountType)a.Type),
                Balance          = a.Balance,
                LinkedEntityType = LinkedEntityType.None, // يُستكمل فعلياً في المرحلة F.3
                LinkedEntityId   = null,
                HasChildren      = allAccounts.Any(x => x.ParentCode == a.Code),
                HasTransactions  = _journal.HasLinesForAccount(a.Code),
                IsSystem         = IsSystemAccount(a.Code),
                StatusVariant    = a.IsActive ? StatusVariant.Success : StatusVariant.Danger,
                StatusText       = a.IsActive ? Localization.Get("Str.Active") : Localization.Get("Str.Inactive"),
                CreatedAt        = a.CreatedAt,
                UpdatedAt        = a.UpdatedAt
            };
        }

        private AccountTreeNode ToTreeNode(Account a, List<Account> allAccounts)
        {
            var dto = ToDto(a, allAccounts);
            return new AccountTreeNode
            {
                Id = dto.Id, Code = dto.Code, Name = dto.Name, ParentId = dto.ParentId, ParentCode = dto.ParentCode,
                ParentName = dto.ParentName, Level = dto.Level, IsLeaf = dto.IsLeaf, IsActive = dto.IsActive, Type = dto.Type, TypeName = dto.TypeName,
                Balance = dto.Balance, LinkedEntityType = dto.LinkedEntityType, LinkedEntityId = dto.LinkedEntityId,
                HasChildren = dto.HasChildren, HasTransactions = dto.HasTransactions, IsSystem = dto.IsSystem,
                StatusVariant = dto.StatusVariant
            };
        }

        private static string TypeName(AccountType type) => type switch
        {
            AccountType.Asset     => "أصول",
            AccountType.Liability => "خصوم",
            AccountType.Equity    => "حقوق ملكية",
            AccountType.Revenue   => "إيرادات",
            AccountType.Expense   => "مصروفات",
            _                     => "—"
        };
    }
}
