using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Permissions;
using Db = PrimeERP.Data.Core.DbHelper;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Services.Inventory
{
    // أساس مشترك لـStockInService/StockOutService — الفرق الوحيد فعلياً MovementType واسم السلسلة الرقمية
    // ("StockIn"/"StockOut")؛ بلا ترحيل محاسبي (تسويات مخزون بحتة، نطاق مُبسَّط عمداً — راجع تعليق سابق).
    public abstract class StockAdjustmentServiceBase<TRepo> : ServiceBase where TRepo : IStockInRepository
    {
        protected readonly TRepo Repo;
        private readonly IProductRepository _products;
        private readonly IWarehouseService _warehouses;
        private readonly IStockService _stock;
        private readonly INumberSequenceService _numbers;
        private readonly IDocumentLinkService _links;
        private readonly MovementType _direction;
        private readonly string _sequenceKey, _permissionAction, _entityName;

        protected StockAdjustmentServiceBase(TRepo repo, IProductRepository products, IWarehouseService warehouses, IStockService stock,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentLinkService links,
            MovementType direction, string sequenceKey, string permissionAction, string entityName)
            : base(permissions, settings, localization, audit)
        {
            Repo = repo; _products = products; _warehouses = warehouses; _stock = stock; _numbers = numbers;
            _links = links; _direction = direction;
            _sequenceKey = sequenceKey; _permissionAction = permissionAction; _entityName = entityName;
        }

        protected override string PermissionPrefix => "Inventory";
        protected override string StringPrefix => "Str.Stock";
        protected override string EntityName => _entityName;

        private bool Can() => Permissions.Can($"Inventory.{_permissionAction}");

        public Result<PagedResult<StockAdjustmentDto>> GetPaged(int page, int pageSize, StockAdjustmentFilter filter = null)
        {
            if (!Can()) return FailDenied<PagedResult<StockAdjustmentDto>>();
            filter ??= new StockAdjustmentFilter();

            var (items, total) = Repo.GetPaged(page, pageSize, filter.SearchText, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<StockAdjustmentDto> { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<StockAdjustmentDetailDto> GetById(int id)
        {
            if (!Can()) return FailDenied<StockAdjustmentDetailDto>();

            var doc = Repo.GetById(id);
            if (doc == null) return Result.Fail<StockAdjustmentDetailDto>("المستند غير موجود", ErrorCode.NotFound);

            var baseDto = ToDto(doc);
            return Result.Ok(new StockAdjustmentDetailDto
            {
                Id = baseDto.Id, DocNo = baseDto.DocNo, MovementDate = baseDto.MovementDate, WarehouseId = baseDto.WarehouseId,
                WarehouseName = baseDto.WarehouseName, TotalQty = baseDto.TotalQty, CreatedAt = baseDto.CreatedAt,
                Lines = Repo.GetLines(id).Select(l => new StockAdjustmentLineDto
                { Id = l.Id, LineNo = l.LineNo, ProductCode = l.ProductCode, ProductName = l.ProductName, Qty = l.Qty, UnitCost = l.UnitCost, Notes = l.Notes }).ToList()
            });
        }

        public Result<StockAdjustmentDetailDto> Create(CreateStockAdjustmentDto dto)
        {
            if (!Can()) return FailDenied<StockAdjustmentDetailDto>();
            if (dto.Lines == null || dto.Lines.Count == 0) return Result.Fail<StockAdjustmentDetailDto>("المستند يحتاج سطراً واحداً على الأقل", ErrorCode.ValidationFailed);

            var resolvedLines = new List<StockAdjustmentLine>();
            var pulls = new List<CreateStockAdjustmentLineDto>();
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

                var pullCheck = _links.ValidatePull(l.SourceType, l.SourceId, l.SourceLineId, l.Qty, l.SourceNo);
                if (pullCheck.IsFailure) return Result.Fail<StockAdjustmentDetailDto>(pullCheck.ErrorMessage, pullCheck.ErrorCode);
                pulls.Add(l);
            }

            int docId;
            try
            {
                docId = Db.RunTransaction((conn, tx) =>
                {
                    var docNo = _numbers.Next(conn, tx, _sequenceKey);
                    var doc = new StockAdjustment { DocNo = docNo, MovementDate = dto.MovementDate, WarehouseId = dto.WarehouseId, Notes = dto.Notes, CreatedBy = AppSession.Username };
                    var id = Repo.InsertHeader(conn, tx, doc);

                    var links = new List<DocumentLink>();
                    for (int i = 0; i < resolvedLines.Count; i++)
                    {
                        var line = resolvedLines[i];
                        var lineId = Repo.InsertLine(conn, tx, id, line);
                        var moveResult = _stock.RecordMovement(conn, tx, line.ProductId, dto.WarehouseId, _direction, line.Qty, line.UnitCost, _entityName, id, docNo, dto.MovementDate);
                        if (!moveResult.IsSuccess) throw new InvalidOperationException(moveResult.ErrorMessage);

                        if (pulls[i].SourceLineId <= 0) continue;
                        links.Add(new DocumentLink
                        {
                            SourceType = pulls[i].SourceType, SourceId = pulls[i].SourceId, SourceNo = pulls[i].SourceNo,
                            SourceLineId = pulls[i].SourceLineId, TargetType = _entityName, TargetId = id,
                            TargetLineId = lineId, PulledQty = line.Qty
                        });
                    }
                    _links.RecordPull(links, conn, tx);

                    return id;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<StockAdjustmentDetailDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log(_entityName, docId, AuditAction.Insert, newValue: new { dto.WarehouseId, LineCount = resolvedLines.Count });
            return GetById(docId);
        }

        public Result Update(CreateStockAdjustmentDto dto) => Result.Fail("المستند مُرحَّل فور إنشائه — لا يمكن تعديله", ErrorCode.ValidationFailed);

        /// <summary>
        /// حذف الإذن وأثره المخزني معاً. لا قيد له — أذون المخزن حركةُ مخزون لا محاسبة. والسحب يمنع
        /// الحذف: إذنٌ سُحب منه يقوم عليه مستندٌ لاحق.
        /// </summary>
        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();
            if (Repo.GetById(id) == null) return Result.Fail("المستند غير موجود", ErrorCode.NotFound);

            if (_links.GetPulledBySource(_entityName, id).Count > 0)
                return Result.Fail("سُحب من هذا المستند — احذف ما سُحب إليه أولاً", ErrorCode.ValidationFailed);

            Db.RunTransaction((conn, tx) =>
            {
                _links.RemovePull(_entityName, id, conn, tx);
                _stock.RemoveMovements(conn, tx, _entityName, id);
                Repo.DeleteDocument(conn, tx, id);
            });

            Audit.Log(_entityName, id, AuditAction.Delete);
            return Result.Ok();
        }

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
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentLinkService links)
            : base(repo, products, warehouses, stock, numbers, permissions, settings, localization, audit, links, MovementType.In, "StockIn", "StockIn", "StockIn") { }
    }

    public class StockOutService : StockAdjustmentServiceBase<IStockOutRepository>, IStockOutService
    {
        public StockOutService(IStockOutRepository repo, IProductRepository products, IWarehouseService warehouses, IStockService stock,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentLinkService links)
            : base(repo, products, warehouses, stock, numbers, permissions, settings, localization, audit, links, MovementType.Out, "StockOut", "StockOut", "StockOut") { }
    }

    public class GoodsReceiptService : StockAdjustmentServiceBase<IGoodsReceiptRepository>, IGoodsReceiptService
    {
        public GoodsReceiptService(IGoodsReceiptRepository repo, IProductRepository products, IWarehouseService warehouses, IStockService stock,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentLinkService links)
            : base(repo, products, warehouses, stock, numbers, permissions, settings, localization, audit, links, MovementType.In, "GoodsReceipt", "GoodsReceipt", "GoodsReceipt") { }
    }

    public class GoodsIssueService : StockAdjustmentServiceBase<IGoodsIssueRepository>, IGoodsIssueService
    {
        public GoodsIssueService(IGoodsIssueRepository repo, IProductRepository products, IWarehouseService warehouses, IStockService stock,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentLinkService links)
            : base(repo, products, warehouses, stock, numbers, permissions, settings, localization, audit, links, MovementType.Out, "GoodsIssue", "GoodsIssue", "GoodsIssue") { }
    }

    public class DeliveryNoteService : StockAdjustmentServiceBase<IDeliveryNoteRepository>, IDeliveryNoteService
    {
        public DeliveryNoteService(IDeliveryNoteRepository repo, IProductRepository products, IWarehouseService warehouses, IStockService stock,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentLinkService links)
            : base(repo, products, warehouses, stock, numbers, permissions, settings, localization, audit, links, MovementType.Out, "DeliveryNote", "DeliveryNote", "DeliveryNote") { }
    }

    public class SalesReceiptService : StockAdjustmentServiceBase<ISalesReceiptRepository>, ISalesReceiptService
    {
        public SalesReceiptService(ISalesReceiptRepository repo, IProductRepository products, IWarehouseService warehouses, IStockService stock,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentLinkService links)
            : base(repo, products, warehouses, stock, numbers, permissions, settings, localization, audit, links, MovementType.In, "SalesReceipt", "SalesReceipt", "SalesReceipt") { }
    }
}
