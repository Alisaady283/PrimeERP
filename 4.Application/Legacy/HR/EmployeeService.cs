using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Legacy.Common;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.HR
{
    /// <summary>خدمة الموظفين</summary>
    public class EmployeeService : EntityService<Employee, EmployeeDto, CreateEmployeeDto, UpdateEmployeeDto, EmployeeFilter>,
        IEmployeeService, IAccountLinkedService
    {
        protected override string PermissionPrefix => "HR";
        protected override string StringPrefix => "Str.Employee";
        protected override string EntityName => "Employees";
        protected override string SequenceKey => "Employee";

        public static readonly Field<Employee>[] Rules =
        {
            new(x => x.Code, "Str.Field.EmployeeCode", Required: true),
            new(x => x.Name, "Str.Field.EmployeeName", Required: true, Max: 150),
            new(x => x.Phone, "Str.Field.Phone", Format: FieldFormat.Phone),
            new(x => x.Email, "Str.Email", Format: FieldFormat.Email),
            new(x => x.BasicSalary, "Str.BasicSalary", From: 0),
            new(x => x.HireDate, "Str.HireDate", Format: FieldFormat.Date),
        };

        protected override Field<Employee>[] Fields => Rules;

        private readonly IEmployeeRepository _employees;
        private readonly AccountSpec<Employee>[] _advance;

        public EmployeeService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IEmployeeRepository employees, INumberSequenceService numbers, AccountCases tree)
            : base(permissions, settings, localization, audit, numbers, tree)
        {
            _employees = employees;
            _advance = new[]
            {
                new AccountSpec<Employee>(AdvanceRoot, e => e.Name, e => e.AccountCode, (e, code) => e.AccountCode = code, Optional: true)
            };
        }

        protected override IReadOnlyList<AccountSpec<Employee>> Accounts => _advance;

        private Result<Account> AdvanceRoot(PrimeDbContext db, Employee _) =>
            Tree.Root(db, Setting(SettingKeys.Accounts.EmployeeAdvances, ""), "Str.Employee.NotFound");

        protected override Employee FindById(int id) => _employees.GetById(id);

        protected override (List<Employee> Items, int Total) FindPaged(int page, int pageSize, EmployeeFilter filter)
        {
            filter ??= new EmployeeFilter();
            return _employees.GetPaged(page, pageSize, filter.SearchText, filter.DepartmentId, filter.SortBy, filter.SortDescending);
        }

        protected override List<Employee> FindSearch(string term, int maxResults) => _employees.Search(term, maxResults);

        protected override Employee New(CreateEmployeeDto dto) => Rows.Copy<Employee>(dto, new(), to =>
        {
            to.Status = dto.IsActive ? EmployeeStatus.Active : EmployeeStatus.Inactive;
        });

        protected override void Number(Employee e, string code) => e.Code = code;

        protected override void Apply(Employee e, UpdateEmployeeDto dto)
        {
            Rows.Copy(dto, e, x => x.Status = dto.IsActive ? EmployeeStatus.Active : EmployeeStatus.Inactive);
        }

        protected override int IdOf(UpdateEmployeeDto dto) => dto.Id;
        protected override int Insert(PrimeDbContext db, Employee e) => _employees.Insert(e, db);
        protected override void Save(PrimeDbContext db, Employee e) => _employees.Update(e, db);
        protected override void Erase(PrimeDbContext db, Employee e) => _employees.Delete(e.Id, CurrentUser, db);

        protected override object AuditValue(Employee e) => new { e.Code, e.Name };
        protected override string DeleteDetails(Employee e) => e.Code;

        public string[] RootKeys => new[] { SettingKeys.Accounts.EmployeeAdvances };

        Result IAccountLinkedService.CreateFromAccount(PrimeDbContext db, string accountCode, string name, string rootCode)
        {
            _employees.Insert(new Employee
            {
                Code = Numbers.Next(db, "Employee"), Name = name, AccountCode = accountCode,
                HireDate = System.DateTime.Today, Status = EmployeeStatus.Active
            }, db);
            return Result.Ok();
        }

        public Result UpdateNameFromAccount(PrimeDbContext db, string accountCode, string name)
        {
            _employees.UpdateNameByAccountCode(db, accountCode, name);
            return Result.Ok();
        }

        public Result DeleteByAccountCode(PrimeDbContext db, string accountCode)
        {
            var employee = _employees.GetByAccountCode(accountCode, db);
            if (employee != null) _employees.Delete(employee.Id, CurrentUser, db);
            return Result.Ok();
        }

        protected override EmployeeDto ToDto(Employee e)
        {
            var isActive = e.Status == EmployeeStatus.Active;
            var (variant, status) = Rows.Active(isActive);
            return Rows.Copy(e, new EmployeeDto(), to =>
            {
                to.IsActive = isActive;
                to.StatusVariant = variant;
                to.StatusText = status;
                to.CanEdit = Can("Edit");
                to.CanDelete = Can("Delete");
            });
        }
    }
}
