using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Permissions;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services.Inventory
{
    // أساس مشترك لـStockInService/StockOutService — الفرق الوحيد فعلياً MovementType واسم السلسلة الرقمية
    // ("StockIn"/"StockOut")؛ بلا ترحيل محاسبي (تسويات مخزون بحتة، نطاق مُبسَّط عمداً — راجع تعليق سابق).
    public abstract class StockAdjustmentServiceBase<TRepo> where TRepo : IStockInRepository
    {
        protected readonly TRepo Repo;
        private readonly IProductRepository _products;
        private readonly IWarehouseService _warehouses;
        private readonly IStockService _stock;
        private readonly INumberSequenceService _numbers;
        private readonly IPermissionService _permissions;
        private readonly IAuditLogger _audit;
        private readonly MovementType _direction;
        private readonly string _sequenceKey, _permissionAction, _entityName;

        protected StockAdjustmentServiceBase(TRepo repo, IProductRepository products, IWarehouseService warehouses, IStockService stock,
            INumberSequenceService numbers, IPermissionService permissions, IAuditLogger audit,
            MovementType direction, string sequenceKey, string permissionAction, string entityName)
        {
            Repo = repo; _products = products; _warehouses = warehouses; _stock = stock; _numbers = numbers;
            _permissions = permissions; _audit = audit; _direction = direction;
            _sequenceKey = sequenceKey; _permissionAction = permissionAction; _entityName = entityName;
        }

        private bool Can() => _permissions.Can($"Inventory.{_permissionAction}");

        public Result<PagedResult<StockAdjustmentDto>> GetPaged(int page, int pageSize, StockAdjustmentFilter filter = null)
        {
            if (!Can()) return Result.Fail<PagedResult<StockAdjustmentDto>>("لا صلاحية", ErrorCode.Unauthorized);
            filter ??= new StockAdjustmentFilter();

            var (items, total) = Repo.GetPaged(page, pageSize, filter.SearchText, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<StockAdjustmentDto> { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<StockAdjustmentDetailDto> GetById(int id)
        {
            if (!Can()) return Result.Fail<StockAdjustmentDetailDto>("لا صلاحية", ErrorCode.Unauthorized);

            var doc = Repo.GetById(id);
            if (doc == null) return Result.Fail<StockAdjustmentDetailDto>("المستند غير موجود", ErrorCode.NotFound);

            var baseDto = ToDto(doc);
            return Result.Ok(new StockAdjustmentDetailDto
            {
                Id = baseDto.Id, DocNo = baseDto.DocNo, MovementDate = baseDto.MovementDate, WarehouseId = baseDto.WarehouseId,
                WarehouseName = baseDto.WarehouseName, TotalQty = baseDto.TotalQty, CreatedAt = baseDto.CreatedAt,
                Lines = Repo.GetLines(id).Select(l => new StockAdjustmentLineDto
                { LineNo = l.LineNo, ProductCode = l.ProductCode, ProductName = l.ProductName, Qty = l.Qty, UnitCost = l.UnitCost, Notes = l.Notes }).ToList()
            });
        }

        public Result<StockAdjustmentDetailDto> Create(CreateStockAdjustmentDto dto)
        {
            if (!Can()) return Result.Fail<StockAdjustmentDetailDto>("لا صلاحية", ErrorCode.Unauthorized);
            if (dto.Lines == null || dto.Lines.Count == 0) return Result.Fail<StockAdjustmentDetailDto>("المستند يحتاج سطراً واحداً على الأقل", ErrorCode.ValidationFailed);

            var resolvedLines = new List<StockAdjustmentLine>();
            foreach (var l in dto.Lines)
            {
                var product = _products.GetByCode(l.ProductCode);
                if (product == null) return Result.Fail<StockAdjustmentDetailDto>($"الصنف بالكود {l.ProductCode} غير موجود", ErrorCode.ValidationFailed);
                if (l.Qty <= 0) return Result.Fail<StockAdjustmentDetailDto>("الكمية يجب أن تكون أكبر من صفر", ErrorCode.ValidationFailed);

                resolvedLines.Add(new StockAdjustmentLine
                {
                    LineNo = l.LineNo, ProductId = product.Id, ProductCode = product.Code, ProductName = product.Name,
                    Qty = l.Qty, UnitCost = l.UnitCost > 0 ? l.UnitCost : product.CostPrice, Notes = l.Notes
                });
            }

            int docId;
            try
            {
                docId = Db.RunTransaction((conn, tx) =>
                {
                    var docNo = _numbers.Next(conn, tx, _sequenceKey);
                    var doc = new StockAdjustment { DocNo = docNo, MovementDate = dto.MovementDate, WarehouseId = dto.WarehouseId, Notes = dto.Notes, CreatedBy = AppSession.Username };
                    var id = Repo.InsertHeader(conn, tx, doc);

                    foreach (var line in resolvedLines)
                    {
                        Repo.InsertLine(conn, tx, id, line);
                        var moveResult = _stock.RecordMovement(conn, tx, line.ProductId, dto.WarehouseId, _direction, line.Qty, line.UnitCost, _entityName, id, docNo, dto.MovementDate);
                        if (!moveResult.IsSuccess) throw new InvalidOperationException(moveResult.ErrorMessage);
                    }

                    return id;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<StockAdjustmentDetailDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            _audit.Log(_entityName, docId, AuditAction.Insert, newValue: new { dto.WarehouseId, LineCount = resolvedLines.Count });
            return GetById(docId);
        }

        public Result Update(CreateStockAdjustmentDto dto) => Result.Fail("المستند مُرحَّل فور إنشائه — لا يمكن تعديله", ErrorCode.ValidationFailed);

        public Result Delete(int id) => Result.Fail("المستند مُرحَّل فور إنشائه — لا يمكن حذفه", ErrorCode.ValidationFailed);

        private StockAdjustmentDto ToDto(StockAdjustment d) => new()
        {
            Id = d.Id, DocNo = d.DocNo, MovementDate = d.MovementDate, WarehouseId = d.WarehouseId,
            WarehouseName = _warehouses.GetAll().Value.FirstOrDefault(w => w.Id == d.WarehouseId)?.Name,
            TotalQty = Repo.GetLines(d.Id).Sum(l => l.Qty), CreatedAt = d.CreatedAt
        };
    }

    public class StockInService : StockAdjustmentServiceBase<IStockInRepository>, IStockInService
    {
        public StockInService(IStockInRepository repo, IProductRepository products, IWarehouseService warehouses, IStockService stock,
            INumberSequenceService numbers, IPermissionService permissions, IAuditLogger audit)
            : base(repo, products, warehouses, stock, numbers, permissions, audit, MovementType.In, "StockIn", "StockIn", "StockIn") { }
    }

    public class StockOutService : StockAdjustmentServiceBase<IStockOutRepository>, IStockOutService
    {
        public StockOutService(IStockOutRepository repo, IProductRepository products, IWarehouseService warehouses, IStockService stock,
            INumberSequenceService numbers, IPermissionService permissions, IAuditLogger audit)
            : base(repo, products, warehouses, stock, numbers, permissions, audit, MovementType.Out, "StockOut", "StockOut", "StockOut") { }
    }

    public class GoodsReceiptService : StockAdjustmentServiceBase<IGoodsReceiptRepository>, IGoodsReceiptService
    {
        public GoodsReceiptService(IGoodsReceiptRepository repo, IProductRepository products, IWarehouseService warehouses, IStockService stock,
            INumberSequenceService numbers, IPermissionService permissions, IAuditLogger audit)
            : base(repo, products, warehouses, stock, numbers, permissions, audit, MovementType.In, "GoodsReceipt", "GoodsReceipt", "GoodsReceipt") { }
    }

    public class GoodsIssueService : StockAdjustmentServiceBase<IGoodsIssueRepository>, IGoodsIssueService
    {
        public GoodsIssueService(IGoodsIssueRepository repo, IProductRepository products, IWarehouseService warehouses, IStockService stock,
            INumberSequenceService numbers, IPermissionService permissions, IAuditLogger audit)
            : base(repo, products, warehouses, stock, numbers, permissions, audit, MovementType.Out, "GoodsIssue", "GoodsIssue", "GoodsIssue") { }
    }

    public class DeliveryNoteService : StockAdjustmentServiceBase<IDeliveryNoteRepository>, IDeliveryNoteService
    {
        public DeliveryNoteService(IDeliveryNoteRepository repo, IProductRepository products, IWarehouseService warehouses, IStockService stock,
            INumberSequenceService numbers, IPermissionService permissions, IAuditLogger audit)
            : base(repo, products, warehouses, stock, numbers, permissions, audit, MovementType.Out, "DeliveryNote", "DeliveryNote", "DeliveryNote") { }
    }

    public class SalesReceiptService : StockAdjustmentServiceBase<ISalesReceiptRepository>, ISalesReceiptService
    {
        public SalesReceiptService(ISalesReceiptRepository repo, IProductRepository products, IWarehouseService warehouses, IStockService stock,
            INumberSequenceService numbers, IPermissionService permissions, IAuditLogger audit)
            : base(repo, products, warehouses, stock, numbers, permissions, audit, MovementType.In, "SalesReceipt", "SalesReceipt", "SalesReceipt") { }
    }
}
