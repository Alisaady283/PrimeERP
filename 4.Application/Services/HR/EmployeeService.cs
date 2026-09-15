using System.Collections.Generic;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using System.Data.Common;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Domain.Entities;
using Db = PrimeERP.Data.Core.DbHelper;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.HR
{
    // بنفس بنية ProductService. IsActive في الحوار/الشبكة مبسَّطة عمداً فوق Employee.Status (Active/Inactive
    // فقط، OnLeave غير مكشوف بعد — قابل للتطوير لاحقاً بلا توسيع FieldKind الآن).
    public class EmployeeService : CrudServiceBase<Employee, EmployeeDto, EmployeeFilter>, IEmployeeService, IAccountLinkedService
    {
        protected override string PermissionPrefix => "HR";
        protected override string StringPrefix => "Str.Employee";
        protected override string EntityName => "Employees";

        private readonly IEmployeeRepository _employees;
        private readonly IDepartmentRepository _departments;
        private readonly IJobTitleRepository _jobTitles;
        private readonly INumberSequenceService _numbers;
        private readonly Accounting.IAccountService _accounts;

        public EmployeeService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IEmployeeRepository employees, IDepartmentRepository departments, IJobTitleRepository jobTitles,
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

            var validation = new EmployeeValidator().Validate(employee);
            if (!validation.IsValid) return Result.Fail<EmployeeDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            // الموظف وحساب سلفته سجلٌّ واحد بوجهين، كالعميل: يُنشآن معاً أو لا يُنشأ أيّهما.
            var id = Db.RunTransaction((conn, tx) =>
            {
                employee.AccountCode = CreateAdvanceAccount(conn, tx, employee.Name);
                return _employees.Insert(employee, conn, tx);
            });
            employee.Id = id;

            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { employee.Code, employee.Name });
            return Result.Ok(ToDto(employee));
        }

        /// <summary>ورقةٌ باسم الموظف تحت جذر سلف الموظفين. جذرٌ غير مضبوط = بلا حساب، والموظف يُنشأ
        /// على أي حال — فربطُ الحسابات إعدادٌ لا شرطٌ لوجود الموظف.</summary>
        private string CreateAdvanceAccount(DbConnection conn, DbTransaction tx, string name)
        {
            var rootCode = Settings.Get(SettingKeys.Accounts.EmployeeAdvances, "");
            if (string.IsNullOrWhiteSpace(rootCode)) return null;

            var root = _accounts.GetByCode(rootCode);
            if (!root.IsSuccess) return null;

            // SkipAutoLink إلزامي: يمنع AccountService.Create من استدعاء CreateFromAccount ثانيةً (حلقة لا نهائية).
            var created = _accounts.Create(conn, tx, new CreateAccountDto
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

            var validation = new EmployeeValidator().Validate(employee);
            if (!validation.IsValid) return Result.Fail(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            // الاسم يتبع في الاتجاهين: تعديله هنا يُعدّل حسابه، وتعديله في الشجرة يُعدّله هنا
            // (UpdateNameFromAccount) — بلا استدعاءٍ عكسيّ من أيّهما، فلا حلقة.
            Db.RunTransaction((conn, tx) =>
            {
                _employees.Update(employee, conn, tx);
                if (nameChanged && !string.IsNullOrWhiteSpace(employee.AccountCode))
                    _accounts.UpdateName(conn, tx, employee.AccountCode, employee.Name);
            });

            Audit.Log(EntityName, employee.Id, AuditAction.Update, newValue: new { employee.Name });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var employee = _employees.GetById(id);
            if (employee == null) return Fail("NotFound", ErrorCode.NotFound);

            // الحساب يُحذف مع صاحبه. ورفضُ AccountService حذفَ حسابٍ عليه قيود يحمي السلفة القائمة:
            // موظفٌ له رصيد سلفة لا يُحذف بلا تصفيتها أوّلاً.
            Db.RunTransaction((conn, tx) =>
            {
                if (!string.IsNullOrWhiteSpace(employee.AccountCode))
                    _accounts.Delete(conn, tx, employee.AccountCode);
                _employees.Delete(id, CurrentUser, conn, tx);
            });

            Audit.Log(EntityName, id, AuditAction.Delete, details: employee.Code);
            return Result.Ok();
        }

        // ===================== الاتجاه المعاكس: من الشجرة إلى الموظف =====================

        /// <summary>حسابٌ أُنشئ تحت جذر السلف مباشرةً ينشئ موظفه — كما ينشئ حسابُ العميل عميلَه.</summary>
        Result IAccountLinkedService.CreateFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name, string rootCode)
        {
            var employee = new Employee
            {
                Code = _numbers.Next(conn, tx, "Employee"), Name = name, AccountCode = accountCode,
                HireDate = System.DateTime.Today, Status = EmployeeStatus.Active, CreatedBy = CurrentUser
            };

            _employees.Insert(employee, conn, tx);
            return Result.Ok();
        }

        /// <summary>تعديل اسم الحساب يتبعه اسم الموظف. بلا مزامنةٍ عكسية فلا حلقة.</summary>
        public Result UpdateNameFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name)
        {
            _employees.UpdateNameByAccountCode(conn, tx, accountCode, name);
            return Result.Ok();
        }

        /// <summary>حذف الحساب يحذف موظفه. لا سجلّ مرتبط = لا خطأ.</summary>
        public Result DeleteByAccountCode(DbConnection conn, DbTransaction tx, string accountCode)
        {
            var employee = _employees.GetByAccountCode(accountCode, conn, tx);
            if (employee == null) return Result.Ok();

            _employees.Delete(employee.Id, CurrentUser, conn, tx);
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
