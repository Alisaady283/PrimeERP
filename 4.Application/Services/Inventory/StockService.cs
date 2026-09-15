using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using PrimeERP.Application.Services.Common;
using PrimeERP.Domain.Rules;
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

        public Result<List<Domain.Entities.StockMovement>> GetHistory(int productId, int? warehouseId, int maxResults = 200) =>
            Result.Ok(_movements.GetHistory(productId, warehouseId, maxResults));

        public Result<List<Domain.Entities.StockMovement>> GetMovements(DateTime from, DateTime to, int? warehouseId = null, int maxResults = 500) =>
            Result.Ok(_movements.GetMovements(from, to, warehouseId, maxResults));

        public Result RecordMovement(DbConnection conn, DbTransaction tx, int productId, int warehouseId, MovementType type,
            decimal qty, decimal unitCost, string sourceDocType, int? sourceDocId, string sourceDocNo, DateTime? date = null, string notes = null)
        {
            if (type != MovementType.Adjustment && qty <= 0) return Fail("الكمية يجب أن تكون أكبر من صفر", ErrorCode.ValidationFailed);

            // القراءة داخل معاملة المستند لا خارجها: معاملتان متتاليتان تُسلسَلان، فالثانية ترى أثر الأولى
            // ملتزماً وتُرفض لو لم يبقَ ما يكفي — وهو تتابعٌ صحيح مهما تقارب وقتُ تسجيلهما.
            var currentBalance = _movements.GetBalance(productId, warehouseId, conn, tx);
            var signedQty = type == MovementType.Out ? -qty : qty;

            // الحارس على كل ما يُنقص الرصيد لا على الصرف وحده: التسوية بكميةٍ سالبة تُنزله تحت الصفر أيضاً.
            if (currentBalance + signedQty < 0)
                return Fail("الرصيد المتاح غير كافٍ لإتمام هذه الحركة", ErrorCode.ValidationFailed);

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

        public void RemoveMovements(DbConnection conn, DbTransaction tx, string sourceDocType, int sourceDocId) =>
            _movements.DeleteBySource(conn, tx, sourceDocType, sourceDocId);

        public decimal? SourceUnitCost(DbConnection conn, DbTransaction tx, string sourceDocType, int sourceDocId, int productId) =>
            _movements.GetSourceUnitCost(sourceDocType, sourceDocId, productId, conn, tx);

        public Result<List<StockMovement>> GetCostingHistory(int productId) =>
            Result.Ok(_movements.GetForCosting(productId));

        public decimal CurrentUnitCost(DbConnection conn, DbTransaction tx, int productId) =>
            InventoryCosting.Replay(_movements.GetForCosting(productId, conn, tx)
                .Select(m => new InventoryCosting.Entry(m.MovementType, m.Qty, m.UnitCost))).UnitCost;

        /// <summary>
        /// الطبقات تُشتقّ من سجلّ الحركات لا تُخزَّن — طابورٌ واحد لكل صنف عبر المخازن كلها. تُقرأ مرّةً
        /// لكل صنف في المستند ثم تُستهلك سطراً سطراً، فلا استعلام لكل سطر ولا ازدواج في التسعير.
        /// </summary>
        public Result<List<decimal>> GetIssueCosts(DbConnection conn, DbTransaction tx,
            List<(int ProductId, decimal Qty)> lines)
        {
            var balances = new Dictionary<int, InventoryCosting.Balance>();
            var costs = new List<decimal>(lines.Count);

            foreach (var (productId, qty) in lines)
            {
                if (!balances.TryGetValue(productId, out var balance))
                {
                    balance = InventoryCosting.Replay(_movements.GetForCosting(productId, conn, tx)
                        .Select(m => new InventoryCosting.Entry(m.MovementType, m.Qty, m.UnitCost)));
                    balances[productId] = balance;
                }

                if (!InventoryCosting.TryIssueCost(balance, qty, out var cost))
                    return Fail<List<decimal>>("الرصيد المتاح غير كافٍ لإتمام هذه الحركة", ErrorCode.ValidationFailed);

                // الرصيد يتحرّك مع السطر: سطران لنفس الصنف في مستندٍ واحد يُسعَّر ثانيهما بعد أوّلهما.
                balances[productId] = InventoryCosting.Apply(balance,
                    new InventoryCosting.Entry(MovementType.Out, qty, 0), out _);

                costs.Add(cost);
            }

            return Result.Ok(costs);
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
