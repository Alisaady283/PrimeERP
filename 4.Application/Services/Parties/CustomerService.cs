using PrimeERP.Platform.Localization;
using PrimeERP.Application.Services;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Contracts;
using PrimeERP.Platform.Settings;
using PrimeERP.Domain.Entities;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Platform.Audit;
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
    public class CustomerService : PartyServiceBase<Customer, CustomerDto, CustomerFilter>, ICustomerService
    {
        protected override string PermissionPrefix => "Customers";
        protected override string StringPrefix => "Str.Customer";
        protected override string EntityName => "Customers";

        protected override string AccountSettingKey => SettingKeys.Accounts.Customers;
        protected override string SequenceKey => "Customer";
        protected override IPartyRepository<Customer> Repository => _customers;
        protected override IValidator<Customer> Validator => new CustomerValidator();
        protected override string AccountCodeOf(Customer entity) => entity.AccountCode;

        protected override Customer BuildFromAccount(string code, string name, string accountCode) => new()
        {
            Code        = code,
            Name        = name,
            AccountCode = accountCode,
            IsActive    = true,
            CreatedBy   = CurrentUser
        };

        private readonly ICustomerRepository _customers;
        private readonly IJournalRepository _journalRepo;
        private readonly IAccountRepository _accountRepo;

        public CustomerService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            IAccountService accounts, INumberSequenceService numbers, ICustomerRepository customers, IAccountRepository accountRepo, IJournalRepository journalRepo)
            : base(permissions, settings, localization, audit, accounts, numbers, accountRepo)
        {
            _customers = customers;
            _accountRepo = accountRepo;
            _journalRepo = journalRepo;
        }

        // ===================== القراءة =====================
        // GetById/GetPaged/Search جاهزة من CrudServiceBase عبر FindById/FindPaged/FindSearch أدناه.

        protected override Customer FindById(int id) => _customers.GetById(id);

        protected override (List<Customer> Items, int Total) FindPaged(int page, int pageSize, CustomerFilter filter)
        {
            filter ??= new CustomerFilter();
            return _customers.GetPaged(page, pageSize, filter.SearchText, filter.IsActive, filter.HasBalance,
                filter.OverCreditLimit, filter.CategoryId, filter.SortBy, filter.SortDescending);
        }

        protected override List<Customer> FindSearch(string term, int maxResults) => _customers.Search(term, maxResults);

        public Result<CustomerDto> GetByCode(string code)
        {
            if (!Can("View")) return FailDenied<CustomerDto>();

            var customer = _customers.GetByCode(code);
            if (customer == null)
                return Fail<CustomerDto>("NotFound", ErrorCode.NotFound);

            return Ok(ToDto(customer));
        }

        // GetStatement جاهزة من PartyServiceBase.

        // ===================== الإنشاء =====================

        public Result<CustomerDto> Create(CreateCustomerDto dto)
        {
            if (!Can("Create")) return FailDenied<CustomerDto>();

            var parent = GetParentAccount();
            if (!parent.IsSuccess) return Result.Fail<CustomerDto>(parent.ErrorMessage, parent.ErrorCode);

            var code = Numbers.Next(SequenceKey);
            var customer = BuildNewCustomer(dto, code);

            var validation = Validator.Validate(customer);
            if (!validation.IsValid)
                return Result.Fail<CustomerDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            // تحذيرات لا تمنع — تُسجَّل في تفاصيل Audit فقط، لا Fail.
            var nameIsDuplicate = Setting(SettingKeys.Financial.WarnOnDuplicateCustomerName, true) && _customers.ExistsName(customer.Name);
            var phoneIsDuplicate = Setting(SettingKeys.Financial.WarnOnDuplicatePhone, true)
                && !string.IsNullOrWhiteSpace(customer.Phone) && _customers.ExistsPhone(customer.Phone);

            int newId;
            try
            {
                newId = Db.RunTransaction((conn, tx) =>
                {
                    var accountResult = Accounts.Create(conn, tx, new CreateAccountDto
                    {
                        ParentId = parent.Value.Id,
                        Name = customer.Name,
                        IsLeaf = true,
                        SkipAutoLink = true // ⚠️ إلزامي — يمنع AccountService.Create من استدعاء CreateFromAccount ثانية (حلقة لا نهائية)
                    });

                    if (!accountResult.IsSuccess)
                        throw new InvalidOperationException(accountResult.ErrorMessage);

                    customer.AccountCode = accountResult.Value.Code;
                    customer.CreatedBy = CurrentUser;

                    return _customers.Insert(customer, conn, tx);
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

            Audit.Log(EntityName, newId, AuditAction.Insert, newValue: new { customer.Code, customer.Name },
                details: string.IsNullOrEmpty(warnings) ? null : warnings);

            customer.Id = newId;
            return Result.Ok(ToDto(customer));
        }

        /// <summary>بمعاملة خارجية — يخدم مستندات F.4 (فاتورة تنشئ عميلاً جديداً ضمن معاملتها). بلا تحقق صلاحية (المستدعي تحقق صلاحيته الخاصة).</summary>
        public Result<CustomerDto> Create(DbConnection conn, DbTransaction tx, CreateCustomerDto dto)
        {
            var parent = GetParentAccount(conn, tx);
            if (!parent.IsSuccess) return Result.Fail<CustomerDto>(parent.ErrorMessage, parent.ErrorCode);

            var code = Numbers.Next(conn, tx, SequenceKey);
            var customer = BuildNewCustomer(dto, code);

            var validation = Validator.Validate(customer);
            if (!validation.IsValid)
                return Result.Fail<CustomerDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            var link = CreateLinkedAccount(conn, tx, parent.Value.Id, customer.Name);
            if (!link.IsSuccess)
                return Result.Fail<CustomerDto>(link.ErrorMessage, link.ErrorCode);

            customer.AccountCode = link.Value;
            customer.CreatedBy = CurrentUser;

            var newId = _customers.Insert(customer, conn, tx);
            customer.Id = newId;

            return Result.Ok(ToDto(customer));
        }

        // CreateFromAccount جاهزة من PartyServiceBase.

        // ===================== التعديل =====================

        public Result Update(UpdateCustomerDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var customer = _customers.GetById(dto.Id);
            if (customer == null)
                return Fail("NotFound", ErrorCode.NotFound);

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

            var validation = Validator.Validate(customer);
            if (!validation.IsValid)
                return Result.Fail(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            Db.RunTransaction((conn, tx) =>
            {
                _customers.Update(customer, conn, tx);

                // مزامنة اسم الحساب لو تغيّر الاسم — اتجاه واحد (عميل→حساب)؛ الاتجاه المعاكس (حساب→عميل) عبر
                // UpdateNameFromAccount أدناه لا يستدعي هذا مرة أخرى، فلا حلقة ping-pong.
                if (nameChanged && !string.IsNullOrWhiteSpace(customer.AccountCode))
                    Accounts.UpdateName(conn, tx, customer.AccountCode, customer.Name);
            });

            Audit.Log(EntityName, customer.Id, AuditAction.Update, newValue: new { customer.Name });
            return Result.Ok();
        }

        // UpdateNameFromAccount جاهزة من PartyServiceBase.

        // ===================== الحذف =====================

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var customer = _customers.GetById(id);
            if (customer == null)
                return Fail("NotFound", ErrorCode.NotFound);

            // TODO F.4: تحقق الفواتير (Sales/Purchase) — لا خدمة فواتير مبنية بعد، يُضاف فور بنائها في F.4.
            if (!string.IsNullOrWhiteSpace(customer.AccountCode) && _journalRepo.HasLinesForAccount(customer.AccountCode))
                return Fail("HasTransactions", ErrorCode.ValidationFailed);

            Db.RunTransaction((conn, tx) =>
            {
                _customers.Delete(id, CurrentUser, conn, tx);
                if (!string.IsNullOrWhiteSpace(customer.AccountCode))
                    Accounts.Delete(conn, tx, customer.AccountCode);
            });

            Audit.Log(EntityName, id, AuditAction.Delete, details: customer.Code);
            return Result.Ok();
        }

        // DeleteByAccountCode جاهزة من PartyServiceBase.

        // ===================== الأرصدة والائتمان =====================

        // RecalculateBalance جاهزة من PartyServiceBase.

        public Result RecalculateAllBalances()
        {
            if (!Can("Edit")) return FailDenied();

            var customers = _customers.GetAll(activeOnly: false).Where(c => !string.IsNullOrWhiteSpace(c.AccountCode)).ToList();

            // كل قراءات الرصيد قبل فتح المعاملة (IAccountService.GetBalanceAsOf غير آمنة داخل معاملة خارجية —
            // نفس سبب كل قراءة أخرى في هذه الجلسة، راجع AccountService.RecalculateBalance(conn,tx,...)).
            var balances = new List<(int Id, decimal Balance)>();
            foreach (var c in customers)
            {
                var balanceResult = Accounts.GetBalanceAsOf(c.AccountCode, DateTime.Today);
                if (balanceResult.IsSuccess)
                    balances.Add((c.Id, balanceResult.Value));
            }

            Db.RunTransaction((conn, tx) =>
            {
                foreach (var (id, balance) in balances)
                    _customers.SetBalance(id, balance, conn, tx);
            });

            return Result.Ok();
        }

        public Result<CreditCheckResult> CheckCreditLimit(int id, decimal additional)
        {
            if (!Can("View")) return FailDenied<CreditCheckResult>();

            var customer = _customers.GetById(id);
            if (customer == null)
                return Fail<CreditCheckResult>("NotFound", ErrorCode.NotFound);

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
                ? Result.Fail<CreditCheckResult>($"{Msg("CreditLimitExceeded")}: {exceededBy:N2}", ErrorCode.ValidationFailed)
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

        protected override CustomerDto ToDto(Customer c)
        {
            var isOverLimit = c.CreditLimit > 0 && c.Balance > c.CreditLimit;
            var hasTransactions = !string.IsNullOrWhiteSpace(c.AccountCode) && _journalRepo.HasLinesForAccount(c.AccountCode);
            var accountName = string.IsNullOrWhiteSpace(c.AccountCode) ? null : _accountRepo.GetByCode(c.AccountCode)?.Name;

            var (variant, statusKey) = ComputeStatus(c.IsActive, isOverLimit);

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
                StatusText        = Msg($"Status.{statusKey}"),
                CanEdit           = Can("Edit"),
                CanDelete         = CanDeleteWith(hasTransactions),
                HasTransactions   = hasTransactions
            };
        }
    }
}
