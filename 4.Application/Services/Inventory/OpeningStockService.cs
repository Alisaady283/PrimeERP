using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services.Inventory
{
    public interface IOpeningStockService
    {
        Result<CreateOpeningStockDto> Create(CreateOpeningStockDto dto);
    }

    /// <summary>
    /// رصيد أول المدة للأصناف: حركة وارد لكل صنف بتكلفتها فيبدأ المتوسط المرجّح من أول يوم، وقيدٌ واحد
    /// من حـ/ المخزون إلى الحساب المختار من الإعدادات. الاثنان في معاملة واحدة — فلا مخزونٌ بلا قيده.
    /// </summary>
    public class OpeningStockService : ServiceBase, IOpeningStockService, IPermissionGated
    {
        private readonly IStockService _stock;
        private readonly IJournalService _journals;

        public OpeningStockService(IStockService stock, IJournalService journals, IPermissionService permissions,
            ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _stock = stock; _journals = journals;
        }

        protected override string PermissionPrefix => "Inventory";
        protected override string StringPrefix => "Str.Stock";
        protected override string EntityName => "OpeningStock";

        public string PermissionKey => PermissionKeys.Inventory.OpeningStock;

        public Result<CreateOpeningStockDto> Create(CreateOpeningStockDto dto)
        {
            if (!Permissions.Can(PermissionKey)) return FailDenied<CreateOpeningStockDto>();

            var lines = (dto.Lines ?? new()).Where(l => l.ProductId > 0 && l.Qty > 0).ToList();
            if (lines.Count == 0)
                return Result.Fail<CreateOpeningStockDto>("لا أصناف في المستند", ErrorCode.ValidationFailed);

            if (dto.WarehouseId <= 0)
                return Result.Fail<CreateOpeningStockDto>("المخزن مطلوب", ErrorCode.ValidationFailed);

            var inventory = Setting(SettingKeys.Accounts.Inventory, "");
            var counter = Setting(SettingKeys.Accounts.OpeningAdjustments, "");
            if (string.IsNullOrWhiteSpace(counter))
                return Result.Fail<CreateOpeningStockDto>(
                    "حدّد حساب «الأرصدة الافتتاحية والتسويات» من الإعدادات أولاً", ErrorCode.ValidationFailed);

            var total = lines.Sum(l => l.Qty * l.UnitCost);

            try
            {
                Db.RunTransaction((conn, tx) =>
                {
                    var entry = _journals.Create(conn, tx, new CreateJournalDto
                    {
                        EntryDate = dto.Date,
                        Description = "رصيد أول المدة للأصناف",
                        Source = EntityName,
                        Lines = new List<CreateJournalLineDto>
                        {
                            new() { LineNo = 1, AccountCode = inventory, Debit = total, Credit = 0 },
                            new() { LineNo = 2, AccountCode = counter,   Debit = 0, Credit = total }
                        }
                    });
                    if (entry.IsFailure) throw new InvalidOperationException(entry.ErrorMessage);

                    _journals.Post(conn, tx, entry.Value.Id);

                    foreach (var line in lines)
                    {
                        var moved = _stock.RecordMovement(conn, tx, line.ProductId, dto.WarehouseId,
                            MovementType.In, line.Qty, line.UnitCost, EntityName, entry.Value.Id, entry.Value.EntryNo,
                            dto.Date, line.Notes);

                        if (moved.IsFailure) throw new InvalidOperationException(moved.ErrorMessage);
                    }

                    dto.Id = entry.Value.Id;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<CreateOpeningStockDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log(EntityName, dto.Id, AuditAction.Insert, newValue: new { lines.Count, total });
            return Result.Ok(dto);
        }
    }
}
