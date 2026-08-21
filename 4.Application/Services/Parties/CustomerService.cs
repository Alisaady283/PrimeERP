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
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;
using Auditor = PrimeERP.Platform.Audit.AuditLogger;
using AuditAction = PrimeERP.Domain.Enums.AuditAction;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services.Parties
{
    /// <summary>
    /// المالك الوحيد لمنطق العملاء — Repository تحته CRUD صرف فقط. كل حساب يُنشأ/يُحدَّث/يُحذف عبر
    /// IAccountService حصراً (لا CustomerRepository يلمس جدول Accounts). CreateAccountDto.SkipAutoLink=true
    /// إلزامي في كل استدعاء IAccountService.Create من هنا — يقطع الحلقة اللانهائية مع AccountService.Create
    /// (الذي يستدعي CreateFromAccount أدناه عند الربط التلقائي) — راجع MIGRATION_INVENTORY.md.
    /// </summary>
    public class CustomerService : ICustomerService
    {
        public static readonly CustomerService Instance = new();

        private readonly IPermissionService _permissions = PermissionService.Instance;
        private readonly ISettingsService _settings = SettingsService.Instance;
        private readonly IAccountService _accounts = AccountService.Instance;
        private readonly INumberSequenceService _numbers = NumberSequenceService.Instance;

        private static string Denied => LocalizationService.Get("Str.PermissionDenied");
        private static string CurrentUser => AppSession.Username ?? "Admin";

        // ===================== القراءة =====================

        public Result<PagedResult<CustomerDto>> GetPaged(int page, int pageSize, CustomerFilter filter = null)
        {
            if (!_permissions.Can(PermissionKeys.Customers.View))
                return Result.Fail<PagedResult<CustomerDto>>(Denied, ErrorCode.Unauthorized);

            filter ??= new CustomerFilter();

            var (items, total) = CustomerRepository.GetPaged(
                page, pageSize,
                filter.SearchText, filter.IsActive, filter.HasBalance, filter.OverCreditLimit,
                filter.CategoryId, filter.SortBy, filter.SortDescending);

            return Result.Ok(new PagedResult<CustomerDto> { Items = items.Select(ToDto).ToList(), TotalCount = total, Page = page, PageSize = pageSize });
        }

        public Result<CustomerDto> GetById(int id)
        {
            if (!_permissions.Can(PermissionKeys.Customers.View))
                return Result.Fail<CustomerDto>(Denied, ErrorCode.Unauthorized);

            var customer = CustomerRepository.GetById(id);
            if (customer == null)
                return Result.Fail<CustomerDto>(LocalizationService.Get("Str.Customer.NotFound"), ErrorCode.NotFound);

            return Result.Ok(ToDto(customer));
        }

        public Result<CustomerDto> GetByCode(string code)
        {
            if (!_permissions.Can(PermissionKeys.Customers.View))
                return Result.Fail<CustomerDto>(Denied, ErrorCode.Unauthorized);

            var customer = CustomerRepository.GetByCode(code);
            if (customer == null)
                return Result.Fail<CustomerDto>(LocalizationService.Get("Str.Customer.NotFound"), ErrorCode.NotFound);

            return Result.Ok(ToDto(customer));
        }

        public Result<List<CustomerDto>> Search(string term, int maxResults = 50)
        {
            if (!_permissions.Can(PermissionKeys.Customers.View))
                return Result.Fail<List<CustomerDto>>(Denied, ErrorCode.Unauthorized);

            return Result.Ok(CustomerRepository.Search(term ?? "", maxResults).Select(ToDto).ToList());
        }

        public Result<List<AccountStatementLine>> GetStatement(int id, DateTime from, DateTime to)
        {
            if (!_permissions.Can(PermissionKeys.Customers.View))
                return Result.Fail<List<AccountStatementLine>>(Denied, ErrorCode.Unauthorized);

            var customer = CustomerRepository.GetById(id);
            if (customer == null)
                return Result.Fail<List<AccountStatementLine>>(LocalizationService.Get("Str.Customer.NotFound"), ErrorCode.NotFound);

            if (string.IsNullOrWhiteSpace(customer.AccountCode))
                return Result.Fail<List<AccountStatementLine>>(LocalizationService.Get("Str.Customer.AccountNotConfigured"), ErrorCode.Unexpected);

            return _accounts.GetStatement(customer.AccountCode, from, to);
        }

        // ===================== الإنشاء =====================

        public Result<CustomerDto> Create(CreateCustomerDto dto)
        {
            if (!_permissions.Can(PermissionKeys.Customers.Create))
                return Result.Fail<CustomerDto>(Denied, ErrorCode.Unauthorized);

            var customersAccountCode = _settings.Get(SettingKeys.Accounts.Customers, "");
            if (string.IsNullOrWhiteSpace(customersAccountCode))
                return Result.Fail<CustomerDto>(LocalizationService.Get("Str.Customer.AccountNotConfigured"), ErrorCode.Unexpected);

            var parentAccount = AccountRepository.GetByCode(customersAccountCode);
            if (parentAccount == null)
                return Result.Fail<CustomerDto>(LocalizationService.Get("Str.Customer.AccountParentNotFound"), ErrorCode.NotFound);

            if (parentAccount.IsLeaf)
                return Result.Fail<CustomerDto>(LocalizationService.Get("Str.Customer.AccountParentIsLeaf"), ErrorCode.ValidationFailed);

            var code = _numbers.Next("Customer");
            var customer = BuildNewCustomer(dto, code);

            var validation = new CustomerValidator().Validate(customer);
            if (!validation.IsValid)
                return Result.Fail<CustomerDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            // تحذيرات لا تمنع — تُسجَّل في تفاصيل Audit فقط، لا Fail.
            var nameIsDuplicate = _settings.Get(SettingKeys.Financial.WarnOnDuplicateCustomerName, true) && CustomerRepository.ExistsName(customer.Name);
            var phoneIsDuplicate = _settings.Get(SettingKeys.Financial.WarnOnDuplicatePhone, true)
                && !string.IsNullOrWhiteSpace(customer.Phone) && CustomerRepository.ExistsPhone(customer.Phone);

            int newId;
            try
            {
                newId = Db.RunTransaction((conn, tx) =>
                {
                    var accountResult = _accounts.Create(conn, tx, new CreateAccountDto
                    {
                        ParentId = parentAccount.Id,
                        Name = customer.Name,
                        IsLeaf = true,
                        SkipAutoLink = true // ⚠️ إلزامي — يمنع AccountService.Create من استدعاء CreateFromAccount ثانية (حلقة لا نهائية)
                    });

                    if (!accountResult.IsSuccess)
                        throw new InvalidOperationException(accountResult.ErrorMessage);

                    customer.AccountCode = accountResult.Value.Code;
                    customer.CreatedBy = CurrentUser;

                    return CustomerRepository.Insert(conn, tx, customer);
                });
            }
            catch (Exception ex)
            {
                // فشل إنشاء الحساب (أو أي خطأ آخر داخل المعاملة، بما فيها أخطاء DB خام مثل خرق قيد تفرّد) →
                // rollback الكل (Db.RunTransaction تراجعت بالفعل قبل إعادة رمي الاستثناء) وتحويله لـ Result.Fail
                // بدل تسريبه كاستثناء خام للمستدعي.
                return Result.Fail<CustomerDto>(ex.Message);
            }

            var warnings = string.Join(" ", new[]
            {
                nameIsDuplicate ? "تحذير: اسم مكرر." : null,
                phoneIsDuplicate ? "تحذير: رقم هاتف مكرر." : null
            }.Where(w => w != null));

            Auditor.Log("Customers", newId, AuditAction.Insert, newValue: new { customer.Code, customer.Name },
                details: string.IsNullOrEmpty(warnings) ? null : warnings);

            customer.Id = newId;
            return Result.Ok(ToDto(customer));
        }

        /// <summary>بمعاملة خارجية — يخدم مستندات F.4 (فاتورة تنشئ عميلاً جديداً ضمن معاملتها). بلا تحقق صلاحية (المستدعي تحقق صلاحيته الخاصة).</summary>
        public Result<CustomerDto> Create(DbConnection conn, DbTransaction tx, CreateCustomerDto dto)
        {
            var customersAccountCode = _settings.Get(SettingKeys.Accounts.Customers, "");
            if (string.IsNullOrWhiteSpace(customersAccountCode))
                return Result.Fail<CustomerDto>(LocalizationService.Get("Str.Customer.AccountNotConfigured"), ErrorCode.Unexpected);

            // قراءة مباشرة عبر AccountRepository (لا IAccountService.GetByCode — تبني AccountDto كاملاً عبر
            // قراءات إضافية غير آمنة داخل معاملة خارجية؛ راجع تعليق AccountService.Create(conn,tx,...))، نفس
            // نمط JournalService.ValidateAccountsForTransaction من F.2.3.
            var parentAccount = AccountRepository.GetByCode(conn, tx, customersAccountCode);
            if (parentAccount == null)
                return Result.Fail<CustomerDto>(LocalizationService.Get("Str.Customer.AccountParentNotFound"), ErrorCode.NotFound);

            if (parentAccount.IsLeaf)
                return Result.Fail<CustomerDto>(LocalizationService.Get("Str.Customer.AccountParentIsLeaf"), ErrorCode.ValidationFailed);

            var code = _numbers.Next(conn, tx, "Customer");
            var customer = BuildNewCustomer(dto, code);

            var validation = new CustomerValidator().Validate(customer);
            if (!validation.IsValid)
                return Result.Fail<CustomerDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            var accountResult = _accounts.Create(conn, tx, new CreateAccountDto
            {
                ParentId = parentAccount.Id, Name = customer.Name, IsLeaf = true, SkipAutoLink = true
            });
            if (!accountResult.IsSuccess)
                return Result.Fail<CustomerDto>(accountResult.ErrorMessage, accountResult.ErrorCode);

            customer.AccountCode = accountResult.Value.Code;
            customer.CreatedBy = CurrentUser;

            var newId = CustomerRepository.Insert(conn, tx, customer);
            customer.Id = newId;

            return Result.Ok(ToDto(customer));
        }

        /// <summary>الاتجاه المعاكس — يستدعيه AccountService.Create عند الربط التلقائي (حساب أُنشئ بالفعل تحت SettingKeys.Accounts.Customers). لا ينشئ حساباً، ينشئ العميل فقط بـ AccountCode المُمرَّر. بلا Audit مستقل (AccountService.Create سجّلت العملية بالفعل).</summary>
        public Result<CustomerDto> CreateFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name)
        {
            var code = _numbers.Next(conn, tx, "Customer");

            var customer = new Customer
            {
                Code        = code,
                Name        = name,
                AccountCode = accountCode,
                IsActive    = true,
                CreatedBy   = CurrentUser
            };

            var validation = new CustomerValidator().Validate(customer);
            if (!validation.IsValid)
                return Result.Fail<CustomerDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            var newId = CustomerRepository.Insert(conn, tx, customer);
            customer.Id = newId;

            return Result.Ok(ToDto(customer));
        }

        // ===================== التعديل =====================

        public Result Update(UpdateCustomerDto dto)
        {
            if (!_permissions.Can(PermissionKeys.Customers.Edit))
                return Result.Fail(Denied, ErrorCode.Unauthorized);

            var customer = CustomerRepository.GetById(dto.Id);
            if (customer == null)
                return Result.Fail(LocalizationService.Get("Str.Customer.NotFound"), ErrorCode.NotFound);

            var nameChanged = customer.Name != dto.Name; // قبل الاستبدال أدناه

            customer.Name            = dto.Name;
            customer.NameEn          = dto.NameEn;
            customer.Phone           = dto.Phone;
            customer.Phone2          = dto.Phone2;
            customer.Email           = dto.Email;
            customer.Address         = dto.Address;
            customer.City            = dto.City;
            customer.Country         = dto.Country;
            customer.TaxNumber       = dto.TaxNumber;
            customer.CommercialRegNo = dto.CommercialRegNo;
            customer.CreditLimit     = dto.CreditLimit;
            customer.PaymentTermDays = dto.PaymentTermDays;
            customer.Notes           = dto.Notes;
            customer.IsActive        = dto.IsActive;
            customer.UpdatedBy       = CurrentUser;

            var validation = new CustomerValidator().Validate(customer);
            if (!validation.IsValid)
                return Result.Fail(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            Db.RunTransaction((conn, tx) =>
            {
                CustomerRepository.Update(conn, tx, customer);

                // مزامنة اسم الحساب لو تغيّر الاسم — اتجاه واحد (عميل→حساب)؛ الاتجاه المعاكس (حساب→عميل) عبر
                // UpdateNameFromAccount أدناه لا يستدعي هذا مرة أخرى، فلا حلقة ping-pong.
                if (nameChanged && !string.IsNullOrWhiteSpace(customer.AccountCode))
                    _accounts.UpdateName(conn, tx, customer.AccountCode, customer.Name);
            });

            Auditor.Log("Customers", customer.Id, AuditAction.Update, newValue: new { customer.Name });
            return Result.Ok();
        }

        /// <summary>الاتجاه المعاكس — يستدعيه AccountService.Update عند تعديل اسم الحساب مباشرة. يحدّث اسم العميل فقط بلا مزامنة عكسية (يمنع حلقة ping-pong).</summary>
        public Result UpdateNameFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name)
        {
            CustomerRepository.UpdateNameByAccountCode(conn, tx, accountCode, name);
            return Result.Ok();
        }

        // ===================== الحذف =====================

        public Result Delete(int id)
        {
            if (!_permissions.Can(PermissionKeys.Customers.Delete))
                return Result.Fail(Denied, ErrorCode.Unauthorized);

            var customer = CustomerRepository.GetById(id);
            if (customer == null)
                return Result.Fail(LocalizationService.Get("Str.Customer.NotFound"), ErrorCode.NotFound);

            // TODO F.4: تحقق الفواتير (Sales/Purchase) — لا خدمة فواتير مبنية بعد، يُضاف فور بنائها في F.4.
            if (!string.IsNullOrWhiteSpace(customer.AccountCode) && JournalRepository.HasLinesForAccount(customer.AccountCode))
                return Result.Fail(LocalizationService.Get("Str.Customer.HasTransactions"), ErrorCode.ValidationFailed);

            Db.RunTransaction((conn, tx) =>
            {
                CustomerRepository.Delete(conn, tx, id, CurrentUser);
                if (!string.IsNullOrWhiteSpace(customer.AccountCode))
                    _accounts.Delete(conn, tx, customer.AccountCode);
            });

            Auditor.Log("Customers", id, AuditAction.Delete, details: customer.Code);
            return Result.Ok();
        }

        /// <summary>الاتجاه المعاكس — يستدعيه AccountService.Delete عند حذف الحساب مباشرة. يحذف العميل فقط بلا لمس الحساب (محذوف بالفعل من طرف الاستدعاء). idempotent: لا عميل مرتبط = لا خطأ.</summary>
        public Result DeleteByAccountCode(DbConnection conn, DbTransaction tx, string accountCode)
        {
            var customer = CustomerRepository.GetByAccountCode(conn, tx, accountCode);
            if (customer == null)
                return Result.Ok();

            CustomerRepository.Delete(conn, tx, customer.Id, CurrentUser);
            return Result.Ok();
        }

        // ===================== الأرصدة والائتمان =====================

        public Result RecalculateBalance(int id)
        {
            if (!_permissions.Can(PermissionKeys.Customers.Edit))
                return Result.Fail(Denied, ErrorCode.Unauthorized);

            var customer = CustomerRepository.GetById(id);
            if (customer == null)
                return Result.Fail(LocalizationService.Get("Str.Customer.NotFound"), ErrorCode.NotFound);

            if (string.IsNullOrWhiteSpace(customer.AccountCode))
                return Result.Fail(LocalizationService.Get("Str.Customer.AccountNotConfigured"), ErrorCode.Unexpected);

            var balanceResult = _accounts.GetBalanceAsOf(customer.AccountCode, DateTime.Today);
            if (!balanceResult.IsSuccess)
                return Result.Fail(balanceResult.ErrorMessage, balanceResult.ErrorCode);

            Db.RunTransaction((conn, tx) => CustomerRepository.SetBalance(conn, tx, id, balanceResult.Value));
            return Result.Ok();
        }

        public Result RecalculateAllBalances()
        {
            if (!_permissions.Can(PermissionKeys.Customers.Edit))
                return Result.Fail(Denied, ErrorCode.Unauthorized);

            var customers = CustomerRepository.GetAll(activeOnly: false).Where(c => !string.IsNullOrWhiteSpace(c.AccountCode)).ToList();

            // كل قراءات الرصيد قبل فتح المعاملة (IAccountService.GetBalanceAsOf غير آمنة داخل معاملة خارجية —
            // نفس سبب كل قراءة أخرى في هذه الجلسة، راجع AccountService.RecalculateBalance(conn,tx,...)).
            var balances = new List<(int Id, decimal Balance)>();
            foreach (var c in customers)
            {
                var balanceResult = _accounts.GetBalanceAsOf(c.AccountCode, DateTime.Today);
                if (balanceResult.IsSuccess)
                    balances.Add((c.Id, balanceResult.Value));
            }

            Db.RunTransaction((conn, tx) =>
            {
                foreach (var (id, balance) in balances)
                    CustomerRepository.SetBalance(conn, tx, id, balance);
            });

            return Result.Ok();
        }

        public Result<CreditCheckResult> CheckCreditLimit(int id, decimal additional)
        {
            if (!_permissions.Can(PermissionKeys.Customers.View))
                return Result.Fail<CreditCheckResult>(Denied, ErrorCode.Unauthorized);

            var customer = CustomerRepository.GetById(id);
            if (customer == null)
                return Result.Fail<CreditCheckResult>(LocalizationService.Get("Str.Customer.NotFound"), ErrorCode.NotFound);

            if (customer.CreditLimit <= 0)
                return Result.Ok(new CreditCheckResult
                {
                    IsAllowed = true, CurrentBalance = customer.Balance, CreditLimit = 0,
                    AvailableCredit = decimal.MaxValue, ExceededBy = 0m // 0 = لا حد؛ AvailableCredit بلا سقف فعلي
                });

            var projectedBalance = customer.Balance + additional;
            var exceededBy = projectedBalance > customer.CreditLimit ? projectedBalance - customer.CreditLimit : 0m;

            var result = new CreditCheckResult
            {
                IsAllowed       = exceededBy == 0m,
                CurrentBalance  = customer.Balance,
                CreditLimit     = customer.CreditLimit,
                AvailableCredit = customer.CreditLimit - customer.Balance,
                ExceededBy      = exceededBy
            };

            return exceededBy > 0m
                ? Result.Fail<CreditCheckResult>($"{LocalizationService.Get("Str.Customer.CreditLimitExceeded")}: {exceededBy:N2}", ErrorCode.ValidationFailed)
                : Result.Ok(result);
        }

        // ===================== أدوات داخلية =====================

        private static Customer BuildNewCustomer(CreateCustomerDto dto, string code) => new()
        {
            Code            = code,
            Name            = dto.Name,
            NameEn          = dto.NameEn,
            Phone           = dto.Phone,
            Phone2          = dto.Phone2,
            Email           = dto.Email,
            Address         = dto.Address,
            City            = dto.City,
            Country         = dto.Country,
            TaxNumber       = dto.TaxNumber,
            CommercialRegNo = dto.CommercialRegNo,
            CreditLimit     = dto.CreditLimit,
            PaymentTermDays = dto.PaymentTermDays,
            Notes           = dto.Notes,
            IsActive        = true
        };

        private CustomerDto ToDto(Customer c)
        {
            var isOverLimit = c.CreditLimit > 0 && c.Balance > c.CreditLimit;
            var hasTransactions = !string.IsNullOrWhiteSpace(c.AccountCode) && JournalRepository.HasLinesForAccount(c.AccountCode);
            var accountName = string.IsNullOrWhiteSpace(c.AccountCode) ? null : AccountRepository.GetByCode(c.AccountCode)?.Name;

            var statusKey = !c.IsActive ? "Inactive" : (isOverLimit ? "OverLimit" : "Active");
            var variant = !c.IsActive ? StatusVariant.Neutral : (isOverLimit ? StatusVariant.Danger : StatusVariant.Success);

            return new CustomerDto
            {
                Id                = c.Id,
                Code              = c.Code,
                Name              = c.Name,
                NameEn            = c.NameEn,
                Phone             = c.Phone,
                Phone2            = c.Phone2,
                Email             = c.Email,
                Address           = c.Address,
                City              = c.City,
                TaxNumber         = c.TaxNumber,
                CommercialRegNo   = c.CommercialRegNo,
                AccountCode       = c.AccountCode,
                AccountName       = accountName,
                Balance           = c.Balance,
                CreditLimit       = c.CreditLimit,
                IsOverCreditLimit = isOverLimit,
                AvailableCredit   = c.CreditLimit - c.Balance,
                PaymentTermDays   = c.PaymentTermDays,
                IsActive          = c.IsActive,
                Notes             = c.Notes,
                StatusVariant     = variant,
                StatusText        = LocalizationService.Get($"Str.Customer.Status.{statusKey}"),
                CanEdit           = _permissions.Can(PermissionKeys.Customers.Edit),
                CanDelete         = _permissions.Can(PermissionKeys.Customers.Delete) && !hasTransactions,
                HasTransactions   = hasTransactions
            };
        }
    }
}
