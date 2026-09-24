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
using PrimeERP.Domain.Contracts;
using PrimeERP.Platform.Settings;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Rules;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Platform.Audit;
using AuditAction = PrimeERP.Domain.Enums.AuditAction;

namespace PrimeERP.Application.Services.Parties
{
    /// <summary>المالك الوحيد لمنطق العملاء</summary>
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
        protected override decimal CreditLimitOf(Customer entity) => entity.CreditLimit;
        protected override PrimeERP.Application.Services.Cheques.IChequeService Cheques => _cheques;

        protected override decimal BalanceOf(Customer entity) => entity.Balance;

        protected override Customer BuildFromAccount(string code, string name, string accountCode) => new()
        {
            Code        = code,
            Name        = name,
            AccountCode = accountCode,
            IsActive    = true,
            CreatedBy   = CurrentUser
        };

        private readonly IPartyRepository<Customer> _customers;
        private readonly IJournalRepository _journalRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly ICategoryRepository _categories;
        private readonly PrimeERP.Application.Services.Cheques.IChequeService _cheques;

        public CustomerService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            IAccountService accounts, INumberSequenceService numbers, IPartyRepository<Customer> customers, IAccountRepository accountRepo,
            IJournalRepository journalRepo, ICategoryRepository categories, PrimeERP.Application.Services.Cheques.IChequeService cheques)
            : base(permissions, settings, localization, audit, accounts, numbers, accountRepo)
        {
            _customers = customers;
            _accountRepo = accountRepo;
            _journalRepo = journalRepo;
            _categories = categories;
            _cheques = cheques;
        }


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



        public Result<CustomerDto> Create(CreateCustomerDto dto)
        {
            if (!Can("Create")) return FailDenied<CustomerDto>();

            var parent = GetParentAccount();
            if (!parent.IsSuccess) return Result.Fail<CustomerDto>(parent.ErrorMessage, parent.ErrorCode);

            var code = Numbers.Next(SequenceKey);
            var customer = BuildNewCustomer(dto, code);

            var check = Check(Validator, customer);
            if (check.IsFailure) return check.As<CustomerDto>();

            var nameIsDuplicate = Setting(SettingKeys.Financial.WarnOnDuplicateCustomerName, true) && _customers.ExistsName(customer.Name);
            var phoneIsDuplicate = Setting(SettingKeys.Financial.WarnOnDuplicatePhone, true)
                && !string.IsNullOrWhiteSpace(customer.Phone) && _customers.ExistsPhone(customer.Phone);

            int newId;
            try
            {
                newId = Tx(db =>
                {
                    var accountResult = Accounts.Create(db, new CreateAccountDto
                    {
                        ParentId = parent.Value.Id,
                        Name = customer.Name,
                        IsLeaf = true,
                        SkipAutoLink = true // ⚠️ required: prevents an endless link loop
                    });

                    if (!accountResult.IsSuccess)
                        throw new InvalidOperationException(accountResult.ErrorMessage);

                    customer.AccountCode = accountResult.Value.Code;
                    customer.CreatedBy = CurrentUser;

                    return _customers.Insert(customer, db);
                });
            }
            catch (Exception ex)
            {
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

        public Result<CustomerDto> Create(PrimeDbContext db, CreateCustomerDto dto)
        {
            var parent = GetParentAccount(db);
            if (!parent.IsSuccess) return Result.Fail<CustomerDto>(parent.ErrorMessage, parent.ErrorCode);

            var code = Numbers.Next(db, SequenceKey);
            var customer = BuildNewCustomer(dto, code);

            var check = Check(Validator, customer);
            if (check.IsFailure) return check.As<CustomerDto>();

            var link = CreateLinkedAccount(db, parent.Value.Id, customer.Name);
            if (!link.IsSuccess)
                return Result.Fail<CustomerDto>(link.ErrorMessage, link.ErrorCode);

            customer.AccountCode = link.Value;
            customer.CreatedBy = CurrentUser;

            var newId = _customers.Insert(customer, db);
            customer.Id = newId;

            return Result.Ok(ToDto(customer));
        }



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
            customer.CategoryId      = dto.CategoryId;
            customer.UpdatedBy       = CurrentUser;

            var check = Check(Validator, customer);
            if (check.IsFailure) return check;

            Tx(db =>
            {
                _customers.Update(customer, db);

                if (nameChanged && !string.IsNullOrWhiteSpace(customer.AccountCode))
                    Accounts.UpdateName(db, customer.AccountCode, customer.Name);
            });

            Audit.Log(EntityName, customer.Id, AuditAction.Update, newValue: new { customer.Name });
            return Result.Ok();
        }



        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var customer = _customers.GetById(id);
            if (customer == null)
                return Fail("NotFound", ErrorCode.NotFound);

            if (!string.IsNullOrWhiteSpace(customer.AccountCode) && _journalRepo.HasLinesForAccount(customer.AccountCode))
                return Fail("HasTransactions", ErrorCode.ValidationFailed);

            Tx(db =>
            {
                _customers.Delete(id, CurrentUser, db);
                if (!string.IsNullOrWhiteSpace(customer.AccountCode))
                    Accounts.Delete(db, customer.AccountCode);
            });

            Audit.Log(EntityName, id, AuditAction.Delete, details: customer.Code);
            return Result.Ok();
        }





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
            CategoryId      = dto.CategoryId,
            IsActive        = dto.IsActive
        };

        protected override CustomerDto ToDto(Customer c)
        {
            var isOverLimit = PartyRules.IsOverCreditLimit(c.Balance, c.CreditLimit);
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
                CategoryId        = c.CategoryId,
                CategoryName      = c.CategoryName,
                CreatedAt         = c.CreatedAt,
                UpdatedAt         = c.UpdatedAt,
                StatusVariant     = variant,
                StatusText        = Msg($"Status.{statusKey}"),
                CanEdit           = Can("Edit"),
                CanDelete         = CanDeleteWith(hasTransactions),
                HasTransactions   = hasTransactions
            };
        }
    }
}
