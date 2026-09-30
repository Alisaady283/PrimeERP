using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Application.Services.Ledger;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Legacy.Accounting;
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
        Result<CreateOpeningStockDto> Create(CreateOpeningStockDto dto);
    }

    public class OpeningStockService : ServiceBase, IOpeningStockService, IPermissionGated
    {
        private readonly IStockMove _stock;
        private readonly Entries _journals;
        private readonly IJournalRepository _ledger;

        public OpeningStockService(IStockMove stock, Entries journals, IJournalRepository ledger, IPermissionService permissions,
            ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _stock = stock; _journals = journals; _ledger = ledger;
        }

        protected override string PermissionPrefix => "Inventory";
        protected override string StringPrefix => "Str.Stock";
        protected override string EntityName => "OpeningStock";

        public string PermissionKey => PermissionKeys.Inventory.OpeningStock;

        public Result<CreateOpeningStockDto> Create(CreateOpeningStockDto dto)
        {
            if (!Permissions.Can(PermissionKey)) return FailDenied<CreateOpeningStockDto>();

            var lines = (dto.Lines ?? new()).Where(l => l.ProductId > 0 && l.Qty > 0).ToList();
            var input = Check.Valid(dto,
                new Field<CreateOpeningStockDto>(x => x.Lines, "", Must: _ => lines.Count > 0, Message: "Str.Stock.NoProducts"),
                new Field<CreateOpeningStockDto>(x => x.WarehouseId, "", Required: true, Message: "Str.Stock.WarehouseRequired"));
            if (input.IsFailure) return input.As<CreateOpeningStockDto>();

            var inventory = Setting(SettingKeys.Accounts.Inventory, "");
            var counter = Setting(SettingKeys.Accounts.OpeningAdjustments, "");
            if (string.IsNullOrWhiteSpace(counter))
                return Result.Fail<CreateOpeningStockDto>(Msg("OpeningAccountMissing"), ErrorCode.ValidationFailed);

            var total = lines.Sum(l => l.Qty * l.UnitCost);

            var created = Commit(db =>
            {
                var entryId = Posting.Entry(_journals, db, dto.Date, Msg("OpeningDescription"), EntityName, inventory, counter, total);
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
            if (created.IsFailure) return created.As<CreateOpeningStockDto>();

            Audit.Log(EntityName, dto.Id, AuditAction.Insert, newValue: new { lines.Count, total });
            return Result.Ok(dto);
        }
    }
}
