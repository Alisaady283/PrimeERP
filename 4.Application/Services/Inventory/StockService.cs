using System;
using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Application.Services.Common;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services.Inventory
{
    // المصدر الوحيد لحركة المخزون — SalesInvoiceService/PurchaseInvoiceService/StockIn/Out/Transfer كلها
    // تستدعي RecordMovement (نسخة conn,tx لتشارك معاملة المستند نفسها)، لا تكتب SQL مخزون مباشرة أبداً.
    public class StockService : ServiceBase, IStockService
    {
        protected override string PermissionPrefix => "Inventory";
        protected override string StringPrefix => "Str.Stock";
        protected override string EntityName => "StockMovements";

        private readonly IStockMovementRepository _movements;
        private readonly INumberSequenceService _numbers;

        public StockService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IStockMovementRepository movements, INumberSequenceService numbers) : base(permissions, settings, localization, audit)
        {
            _movements = movements;
            _numbers = numbers;
        }

        public Result<decimal> GetBalance(int productId, int? warehouseId = null) =>
            Result.Ok(_movements.GetBalance(productId, warehouseId));

        public Result<List<(int ProductId, int WarehouseId, decimal Balance)>> GetAllBalances() =>
            Result.Ok(_movements.GetAllBalances());

        public Result RecordMovement(DbConnection conn, DbTransaction tx, int productId, int warehouseId, MovementType type,
            decimal qty, decimal unitCost, string sourceDocType, int? sourceDocId, string sourceDocNo, DateTime? date = null, string notes = null)
        {
            if (type != MovementType.Adjustment && qty <= 0) return Fail("الكمية يجب أن تكون أكبر من صفر", ErrorCode.ValidationFailed);

            var currentBalance = _movements.GetBalance(productId, warehouseId, conn, tx);
            if (type == MovementType.Out && currentBalance < qty)
                return Fail("الرصيد المتاح غير كافٍ لإتمام هذه الحركة", ErrorCode.ValidationFailed);

            var signedQty = type == MovementType.Out ? -qty : qty;

            var movement = new StockMovement
            {
                MovementNo = _numbers.Next(conn, tx, "StockMovement"), MovementDate = date ?? DateTime.Now,
                ProductId = productId, WarehouseId = warehouseId, MovementType = type, Qty = qty, UnitCost = unitCost,
                TotalCost = Math.Abs(qty) * unitCost, BalanceAfter = currentBalance + signedQty,
                SourceDocType = sourceDocType, SourceDocId = sourceDocId, SourceDocNo = sourceDocNo, Notes = notes, CreatedBy = CurrentUser
            };

            _movements.Insert(movement, conn, tx);
            return Result.Ok();
        }

        public Result Transfer(int productId, int fromWarehouseId, int toWarehouseId, decimal qty, string notes = null)
        {
            if (!Can("Transfer")) return FailDenied();
            if (fromWarehouseId == toWarehouseId) return Fail("المخزن المصدر والهدف لا يمكن أن يكونا نفس المخزن", ErrorCode.ValidationFailed);

            try
            {
                Db.RunTransaction((conn, tx) =>
                {
                    var no = _numbers.Next(conn, tx, "StockTransfer");
                    var outResult = RecordMovement(conn, tx, productId, fromWarehouseId, MovementType.Out, qty, 0, "StockTransfer", null, no, notes: notes);
                    if (!outResult.IsSuccess) throw new InvalidOperationException(outResult.ErrorMessage);

                    var inResult = RecordMovement(conn, tx, productId, toWarehouseId, MovementType.In, qty, 0, "StockTransfer", null, no, notes: notes);
                    if (!inResult.IsSuccess) throw new InvalidOperationException(inResult.ErrorMessage);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Fail(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log(EntityName, 0, AuditAction.Insert, details: $"تحويل {qty} من مخزن {fromWarehouseId} إلى {toWarehouseId}");
            return Result.Ok();
        }
    }
}
