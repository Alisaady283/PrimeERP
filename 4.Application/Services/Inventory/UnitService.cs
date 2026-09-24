using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Inventory
{
    /// <summary>خدمة الوحدات</summary>
    public class UnitService : ServiceBase, IUnitService
    {
        protected override string PermissionPrefix => "Units";
        protected override string StringPrefix => "Str.Unit";
        protected override string EntityName => "Units";

        private readonly ILookupRepository<Unit> _repo;

        public UnitService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, ILookupRepository<Unit> repo) : base(permissions, settings, localization, audit) => _repo = repo;

        public Result<List<UnitDto>> GetAll(bool includeInactive = false) =>
            Result.Ok(_repo.GetAll(includeInactive).Select(ToDto).ToList());

        public Result<UnitDto> Create(CreateUnitDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return Result.Fail<UnitDto>("اسم الوحدة مطلوب", ErrorCode.ValidationFailed);

            var unit = new Unit { Name = dto.Name, NameEn = dto.NameEn, Symbol = dto.Symbol, IsActive = dto.IsActive };
            var id = _repo.Insert(unit);
            unit.Id = id;

            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { unit.Name });
            return Result.Ok(ToDto(unit));
        }

        public Result Update(UpdateUnitDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return Result.Fail("اسم الوحدة مطلوب", ErrorCode.ValidationFailed);

            var unit = _repo.GetById(dto.Id);
            if (unit == null) return Result.Fail("الوحدة غير موجودة", ErrorCode.NotFound);

            unit.Name = dto.Name; unit.NameEn = dto.NameEn; unit.Symbol = dto.Symbol; unit.IsActive = dto.IsActive;
            _repo.Update(unit);
            Audit.Log(EntityName, unit.Id, AuditAction.Update, newValue: new { unit.Name });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            var unit = _repo.GetById(id);
            if (unit == null) return Result.Fail("الوحدة غير موجودة", ErrorCode.NotFound);

            _repo.Delete(id);
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private static UnitDto ToDto(Unit u) => new() { Id = u.Id, Name = u.Name, NameEn = u.NameEn, Symbol = u.Symbol, IsActive = u.IsActive };
    }
}
