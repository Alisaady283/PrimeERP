using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Common;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Inventory
{
    public class WarehouseService : ServiceBase, IWarehouseService
    {
        protected override string PermissionPrefix => "Warehouses";
        protected override string StringPrefix => "Str.Warehouse";
        protected override string EntityName => "Warehouses";

        private readonly IWarehouseRepository _repo;
        private readonly INumberSequenceService _numbers;

        public WarehouseService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IWarehouseRepository repo, INumberSequenceService numbers) : base(permissions, settings, localization, audit)
        {
            _repo = repo;
            _numbers = numbers;
        }

        public Result<List<WarehouseDto>> GetAll(bool includeInactive = false) =>
            Result.Ok(_repo.GetAll(includeInactive).Select(ToDto).ToList());

        public Result<WarehouseDto> Create(CreateWarehouseDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return Result.Fail<WarehouseDto>("اسم المخزن مطلوب", ErrorCode.ValidationFailed);

            var warehouse = new Warehouse { Code = _numbers.Next("Warehouse"), Name = dto.Name, Location = dto.Location, ManagerName = dto.ManagerName, IsActive = dto.IsActive };
            var id = _repo.Insert(warehouse);
            warehouse.Id = id;

            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { warehouse.Code, warehouse.Name });
            return Result.Ok(ToDto(warehouse));
        }

        public Result Update(UpdateWarehouseDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return Result.Fail("اسم المخزن مطلوب", ErrorCode.ValidationFailed);

            var warehouse = _repo.GetById(dto.Id);
            if (warehouse == null) return Result.Fail("المخزن غير موجود", ErrorCode.NotFound);

            warehouse.Name = dto.Name; warehouse.Location = dto.Location; warehouse.ManagerName = dto.ManagerName; warehouse.IsActive = dto.IsActive;
            _repo.Update(warehouse);
            Audit.Log(EntityName, warehouse.Id, AuditAction.Update, newValue: new { warehouse.Name });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            var warehouse = _repo.GetById(id);
            if (warehouse == null) return Result.Fail("المخزن غير موجود", ErrorCode.NotFound);

            _repo.Delete(id);
            Audit.Log(EntityName, id, AuditAction.Delete, details: warehouse.Code);
            return Result.Ok();
        }

        private static WarehouseDto ToDto(Warehouse w) => new()
        { Id = w.Id, Code = w.Code, Name = w.Name, Location = w.Location, ManagerName = w.ManagerName, IsActive = w.IsActive };
    }
}
