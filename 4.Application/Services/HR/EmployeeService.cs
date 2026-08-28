using System.Collections.Generic;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.HR
{
    // بنفس بنية ProductService — القسم/الوظيفة فئتا Category بـModuleKey مختلف (Departments/JobTitles)، لا
    // جدولان جديدان. IsActive في الحوار/الشبكة مبسَّطة عمداً فوق Employee.Status (Active/Inactive فقط،
    // OnLeave غير مكشوف بعد — قابل للتطوير لاحقاً بلا توسيع FieldKind الآن).
    public class EmployeeService : CrudServiceBase<Employee, EmployeeDto, EmployeeFilter>, IEmployeeService
    {
        protected override string PermissionPrefix => "HR";
        protected override string StringPrefix => "Str.Employee";
        protected override string EntityName => "Employees";

        private readonly IEmployeeRepository _employees;
        private readonly ICategoryRepository _categories;
        private readonly INumberSequenceService _numbers;

        public EmployeeService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IEmployeeRepository employees, ICategoryRepository categories, INumberSequenceService numbers)
            : base(permissions, settings, localization, audit)
        {
            _employees = employees;
            _categories = categories;
            _numbers = numbers;
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

            var validation = new EmployeeValidator().Validate(employee);
            if (!validation.IsValid) return Result.Fail<EmployeeDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            var id = _employees.Insert(employee);
            employee.Id = id;

            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { employee.Code, employee.Name });
            return Result.Ok(ToDto(employee));
        }

        public Result Update(UpdateEmployeeDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var employee = _employees.GetById(dto.Id);
            if (employee == null) return Fail("NotFound", ErrorCode.NotFound);

            employee.Name = dto.Name; employee.DepartmentId = dto.DepartmentId; employee.JobTitleId = dto.JobTitleId;
            employee.Phone = dto.Phone; employee.Email = dto.Email; employee.HireDate = dto.HireDate; employee.BasicSalary = dto.BasicSalary;
            employee.Notes = dto.Notes; employee.Status = dto.IsActive ? EmployeeStatus.Active : EmployeeStatus.Inactive;
            employee.UpdatedBy = CurrentUser;

            var validation = new EmployeeValidator().Validate(employee);
            if (!validation.IsValid) return Result.Fail(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            _employees.Update(employee);
            Audit.Log(EntityName, employee.Id, AuditAction.Update, newValue: new { employee.Name });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var employee = _employees.GetById(id);
            if (employee == null) return Fail("NotFound", ErrorCode.NotFound);

            _employees.Delete(id, CurrentUser);
            Audit.Log(EntityName, id, AuditAction.Delete, details: employee.Code);
            return Result.Ok();
        }

        protected override EmployeeDto ToDto(Employee e)
        {
            var isActive = e.Status == EmployeeStatus.Active;
            var (variant, statusKey) = (isActive ? StatusVariant.Success : StatusVariant.Danger, isActive ? "Active" : "Inactive");
            return new EmployeeDto
            {
                Id = e.Id, Code = e.Code, Name = e.Name,
                DepartmentId = e.DepartmentId, DepartmentName = e.DepartmentId != null ? _categories.GetById(e.DepartmentId.Value)?.Name : null,
                JobTitleId = e.JobTitleId, JobTitleName = e.JobTitleId != null ? _categories.GetById(e.JobTitleId.Value)?.Name : null,
                Phone = e.Phone, Email = e.Email, HireDate = e.HireDate, BasicSalary = e.BasicSalary, Notes = e.Notes,
                IsActive = isActive, StatusVariant = variant, StatusText = LocalizationService.Get($"Str.{statusKey}"),
                CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
                CanEdit = Can("Edit"), CanDelete = Can("Delete")
            };
        }
    }
}
