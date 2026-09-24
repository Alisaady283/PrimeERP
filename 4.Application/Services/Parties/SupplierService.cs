using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Rules;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Parties
{
    /// <summary>المالك الوحيد لمنطق الموردين</summary>
    public class SupplierService : PartyServiceBase<Supplier, SupplierDto, SupplierFilter>, ISupplierService
    {
        protected override string PermissionPrefix => "Suppliers";
        protected override string StringPrefix => "Str.Supplier";
        protected override string EntityName => "Suppliers";

        protected override string AccountSettingKey => SettingKeys.Accounts.Suppliers;
        protected override string SequenceKey => "Supplier";
        protected override IPartyRepository<Supplier> Repository => _suppliers;
        protected override IValidator<Supplier> Validator => new SupplierValidator();
        protected override string AccountCodeOf(Supplier entity) => entity.AccountCode;
        protected override decimal CreditLimitOf(Supplier entity) => entity.CreditLimit;
        protected override PrimeERP.Application.Services.Cheques.IChequeService Cheques => _cheques;

        protected override decimal BalanceOf(Supplier entity) => entity.Balance;

        protected override Supplier BuildFromAccount(string code, string name, string accountCode) => new()
        {
            Code = code, Name = name, AccountCode = accountCode, IsActive = true, CreatedBy = CurrentUser
        };

        private readonly IPartyRepository<Supplier> _suppliers;
        private readonly IJournalRepository _journalRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly ICategoryRepository _categories;
        private readonly PrimeERP.Application.Services.Cheques.IChequeService _cheques;

        public SupplierService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            IAccountService accounts, INumberSequenceService numbers, IPartyRepository<Supplier> suppliers, IAccountRepository accountRepo,
            IJournalRepository journalRepo, ICategoryRepository categories, PrimeERP.Application.Services.Cheques.IChequeService cheques)
            : base(permissions, settings, localization, audit, accounts, numbers, accountRepo)
        {
            _suppliers = suppliers;
            _accountRepo = accountRepo;
            _journalRepo = journalRepo;
            _categories = categories;
            _cheques = cheques;
        }

        protected override Supplier FindById(int id) => _suppliers.GetById(id);

        protected override (List<Supplier> Items, int Total) FindPaged(int page, int pageSize, SupplierFilter filter)
        {
            filter ??= new SupplierFilter();
            return _suppliers.GetPaged(page, pageSize, filter.SearchText, filter.IsActive, filter.HasBalance,
                filter.OverCreditLimit, filter.CategoryId, filter.SortBy, filter.SortDescending);
        }

        protected override List<Supplier> FindSearch(string term, int maxResults) => _suppliers.Search(term, maxResults);

        public Result<SupplierDto> GetByCode(string code)
        {
            if (!Can("View")) return FailDenied<SupplierDto>();

            var supplier = _suppliers.GetByCode(code);
            if (supplier == null) return Fail<SupplierDto>("NotFound", ErrorCode.NotFound);

            return Ok(ToDto(supplier));
        }

        public Result<SupplierDto> Create(CreateSupplierDto dto)
        {
            if (!Can("Create")) return FailDenied<SupplierDto>();

            var parent = GetParentAccount();
            if (!parent.IsSuccess) return Result.Fail<SupplierDto>(parent.ErrorMessage, parent.ErrorCode);

            var code = Numbers.Next(SequenceKey);
            var supplier = BuildNewSupplier(dto, code);

            var check = Check(Validator, supplier);
            if (check.IsFailure) return check.As<SupplierDto>();

            int newId;
            try
            {
                newId = Tx(db =>
                {
                    var link = CreateLinkedAccount(db, parent.Value.Id, supplier.Name);
                    if (!link.IsSuccess) throw new InvalidOperationException(link.ErrorMessage);

                    supplier.AccountCode = link.Value;
                    supplier.CreatedBy = CurrentUser;

                    return _suppliers.Insert(supplier, db);
                });
            }
            catch (Exception ex)
            {
                return Result.Fail<SupplierDto>(ex.Message);
            }

            Audit.Log(EntityName, newId, AuditAction.Insert, newValue: new { supplier.Code, supplier.Name });

            supplier.Id = newId;
            return Result.Ok(ToDto(supplier));
        }

        public Result<SupplierDto> Create(PrimeDbContext db, CreateSupplierDto dto)
        {
            var parent = GetParentAccount(db);
            if (!parent.IsSuccess) return Result.Fail<SupplierDto>(parent.ErrorMessage, parent.ErrorCode);

            var code = Numbers.Next(db, SequenceKey);
            var supplier = BuildNewSupplier(dto, code);

            var check = Check(Validator, supplier);
            if (check.IsFailure) return check.As<SupplierDto>();

            var link = CreateLinkedAccount(db, parent.Value.Id, supplier.Name);
            if (!link.IsSuccess) return Result.Fail<SupplierDto>(link.ErrorMessage, link.ErrorCode);

            supplier.AccountCode = link.Value;
            supplier.CreatedBy = CurrentUser;

            var newId = _suppliers.Insert(supplier, db);
            supplier.Id = newId;

            return Result.Ok(ToDto(supplier));
        }

        public Result Update(UpdateSupplierDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var supplier = _suppliers.GetById(dto.Id);
            if (supplier == null) return Fail("NotFound", ErrorCode.NotFound);

            var nameChanged = supplier.Name != dto.Name; // قبل الاستبدال أدناه

            supplier.Name = dto.Name; supplier.NameEn = dto.NameEn; supplier.Phone = dto.Phone; supplier.Phone2 = dto.Phone2;
            supplier.Email = dto.Email; supplier.Address = dto.Address; supplier.City = dto.City; supplier.Country = dto.Country;
            supplier.TaxNumber = dto.TaxNumber; supplier.CommercialRegNo = dto.CommercialRegNo; supplier.CreditLimit = dto.CreditLimit;
            supplier.PaymentTermDays = dto.PaymentTermDays; supplier.SupplierType = dto.SupplierType; supplier.Notes = dto.Notes;
            supplier.IsActive = dto.IsActive; supplier.CategoryId = dto.CategoryId; supplier.UpdatedBy = CurrentUser;

            var check = Check(Validator, supplier);
            if (check.IsFailure) return check;

            Tx(db =>
            {
                _suppliers.Update(supplier, db);

                if (nameChanged && !string.IsNullOrWhiteSpace(supplier.AccountCode))
                    Accounts.UpdateName(db, supplier.AccountCode, supplier.Name);
            });

            Audit.Log(EntityName, supplier.Id, AuditAction.Update, newValue: new { supplier.Name });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var supplier = _suppliers.GetById(id);
            if (supplier == null) return Fail("NotFound", ErrorCode.NotFound);

            if (!string.IsNullOrWhiteSpace(supplier.AccountCode) && _journalRepo.HasLinesForAccount(supplier.AccountCode))
                return Fail("HasTransactions", ErrorCode.ValidationFailed);

            Tx(db =>
            {
                _suppliers.Delete(id, CurrentUser, db);
                if (!string.IsNullOrWhiteSpace(supplier.AccountCode))
                    Accounts.Delete(db, supplier.AccountCode);
            });

            Audit.Log(EntityName, id, AuditAction.Delete, details: supplier.Code);
            return Result.Ok();
        }


        private static Supplier BuildNewSupplier(CreateSupplierDto dto, string code) => new()
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
            SupplierType    = dto.SupplierType,
            Notes           = dto.Notes,
            CategoryId      = dto.CategoryId,
            IsActive        = dto.IsActive
        };

        protected override SupplierDto ToDto(Supplier s)
        {
            var isOverLimit = PartyRules.IsOverCreditLimit(s.Balance, s.CreditLimit);
            var hasTransactions = !string.IsNullOrWhiteSpace(s.AccountCode) && _journalRepo.HasLinesForAccount(s.AccountCode);
            var accountName = string.IsNullOrWhiteSpace(s.AccountCode) ? null : _accountRepo.GetByCode(s.AccountCode)?.Name;

            var (variant, statusKey) = ComputeStatus(s.IsActive, isOverLimit);

            return new SupplierDto
            {
                Id                = s.Id,
                Code              = s.Code,
                Name              = s.Name,
                NameEn            = s.NameEn,
                Phone             = s.Phone,
                Phone2            = s.Phone2,
                Email             = s.Email,
                Address           = s.Address,
                City              = s.City,
                TaxNumber         = s.TaxNumber,
                CommercialRegNo   = s.CommercialRegNo,
                AccountCode       = s.AccountCode,
                AccountName       = accountName,
                Balance           = s.Balance,
                CreditLimit       = s.CreditLimit,
                IsOverCreditLimit = isOverLimit,
                AvailableCredit   = s.CreditLimit - s.Balance,
                PaymentTermDays   = s.PaymentTermDays,
                SupplierType      = s.SupplierType,
                IsActive          = s.IsActive,
                Notes             = s.Notes,
                CategoryId        = s.CategoryId,
                CategoryName      = s.CategoryName,
                CreatedAt         = s.CreatedAt,
                UpdatedAt         = s.UpdatedAt,
                StatusVariant     = variant,
                StatusText        = Msg($"Status.{statusKey}"),
                CanEdit           = Can("Edit"),
                CanDelete         = CanDeleteWith(hasTransactions),
                HasTransactions   = hasTransactions
            };
        }
    }
}
