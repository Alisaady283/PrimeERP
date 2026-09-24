using PrimeERP.Data.Core;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.HR
{
    /// <summary>خدمة الموظفين</summary>
    public class EmployeeService : CrudServiceBase<Employee, EmployeeDto, EmployeeFilter>, IEmployeeService, IAccountLinkedService
    {
        protected override string PermissionPrefix => "HR";
        protected override string StringPrefix => "Str.Employee";
        protected override string EntityName => "Employees";

        private readonly IEmployeeRepository _employees;
        private readonly ILookupRepository<Department> _departments;
        private readonly ILookupRepository<JobTitle> _jobTitles;
        private readonly INumberSequenceService _numbers;
        private readonly Accounting.IAccountService _accounts;

        public EmployeeService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IEmployeeRepository employees, ILookupRepository<Department> departments, ILookupRepository<JobTitle> jobTitles,
            INumberSequenceService numbers, Accounting.IAccountService accounts)
            : base(permissions, settings, localization, audit)
        {
            _employees = employees;
            _departments = departments;
            _jobTitles = jobTitles;
            _numbers = numbers;
            _accounts = accounts;
        }

        protected override Employee FindById(int id) => _employees.GetById(id);

        protected override (List<Employee> Items, int Total) FindPaged(int page, int pageSize, EmployeeFilter filter)
        {
            filter ??= new EmployeeFilter();
            return _employees.GetPaged(page, pageSize, filter.SearchText, filter.DepartmentId, filter.SortBy, filter.SortDescending);
        }

        protected override List<Employee> FindSearch(string term, int maxResults) => _employees.Search(term, maxResults);

        public Result<EmployeeDto> Create(CreateEmployeeDto dto)
        {
            if (!Can("Create")) return FailDenied<EmployeeDto>();

            var employee = new Employee
            {
                Code = _numbers.Next("Employee"), Name = dto.Name, DepartmentId = dto.DepartmentId, JobTitleId = dto.JobTitleId,
                Phone = dto.Phone, Email = dto.Email, HireDate = dto.HireDate, BasicSalary = dto.BasicSalary, Notes = dto.Notes,
                Status = dto.IsActive ? EmployeeStatus.Active : EmployeeStatus.Inactive, CreatedBy = CurrentUser
            };

            var check = Check(new EmployeeValidator(), employee);
            if (check.IsFailure) return check.As<EmployeeDto>();

            var id = Tx(db =>
            {
                employee.AccountCode = CreateAdvanceAccount(db, employee.Name);
                return _employees.Insert(employee, db);
            });
            employee.Id = id;

            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { employee.Code, employee.Name });
            return Result.Ok(ToDto(employee));
        }

        private string CreateAdvanceAccount(PrimeDbContext db, string name)
        {
            var rootCode = Settings.Get(SettingKeys.Accounts.EmployeeAdvances, "");
            if (string.IsNullOrWhiteSpace(rootCode)) return null;

            var root = _accounts.GetByCode(rootCode);
            if (!root.IsSuccess) return null;

            var created = _accounts.Create(db, new CreateAccountDto
            { ParentId = root.Value.Id, Name = name, IsLeaf = true, SkipAutoLink = true });

            return created.IsSuccess ? created.Value.Code : null;
        }

        public Result Update(UpdateEmployeeDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var employee = _employees.GetById(dto.Id);
            if (employee == null) return Fail("NotFound", ErrorCode.NotFound);

            var nameChanged = employee.Name != dto.Name;   // قبل الاستبدال أدناه

            employee.Name = dto.Name; employee.DepartmentId = dto.DepartmentId; employee.JobTitleId = dto.JobTitleId;
            employee.Phone = dto.Phone; employee.Email = dto.Email; employee.HireDate = dto.HireDate; employee.BasicSalary = dto.BasicSalary;
            employee.Notes = dto.Notes; employee.Status = dto.IsActive ? EmployeeStatus.Active : EmployeeStatus.Inactive;
            employee.UpdatedBy = CurrentUser;

            var check = Check(new EmployeeValidator(), employee);
            if (check.IsFailure) return check;

            Tx(db =>
            {
                _employees.Update(employee, db);
                if (nameChanged && !string.IsNullOrWhiteSpace(employee.AccountCode))
                    _accounts.UpdateName(db, employee.AccountCode, employee.Name);
            });

            Audit.Log(EntityName, employee.Id, AuditAction.Update, newValue: new { employee.Name });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var employee = _employees.GetById(id);
            if (employee == null) return Fail("NotFound", ErrorCode.NotFound);

            Tx(db =>
            {
                if (!string.IsNullOrWhiteSpace(employee.AccountCode))
                    _accounts.Delete(db, employee.AccountCode);
                _employees.Delete(id, CurrentUser, db);
            });

            Audit.Log(EntityName, id, AuditAction.Delete, details: employee.Code);
            return Result.Ok();
        }


        Result IAccountLinkedService.CreateFromAccount(PrimeDbContext db, string accountCode, string name, string rootCode)
        {
            var employee = new Employee
            {
                Code = _numbers.Next(db, "Employee"), Name = name, AccountCode = accountCode,
                HireDate = System.DateTime.Today, Status = EmployeeStatus.Active, CreatedBy = CurrentUser
            };

            _employees.Insert(employee, db);
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
            if (employee == null) return Result.Ok();

            _employees.Delete(employee.Id, CurrentUser, db);
            return Result.Ok();
        }

        protected override EmployeeDto ToDto(Employee e)
        {
            var isActive = e.Status == EmployeeStatus.Active;
            var (variant, statusKey) = (isActive ? StatusVariant.Success : StatusVariant.Danger, isActive ? "Active" : "Inactive");
            return new EmployeeDto
            {
                Id = e.Id, Code = e.Code, Name = e.Name, AccountCode = e.AccountCode,
                DepartmentId = e.DepartmentId, DepartmentName = e.DepartmentId != null ? _departments.GetById(e.DepartmentId.Value)?.Name : null,
                JobTitleId = e.JobTitleId, JobTitleName = e.JobTitleId != null ? _jobTitles.GetById(e.JobTitleId.Value)?.Name : null,
                Phone = e.Phone, Email = e.Email, HireDate = e.HireDate, BasicSalary = e.BasicSalary, Notes = e.Notes,
                IsActive = isActive, StatusVariant = variant, StatusText = LocalizationService.Get($"Str.{statusKey}"),
                CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
                CanEdit = Can("Edit"), CanDelete = Can("Delete")
            };
        }
    }
}
