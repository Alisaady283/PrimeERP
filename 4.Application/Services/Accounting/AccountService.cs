using PrimeERP.Platform.Localization;
using System;
using System.Collections.Generic;
using System.Data.Common;
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
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services.Accounting
{
    /// <summary>
    /// المالك الوحيد لمنطق شجرة الحسابات — Repository تحته CRUD صرف فقط. كل حساب خاص (عملاء/موردون/مخزون...)
    /// يُقرأ من ISettingsService عبر SettingKeys.Accounts، لا يُكتب هنا حرفياً إطلاقاً (كل عميل شجرة مختلفة).
    /// </summary>
    public class AccountService : IAccountService
    {
        private readonly IPermissionService _permissions;
        private readonly ISettingsService _settings;
        private readonly IAccountRepository _accounts;
        private readonly IJournalRepository _journal;

        /// <summary>لحلّ ICustomerService/ISupplierService اختيارياً (قد لا تكونا مسجَّلتين — ISupplierService لم يُبنَ تنفيذها بعد) — بديل ServiceLocator.TryGet عبر IServiceProvider.GetService (يرجع null لا استثناء لو غير مسجَّلة).</summary>
        private readonly IServiceProvider _services;
        private readonly IAuditLogger _audit;

        public AccountService(IPermissionService permissions, ISettingsService settings, IAccountRepository accounts,
            IJournalRepository journal, IServiceProvider services, IAuditLogger audit)
        {
            _permissions = permissions;
            _settings = settings;
            _accounts = accounts;
            _journal = journal;
            _services = services;
            _audit = audit;
        }

        private static string Denied => LocalizationService.Get("Str.PermissionDenied");

        // ===================== القراءة =====================

        public Result<List<AccountTreeNode>> GetTree(AccountTreeFilter filter = null)
        {
            if (!_permissions.Can(PermissionKeys.Accounts.View))
                return Result.Fail<List<AccountTreeNode>>(Denied, ErrorCode.Unauthorized);

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
            if (!_permissions.Can(PermissionKeys.Accounts.View))
                return Result.Fail<PagedResult<AccountDto>>(Denied, ErrorCode.Unauthorized);

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
            if (!_permissions.Can(PermissionKeys.Accounts.View))
                return Result.Fail<List<AccountDto>>(Denied, ErrorCode.Unauthorized);

            var all = _accounts.GetAll();
            var leaves = all.Where(a => a.IsLeaf && (type == null || a.Type == (int)type.Value))
                            .OrderBy(a => a.Code).ToList();

            return Result.Ok(leaves.Select(a => ToDto(a, all)).ToList());
        }

        public Result<AccountDto> GetById(int id)
        {
            if (!_permissions.Can(PermissionKeys.Accounts.View))
                return Result.Fail<AccountDto>(Denied, ErrorCode.Unauthorized);

            var account = _accounts.GetById(id);
            if (account == null)
                return Result.Fail<AccountDto>("الحساب غير موجود", ErrorCode.NotFound);

            return Result.Ok(ToDto(account, _accounts.GetAll(true)));
        }

        public Result<AccountDto> GetByCode(string code)
        {
            if (!_permissions.Can(PermissionKeys.Accounts.View))
                return Result.Fail<AccountDto>(Denied, ErrorCode.Unauthorized);

            var account = _accounts.GetByCode(code);
            if (account == null)
                return Result.Fail<AccountDto>("الحساب غير موجود", ErrorCode.NotFound);

            return Result.Ok(ToDto(account, _accounts.GetAll(true)));
        }

        public Result<string> GenerateChildCode(int parentId)
        {
            if (!_permissions.Can(PermissionKeys.Accounts.Create))
                return Result.Fail<string>(Denied, ErrorCode.Unauthorized);

            var parent = _accounts.GetById(parentId);
            if (parent == null)
                return Result.Fail<string>("الحساب الأب غير موجود", ErrorCode.NotFound);

            return Result.Ok(GenerateChildCodeInternal(parent.Code));
        }

        // ===================== الكتابة =====================

        public Result<AccountDto> Create(CreateAccountDto dto)
        {
            if (!_permissions.Can(PermissionKeys.Accounts.Create))
                return Result.Fail<AccountDto>(Denied, ErrorCode.Unauthorized);

            var parent = _accounts.GetById(dto.ParentId);
            if (parent == null)
                return Result.Fail<AccountDto>("الحساب الأب غير موجود", ErrorCode.NotFound);

            // الأب يجب ألّا يكون Leaf: حساب Leaf يقبل قيوداً مباشرة، فلا يجوز تحويله لأب بصمت عبر مجرد إضافة
            // ابن — ذلك قرار إداري صريح يتم عبر Update (IsLeaf: true→false)، لا تلقائياً هنا. حسابات SeedDefaults
            // الجذرية كلها IsLeaf=false من البداية أصلاً (فئات لا حسابات فعلية)، فلا تعارض مع الاستخدام الطبيعي.
            if (parent.IsLeaf)
                return Result.Fail<AccountDto>("لا يمكن إضافة حساب فرعي تحت حساب يقبل قيوداً مباشرة (Leaf) — حوّله لأب أولاً عبر التعديل", ErrorCode.ValidationFailed);

            var code = GenerateChildCodeInternal(parent.Code);
            var account = BuildNewAccount(dto, parent, code);

            var validation = new AccountValidator(_accounts, isEdit: false).Validate(account);
            if (!validation.IsValid)
                return Result.Fail<AccountDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            var link = ResolveAutoLink(dto.SkipAutoLink, parent.Code);
            if (!link.IsSuccess)
                return Result.Fail<AccountDto>(link.ErrorMessage, link.ErrorCode);

            var newId = Db.RunTransaction((conn, tx) =>
            {
                var id = _accounts.Insert(account, conn, tx);
                account.Id = id;

                if (link.Value.AutoLinkEnabled)
                {
                    if (link.Value.IsUnderCustomers) link.Value.CustomerService.CreateFromAccount(conn, tx, account.Code, account.Name);
                    else if (link.Value.IsUnderSuppliers) link.Value.SupplierService.CreateFromAccount(conn, tx, account.Code, account.Name);
                }
                // الأب = المخزون → لا ربط تلقائي عمداً (المنتج يُنشئ حسابه لا العكس) — لا فرع هنا عمداً.

                return id;
            });

            _audit.Log("Accounts", newId, AuditAction.Insert, newValue: new { account.Code, account.Name });

            var created = _accounts.GetById(newId);
            return Result.Ok(ToDto(created, _accounts.GetAll(true)));
        }

        /// <summary>
        /// بمعاملة خارجية — يخدم CustomerService/SupplierService.Create (dto.SkipAutoLink=true دائماً هنا،
        /// وإلا حلقة لا نهائية: CustomerService.Create → AccountService.Create → CustomerService.CreateFromAccount
        /// → ...). بلا تحقق صلاحية Accounts.Create (المستدعي تحقق صلاحيته الخاصة Customers.Create/Suppliers.
        /// Create). كل قراءة هنا عبر (conn,tx) القائمة صراحة — بما فيها AccountValidator (checkUniqueness:false؛
        /// الكود مولَّد برمجياً بحساب أقصى رقم فرعي حالي + 1، لا يمكن أن يتصادم بحكم طريقة توليده، فإعادة
        /// التحقق عبر استعلام DB منفصل — الذي كان سيحتاج معالجة (conn,tx) خاصة به أصلاً — تكرار غير ضروري).
        /// يبني AccountDto مباشرة من البيانات المتوفرة بلا إعادة قراءة (ToDto العادية تستدعي _journal.
        /// HasLinesForAccount و_settings.GetSection غير الآمنين هنا — راجع BuildFreshAccountDto).
        /// </summary>
        public Result<AccountDto> Create(DbConnection conn, DbTransaction tx, CreateAccountDto dto)
        {
            var parent = _accounts.GetById(dto.ParentId, conn, tx);
            if (parent == null)
                return Result.Fail<AccountDto>("الحساب الأب غير موجود", ErrorCode.NotFound);

            if (parent.IsLeaf)
                return Result.Fail<AccountDto>("لا يمكن إضافة حساب فرعي تحت حساب يقبل قيوداً مباشرة (Leaf) — حوّله لأب أولاً عبر التعديل", ErrorCode.ValidationFailed);

            var code = GenerateChildCodeInternal(conn, tx, parent.Code);
            var account = BuildNewAccount(dto, parent, code);

            var validation = new AccountValidator(_accounts, isEdit: false, checkUniqueness: false).Validate(account);
            if (!validation.IsValid)
                return Result.Fail<AccountDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            var id = _accounts.Insert(account, conn, tx);
            account.Id = id;

            // لا منطق ربط هنا إطلاقاً — dto.SkipAutoLink=true دائماً في هذا المسار.

            return Result.Ok(BuildFreshAccountDto(account, parent));
        }

        public Result Update(UpdateAccountDto dto)
        {
            if (!_permissions.Can(PermissionKeys.Accounts.Edit))
                return Result.Fail(Denied, ErrorCode.Unauthorized);

            var account = _accounts.GetById(dto.Id);
            if (account == null)
                return Result.Fail("الحساب غير موجود", ErrorCode.NotFound);

            if (IsSystemAccount(account.Code) && !_permissions.Can(PermissionKeys.Settings.System))
                return Result.Fail(LocalizationService.Get("Str.Settings.SystemPermissionDenied"), ErrorCode.Unauthorized);

            if (dto.IsLeaf && !account.IsLeaf && _accounts.HasChildren(account.Code))
                return Result.Fail("لا يمكن جعل الحساب فرعياً (Leaf) وله حسابات أبناء", ErrorCode.ValidationFailed);

            if (!dto.IsLeaf && account.IsLeaf && _journal.HasLinesForAccount(account.Code))
                return Result.Fail("لا يمكن تحويل الحساب لأب وله قيود مسجَّلة عليه مباشرة", ErrorCode.ValidationFailed);

            var nameChanged = account.Name != dto.Name; // قبل الاستبدال أدناه

            account.Name   = dto.Name;
            account.Notes  = dto.Notes;
            account.IsLeaf = dto.IsLeaf;

            var validation = new AccountValidator(_accounts, isEdit: true).Validate(account);
            if (!validation.IsValid)
                return Result.Fail(validation.Errors.Values.ToList(), ErrorCode.ValidationFailed);

            // مزامنة اسم العميل/المورد المرتبط لو تغيّر الاسم فقط — اتجاه واحد (حساب→طرف)؛ الاتجاه المعاكس
            // (طرف→حساب) عبر IAccountService.UpdateName لا يستدعي هذا مرة أخرى، فلا حلقة ping-pong. راجع
            // ICustomerService.UpdateNameFromAccount للتفاصيل.
            Result<AutoLinkResolution> link = null;
            if (nameChanged)
            {
                link = ResolveAutoLink(skipAutoLink: false, account.ParentCode);
                if (!link.IsSuccess)
                    return Result.Fail(link.ErrorMessage, link.ErrorCode);
            }

            Db.RunTransaction((conn, tx) =>
            {
                _accounts.Update(account, conn, tx);

                if (nameChanged && link.Value.AutoLinkEnabled)
                {
                    if (link.Value.IsUnderCustomers) link.Value.CustomerService.UpdateNameFromAccount(conn, tx, account.Code, account.Name);
                    else if (link.Value.IsUnderSuppliers) link.Value.SupplierService.UpdateNameFromAccount(conn, tx, account.Code, account.Name);
                }
            });

            _audit.Log("Accounts", account.Id, AuditAction.Update, newValue: new { account.Name, account.IsLeaf });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!_permissions.Can(PermissionKeys.Accounts.Delete))
                return Result.Fail(Denied, ErrorCode.Unauthorized);

            var account = _accounts.GetById(id);
            if (account == null)
                return Result.Fail("الحساب غير موجود", ErrorCode.NotFound);

            if (IsSystemAccount(account.Code))
                return Result.Fail("لا يمكن حذف حساب نظامي", ErrorCode.ValidationFailed);

            if (_accounts.HasChildren(account.Code))
                return Result.Fail("لا يمكن حذف حساب له حسابات أبناء", ErrorCode.ValidationFailed);

            if (_journal.HasLinesForAccount(account.Code))
                return Result.Fail("لا يمكن حذف حساب له قيود مسجَّلة", ErrorCode.ValidationFailed);

            var link = ResolveAutoLink(skipAutoLink: false, account.ParentCode);
            if (!link.IsSuccess)
                return Result.Fail(link.ErrorMessage, link.ErrorCode);

            Db.RunTransaction((conn, tx) =>
            {
                _accounts.Delete(account.Code, conn, tx);

                if (link.Value.AutoLinkEnabled)
                {
                    if (link.Value.IsUnderCustomers) link.Value.CustomerService.DeleteByAccountCode(conn, tx, account.Code);
                    else if (link.Value.IsUnderSuppliers) link.Value.SupplierService.DeleteByAccountCode(conn, tx, account.Code);
                }
            });

            _audit.Log("Accounts", account.Id, AuditAction.Delete, details: account.Code);
            return Result.Ok();
        }

        /// <summary>بمعاملة خارجية، بالكود مباشرة (لا Id — المستدعي يملكه بالفعل) — يخدم CustomerService/SupplierService.Delete. بلا تحقق قيود/أبناء (تحقّقها المستدعي قبل فتح معاملته) وبلا استدعاء DeleteByAccountCode عكسياً هنا (يمنع حلقة ping-pong — الطرف المرتبط يحذف نفسه هو، لا AccountService ينوب عنه).</summary>
        public Result Delete(DbConnection conn, DbTransaction tx, string accountCode)
        {
            _accounts.Delete(accountCode, conn, tx);
            return Result.Ok();
        }

        /// <summary>يزامن اسم حساب من تعديل الطرف المرتبط (عميل/مورد) — الاسم فقط، بلا تحقق تحويل Leaf أو صلاحية Settings.System (غير ذي صلة لحساب طرف مرتبط دائماً Leaf وليس نظامياً). يخدم CustomerService/SupplierService.Update ضمن معاملتهما.</summary>
        public Result UpdateName(DbConnection conn, DbTransaction tx, string accountCode, string name)
        {
            _accounts.UpdateName(conn, tx, accountCode, name);
            return Result.Ok();
        }

        // ===================== الأرصدة =====================

        public Result RecalculateBalance(string code)
        {
            if (!_permissions.Can(PermissionKeys.Accounts.Edit))
                return Result.Fail(Denied, ErrorCode.Unauthorized);

            var account = _accounts.GetByCode(code);
            if (account == null)
                return Result.Fail("الحساب غير موجود", ErrorCode.NotFound);

            var balance = ComputeBalance(code);
            Db.RunTransaction((conn, tx) => _accounts.UpdateBalance(code, balance, conn, tx));

            _audit.Log("Accounts", account.Id, AuditAction.Update, details: $"إعادة حساب رصيد {code}: {balance:N2}");
            return Result.Ok();
        }

        /// <summary>
        /// تُستدعى من خدمات أخرى (JournalService.Post/Unpost) وهي بالفعل داخل معاملتها الخاصة — تقرأ عبر نفس
        /// (conn,tx) لا اتصال جديد (Db.Query العادية تُعلِّق/deadlock على SQLite لو استُخدمت هنا، لأن القفل
        /// الكتابي للمعاملة الخارجية لا يُحرَّر حتى تعود؛ راجع تعليق DbHelper.Query(conn,tx,...)).
        /// </summary>
        public Result RecalculateBalance(DbConnection conn, DbTransaction tx, string code)
        {
            _accounts.UpdateBalance(code, ComputeBalance(conn, tx, code), conn, tx);
            return Result.Ok();
        }

        private decimal ComputeBalance(string code) =>
            _journal.GetPostedLinesForAccount(code, null, null).Sum(l => l.Debit - l.Credit);

        private decimal ComputeBalance(DbConnection conn, DbTransaction tx, string code) =>
            _journal.GetPostedLinesForAccount(code, null, null, conn, tx).Sum(l => l.Debit - l.Credit);

        public Result RecalculateAllBalances()
        {
            if (!_permissions.Can(PermissionKeys.Accounts.Edit))
                return Result.Fail(Denied, ErrorCode.Unauthorized);

            var leaves = _accounts.GetLeaves();

            Db.RunTransaction((conn, tx) =>
            {
                foreach (var account in leaves)
                    _accounts.UpdateBalance(account.Code, ComputeBalance(account.Code), conn, tx);
            });

            _audit.Log("Accounts", 0, AuditAction.Update, details: $"إعادة حساب كل الأرصدة ({leaves.Count} حساب)");
            return Result.Ok();
        }

        public Result<decimal> GetBalanceAsOf(string code, DateTime date)
        {
            if (!_permissions.Can(PermissionKeys.Accounts.View))
                return Result.Fail<decimal>(Denied, ErrorCode.Unauthorized);

            var account = _accounts.GetByCode(code);
            if (account == null)
                return Result.Fail<decimal>("الحساب غير موجود", ErrorCode.NotFound);

            var lines = _journal.GetPostedLinesForAccount(code, null, date);
            return Result.Ok(lines.Sum(l => l.Debit - l.Credit));
        }

        public Result<List<AccountStatementLine>> GetStatement(string code, DateTime from, DateTime to)
        {
            if (!_permissions.Can(PermissionKeys.Accounts.View))
                return Result.Fail<List<AccountStatementLine>>(Denied, ErrorCode.Unauthorized);

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

        // ===================== المساعدات =====================
        // بلا تحقق صلاحية عمداً — استعلامات بنيوية صرفة (bool/enum لا بيانات حسّاسة) تستدعيها خدمات أخرى
        // (JournalService.Create مثلاً) للتحقق من قواعد أعمال، لا ينبغي أن تُحجب بصلاحية Accounts.View
        // المستخدم الحالي لو كان الإجراء الأصلي (تسجيل قيد) له صلاحيته الخاصة المُتحقَّق منها بالفعل.

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

        // ===================== أدوات داخلية =====================

        private static Account BuildNewAccount(CreateAccountDto dto, Account parent, string code) => new()
        {
            Code       = code,
            Name       = dto.Name,
            ParentCode = parent.Code,
            Level      = parent.Level + 1,
            IsLeaf     = dto.IsLeaf,
            Type       = parent.Type,
            Notes      = dto.Notes,
            IsActive   = true
        };

        /// <summary>نتيجة تحديد وضع الربط التلقائي (Create/Update/Delete الثلاثة تشترك في نفس المنطق) — كلاس لا Tuple لوضوح الاستدعاء.</summary>
        private class AutoLinkResolution
        {
            public bool AutoLinkEnabled;
            public bool IsUnderCustomers;
            public bool IsUnderSuppliers;
            public ICustomerService CustomerService;
            public ISupplierService SupplierService;
        }

        /// <summary>
        /// يحدّد هل حساب (تحت parentOrAccountCode) مرتبط تلقائياً بعميل/مورد، ويحلّ الخدمة المطلوبة عبر
        /// IServiceProvider.GetService — Fail صريح لو AutoLinkEnabled=true والخدمة غير مسجَّلة (ترجع null، لا
        /// سكوت). skipAutoLink=true يتخطّى كل هذا فوراً (يُستخدم من Create فقط، عبر CreateAccountDto.SkipAutoLink
        /// — Update/Delete يمرّران false دائماً).
        /// </summary>
        private Result<AutoLinkResolution> ResolveAutoLink(bool skipAutoLink, string parentOrAccountCode)
        {
            if (skipAutoLink)
                return Result.Ok(new AutoLinkResolution());

            var customersRoot   = _settings.Get(SettingKeys.Accounts.Customers, "");
            var suppliersRoot   = _settings.Get(SettingKeys.Accounts.Suppliers, "");
            var autoLinkEnabled = _settings.Get(SettingKeys.Accounts.AutoLinkEnabled, true);
            var isUnderCustomers = !string.IsNullOrEmpty(customersRoot) && parentOrAccountCode == customersRoot;
            var isUnderSuppliers = !string.IsNullOrEmpty(suppliersRoot) && parentOrAccountCode == suppliersRoot;

            var resolution = new AutoLinkResolution { AutoLinkEnabled = autoLinkEnabled, IsUnderCustomers = isUnderCustomers, IsUnderSuppliers = isUnderSuppliers };

            if (autoLinkEnabled)
            {
                if (isUnderCustomers)
                {
                    resolution.CustomerService = (ICustomerService)_services.GetService(typeof(ICustomerService));
                    if (resolution.CustomerService == null)
                        return Result.Fail<AutoLinkResolution>(LocalizationService.Get("Str.Accounts.CustomerLinkUnavailable"), ErrorCode.Unexpected);
                }

                if (isUnderSuppliers)
                {
                    resolution.SupplierService = (ISupplierService)_services.GetService(typeof(ISupplierService));
                    if (resolution.SupplierService == null)
                        return Result.Fail<AutoLinkResolution>(LocalizationService.Get("Str.Accounts.SupplierLinkUnavailable"), ErrorCode.Unexpected);
                }
            }

            return Result.Ok(resolution);
        }

        private string GenerateChildCodeInternal(string parentCode) =>
            BuildChildCode(parentCode, _accounts.GetChildren(parentCode));

        /// <summary>نفس GenerateChildCodeInternal أعلاه من داخل معاملة قائمة — تستخدمها Create(conn,tx,...).</summary>
        private string GenerateChildCodeInternal(DbConnection conn, DbTransaction tx, string parentCode) =>
            BuildChildCode(parentCode, _accounts.GetChildren(parentCode, conn, tx));

        private static string BuildChildCode(string parentCode, List<Account> children)
        {
            if (children.Count == 0)
                return parentCode + "001";

            int max = 0;
            foreach (var acc in children)
            {
                var suffix = acc.Code.Length > parentCode.Length ? acc.Code.Substring(parentCode.Length) : "";
                if (int.TryParse(suffix, out int n) && n > max)
                    max = n;
            }
            return parentCode + (max + 1).ToString("D3");
        }

        /// <summary>يبني AccountDto من بيانات لحظة الإنشاء مباشرة، بلا إعادة قراءة من DB — حساب جديد فعلياً لا أبناء/قيود/علم نظامي له بحكم كونه جديداً (لا تنازل، إجابة صحيحة فعلاً لا تقريب). يخدم Create(conn,tx,...) تفادياً لقراءات _journal.HasLinesForAccount/_settings.GetSection غير الآمنتين داخل معاملة خارجية.</summary>
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
            StatusVariant    = StatusVariant.Success
        };

        private bool IsSystemAccount(string code) =>
            _settings.GetSection("Accounts").Values.Contains(code);

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
                // استعلام لكل حساب — مقبول لحجم شجرة حسابات نموذجي؛ يُستبدل باستعلام مجمَّع واحد لو كبر العدد كثيراً.
                HasTransactions  = _journal.HasLinesForAccount(a.Code),
                IsSystem         = IsSystemAccount(a.Code),
                StatusVariant    = a.IsActive ? StatusVariant.Success : StatusVariant.Danger
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
