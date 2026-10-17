using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Ledger.Accounts;
using System;
using PrimeERP.Domain.Calculations;
using PrimeERP.Data.Core;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.PageServices.HR
{
    /// <summary>البدل والخصم خدمةٌ واحدة بجدولين</summary>
    public interface IEmployeeMovementService
    {
        Result<PagedResult<EmployeeMovementDto>> GetPaged(int page, int pageSize, EmployeeMovementFilter filter = null);
        Result<EmployeeMovementDto> GetById(int id);
        Result<EmployeeMovementDto> Create(CreateEmployeeMovementDto dto);
        Result Update(CreateEmployeeMovementDto dto);
        Result Delete(int id);
    }

    public interface IAllowanceService : IEmployeeMovementService { }
    public interface IDeductionService : IEmployeeMovementService { }
    public interface IAdvanceService : IEmployeeMovementService { }

    public abstract class EmployeeMovementService<T>
        : EntityService<T, EmployeeMovementDto, CreateEmployeeMovementDto, CreateEmployeeMovementDto, EmployeeMovementFilter>, IEmployeeMovementService
        where T : EmployeeMovement, new()
    {
        private readonly IEmployeeMovementRepository<T> _repo;
        private readonly IEmployeeRepository _employees;

        protected EmployeeMovementService(IEmployeeMovementRepository<T> repo, IEmployeeRepository employees,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo; _employees = employees;
        }

        protected override string PermissionPrefix => "HR";
        protected override string StringPrefix => "Str.EmployeeMovement";

        private static readonly Field<T>[] MovementFields =
        [
            .. EmployeeCode.Rules<T>(),
            new(x => x.Amount, "Str.Amount", Positive: true),
        ];

        protected override Field<T>[] Fields => MovementFields;

        protected override T FindById(int id) => _repo.GetById(id);

        protected override (List<T> Items, int Total) FindPaged(int page, int pageSize, EmployeeMovementFilter filter)
        {
            filter ??= new EmployeeMovementFilter();
            return _repo.GetPaged(page, pageSize, filter.SearchText, filter.EmployeeId, filter.SortBy, filter.SortDescending);
        }

        protected override List<T> FindSearch(string term, int maxResults) =>
            _repo.GetPaged(1, maxResults, term, null, null, false).Items;

        protected override T New(CreateEmployeeMovementDto dto) => Rows.Copy(dto, new T(), to => Fill(to, dto));

        protected override void Apply(T entity, CreateEmployeeMovementDto dto) => Rows.Copy(dto, entity, to => Fill(to, dto));

        protected override int IdOf(CreateEmployeeMovementDto dto) => dto.Id;

        protected override int Insert(PrimeDbContext db, T item) => _repo.Insert(item, db);

        protected override void Save(PrimeDbContext db, T item) => _repo.Update(item, db);

        protected override void Erase(PrimeDbContext db, T item) => _repo.Delete(item.Id, CurrentUser, db);

        protected override object AuditValue(T item) => new { item.EmployeeId, item.Amount };

        private void Fill(T to, CreateEmployeeMovementDto dto)
        {
            to.EmployeeId = _employees.IdByCode(dto.EmployeeCode);
            var (from, _) = PayrollCalc.Cycle(dto.Date, PayrollSettings.Rules(Settings).StartDay);
            (to.Month, to.Year) = (from.Month, from.Year);
        }

        protected override EmployeeMovementDto ToDto(T item) => Rows.Copy<EmployeeMovementDto>(item, new());
    }

    public class AllowanceService : EmployeeMovementService<EmployeeAllowance>, IAllowanceService
    {
        public AllowanceService(IEmployeeAllowanceRepository repo, IEmployeeRepository employees,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(repo, employees, permissions, settings, localization, audit) { }

        protected override string EntityName => "EmployeeAllowances";
    }

    public class DeductionService : EmployeeMovementService<EmployeeDeduction>, IDeductionService
    {
        public DeductionService(IEmployeeDeductionRepository repo, IEmployeeRepository employees,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(repo, employees, permissions, settings, localization, audit) { }

        protected override string EntityName => "EmployeeDeductions";
    }

    public class AdvanceService : EmployeeMovementService<EmployeeAdvance>, IAdvanceService
    {
        private readonly IEmployeeAdvanceRepository _advances;
        private readonly IEmployeeRepository _employeeRows;
        private readonly Entries _journals;
        private readonly AccountOf _accountOf;

        public AdvanceService(IEmployeeAdvanceRepository repo, IEmployeeRepository employees, Entries journals, AccountOf accountOf,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(repo, employees, permissions, settings, localization, audit)
        {
            _advances = repo; _employeeRows = employees; _journals = journals; _accountOf = accountOf;
        }

        protected override string EntityName => "EmployeeAdvances";

        protected override int Insert(PrimeDbContext db, EmployeeAdvance item)
        {
            var id = base.Insert(db, item);
            if (Posted) _advances.SetEntry(db, id, Post(db, item));
            return id;
        }

        protected override void Save(PrimeDbContext db, EmployeeAdvance item)
        {
            Posting.Reverse(_journals, db, item.JournalEntryId);
            item.JournalEntryId = Posted ? Post(db, item) : null;
            base.Save(db, item);
        }

        protected override void Erase(PrimeDbContext db, EmployeeAdvance item)
        {
            Posting.Reverse(_journals, db, item.JournalEntryId);
            base.Erase(db, item);
        }

        private bool Posted => Setting(SettingKeys.Edition.AdvancesPosted, true);

        private int Post(PrimeDbContext db, EmployeeAdvance item)
        {
            var employee = _employeeRows.GetById(item.EmployeeId, db);
            var advance = AccountOf.Required(employee?.AccountCode, "Str.Payroll.AdvanceAccountMissing", employee?.Name);
            var treasury = _accountOf.Treasury(item.TreasuryId ?? 0, "Str.Advance.TreasuryMissing");
            var accounts = Result.Combine(advance, treasury);
            if (accounts.IsFailure) throw new InvalidOperationException(accounts.ErrorMessage);

            return Posting.Entry(_journals, db, item.Date, $"{Localization.Get("Str.Module.EmployeeAdvances")} — {employee.Name}", EntityName,
                advance.Value, treasury.Value, item.Amount);
        }
    }
}
