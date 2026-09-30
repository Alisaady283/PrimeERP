using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.HR
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

    public abstract class EmployeeMovementService<T> : ServiceBase, IEmployeeMovementService
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
        protected override string StringPrefix => "Str.Employee";

        public Result<PagedResult<EmployeeMovementDto>> GetPaged(int page, int pageSize, EmployeeMovementFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<EmployeeMovementDto>>();
            filter ??= new EmployeeMovementFilter();

            var (items, total) = _repo.GetPaged(page, pageSize, filter.SearchText, filter.EmployeeId, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<EmployeeMovementDto>
            { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<EmployeeMovementDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<EmployeeMovementDto>();

            var item = _repo.GetById(id);
            return item == null
                ? Result.Fail<EmployeeMovementDto>(Localization.Get("Str.Common.RecordNotFound"), ErrorCode.NotFound)
                : Result.Ok(ToDto(item));
        }

        public Result<EmployeeMovementDto> Create(CreateEmployeeMovementDto dto)
        {
            if (!Can("Create")) return FailDenied<EmployeeMovementDto>();

            var check = Validate(dto);
            if (check.IsFailure) return Result.Fail<EmployeeMovementDto>(check.ErrorMessage, check.ErrorCode);

            var item = Rows.Copy(dto, new T(), to =>
            {
                to.EmployeeId = Resolve(dto.EmployeeCode).Id;
            });
            item.Id = _repo.Insert(item);

            Audit.Log(EntityName, item.Id, AuditAction.Insert, newValue: new { item.EmployeeId, item.Amount });
            return Result.Ok(ToDto(_repo.GetById(item.Id)));
        }

        public Result Update(CreateEmployeeMovementDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var item = _repo.GetById(dto.Id);
            if (item == null) return Result.Fail(Localization.Get("Str.Common.RecordNotFound"), ErrorCode.NotFound);

            var check = Validate(dto);
            if (check.IsFailure) return check;

            Rows.Copy(dto, item, i => i.EmployeeId = Resolve(dto.EmployeeCode).Id);

            _repo.Update(item);
            Audit.Log(EntityName, item.Id, AuditAction.Update, newValue: new { item.Amount });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            if (_repo.GetById(id) == null) return Result.Fail(Localization.Get("Str.Common.RecordNotFound"), ErrorCode.NotFound);

            _repo.Delete(id, CurrentUser);
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private Employee Resolve(string code) => _employees.GetByCode(code);

        private Result Validate(CreateEmployeeMovementDto dto)
        {
            return Check.Valid(dto,
                new Field<CreateEmployeeMovementDto>(x => x.EmployeeCode, "", Required: true, Message: "Str.Employee.Required"),
                new Field<CreateEmployeeMovementDto>(x => x.EmployeeCode, "", Must: d => string.IsNullOrWhiteSpace(d.EmployeeCode) || Resolve(d.EmployeeCode) != null,
                    Message: "Str.Employee.CodeNotFound", Args: d => new object[] { d.EmployeeCode }),
                new Field<CreateEmployeeMovementDto>(x => x.Amount, "", Must: d => d.Amount > 0, Message: "Str.Common.AmountPositive"),
                new Field<CreateEmployeeMovementDto>(x => x.Month, "", From: 1, To: 12, Message: "Str.Employee.MonthRange"),
                new Field<CreateEmployeeMovementDto>(x => x.Year, "", From: 2000, Message: "Str.Employee.YearInvalid"));
        }

        private static EmployeeMovementDto ToDto(T item) => Rows.Copy<EmployeeMovementDto>(item, new());
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
}
