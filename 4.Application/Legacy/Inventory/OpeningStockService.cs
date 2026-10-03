using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Application.Services.Ledger;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Inventory
{
    /// <summary>رصيد أول المدة للأصناف</summary>
    public interface IOpeningStockService
    {
        Result<CreateOpeningStockDto> GetById(int id);
        Result<CreateOpeningStockDto> Create(CreateOpeningStockDto dto);
        Result Update(CreateOpeningStockDto dto);
        Result Delete(int id);
    }

    public class OpeningStockService : ServiceBase, IOpeningStockService, IPermissionGated
    {
        public const string SourceKey = "OpeningStock";

        private readonly IStockMove _stock;
        private readonly IStockMovementRepository _movements;
        private readonly Entries _journals;
        private readonly IJournalRepository _ledger;
        private readonly ILookupRepository<Warehouse> _warehouses;
        private readonly AccountOf _accountsOf;

        public OpeningStockService(IStockMove stock, IStockMovementRepository movements, Entries journals, IJournalRepository ledger,
            ILookupRepository<Warehouse> warehouses, AccountOf accountsOf, IPermissionService permissions,
            ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _stock = stock; _movements = movements; _journals = journals; _ledger = ledger; _warehouses = warehouses; _accountsOf = accountsOf;
        }

        protected override string PermissionPrefix => "Inventory";
        protected override string StringPrefix => "Str.Stock";
        protected override string EntityName => SourceKey;

        public string PermissionKey => PermissionKeys.Inventory.OpeningStock;

        public Result<CreateOpeningStockDto> GetById(int id)
        {
            if (!Permissions.Can(PermissionKey)) return FailDenied<CreateOpeningStockDto>();

            var moves = Owned(id) ? _movements.GetBySource(SourceKey, id) : new List<StockMovement>();
            if (moves.Count == 0) return Fail<CreateOpeningStockDto>("NotFound", ErrorCode.NotFound);

            return Ok(new CreateOpeningStockDto
            {
                Id = id, Date = moves[0].MovementDate, WarehouseId = moves[0].WarehouseId,
                Lines = moves.Select((m, i) => Rows.Copy(m, new CreateOpeningStockLineDto(), to =>
                {
                    to.LineNo = i + 1;
                    to.Value = m.TotalCost;
                })).ToList()
            });
        }

        public Result<CreateOpeningStockDto> Create(CreateOpeningStockDto dto)
        {
            if (!Permissions.Can(PermissionKey)) return FailDenied<CreateOpeningStockDto>();

            dto.Id = 0;
            var created = Plan(dto).Then(write => Commit(write));
            if (created.IsFailure) return created.As<CreateOpeningStockDto>();

            Audit.Log(EntityName, dto.Id, AuditAction.Insert, newValue: new { dto.WarehouseId, dto.Lines.Count });
            return Ok(dto);
        }

        public Result Update(CreateOpeningStockDto dto)
        {
            if (!Permissions.Can(PermissionKey)) return FailDenied();
            if (!Owned(dto.Id)) return Fail("NotFound", ErrorCode.NotFound);

            var replaced = Plan(dto).Then(write => Commit(write));
            if (replaced.IsFailure) return replaced;

            Audit.Log(EntityName, dto.Id, AuditAction.Update, newValue: new { dto.WarehouseId, dto.Lines.Count });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Permissions.Can(PermissionKey)) return FailDenied();
            if (!Owned(id)) return Fail("NotFound", ErrorCode.NotFound);

            var removed = Posting.EnsureReversible(_journals, id)
                .Then(() => _stock.StaysPositive(_stock.Effects(SourceKey, id, -1)))
                .Then(() => Commit(db =>
                {
                    Remove(db, id);
                    return Result.Ok();
                }));
            if (removed.IsFailure) return removed;

            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        /// <summary>التحقق ثم دالة الكتابة</summary>
        private Result<Func<PrimeDbContext, Result>> Plan(CreateOpeningStockDto dto)
        {
            dto.Date = Setting(SettingKeys.Company.StartDate, dto.Date);
            var lines = (dto.Lines ?? new()).Where(l => l.ProductId > 0).ToList();

            var input = Check.Valid(dto, new Field<CreateOpeningStockDto>(x => x.WarehouseId, "", Required: true, Message: "Str.Stock.WarehouseRequired"))
                .Then(() => DocumentLines.Check(lines, l => l.Qty, "Str.Stock.NoProducts", l => l.UnitCost));
            if (input.IsFailure) return input.As<Func<PrimeDbContext, Result>>();

            var warehouse = _warehouses.NamesOf(new[] { dto.WarehouseId }).GetValueOrDefault(dto.WarehouseId);
            if (_movements.AnyInWarehouse(SourceKey, dto.WarehouseId, dto.Id))
                return Fail<Func<PrimeDbContext, Result>>("OpeningExists", ErrorCode.ValidationFailed, warehouse);

            var inventory = _accountsOf.Setting(SettingKeys.Accounts.Inventory, "Str.Trade.InventoryMissing");
            if (inventory.IsFailure) return inventory.As<Func<PrimeDbContext, Result>>();
            var counter = _accountsOf.Setting(SettingKeys.Accounts.OpeningAdjustments, "Str.Stock.OpeningAccountMissing");
            if (counter.IsFailure) return counter.As<Func<PrimeDbContext, Result>>();

            var replaced = dto.Id == 0 ? Result.Ok() : Posting.EnsureReversible(_journals, dto.Id)
                .Then(() => _stock.StaysPositive(_stock.Effects(SourceKey, dto.Id, -1)
                    .Concat(lines.Select(l => (l.ProductId, dto.WarehouseId, l.Qty)))));
            if (replaced.IsFailure) return replaced.As<Func<PrimeDbContext, Result>>();

            var total = InventoryCosting.Replay(lines.Select(l => new InventoryCosting.Entry(MovementType.In, l.Qty, l.UnitCost))).Value;

            return Result.Ok<Func<PrimeDbContext, Result>>(db =>
            {
                if (dto.Id > 0) Remove(db, dto.Id);

                var entryId = Posting.Entry(_journals, db, dto.Date, Msg("OpeningDescription", warehouse), EntityName,
                    inventory.Value, counter.Value, total);
                var entryNo = _ledger.GetById(entryId, db)?.EntryNo;

                foreach (var line in lines)
                {
                    var moved = _stock.RecordMovement(db, line.ProductId, dto.WarehouseId,
                        MovementType.In, line.Qty, line.UnitCost, EntityName, entryId, entryNo, dto.Date, line.Notes);
                    if (moved.IsFailure) return moved;
                }

                dto.Id = entryId;
                return Result.Ok();
            });
        }

        /// <summary>القيد ملكه</summary>
        private bool Owned(int id) => _ledger.GetById(id)?.Source == SourceKey;

        private void Remove(PrimeDbContext db, int id)
        {
            _stock.RemoveMovements(db, SourceKey, id);
            Posting.Reverse(_journals, db, id);
        }
    }
}
