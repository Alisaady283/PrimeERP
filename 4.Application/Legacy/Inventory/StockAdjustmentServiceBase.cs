using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Legacy.Documents;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Legacy.Inventory
{
    /// <summary>خدمةٌ بوّابتها مفتاحٌ واحد مُعلَن</summary>
    public interface IPermissionGated
    {
        string PermissionKey { get; }
    }

    public abstract class StockAdjustmentServiceBase<TRepo>
        : DocumentService<StockAdjustment, StockAdjustmentDto, StockAdjustmentDetailDto, CreateStockAdjustmentDto, StockAdjustmentFilter>,
          IPermissionGated
        where TRepo : IStockInRepository
    {
        protected readonly TRepo Repo;
        private readonly IProductRepository _products;
        private readonly ILookupRepository<Warehouse> _warehouses;
        private readonly IStockMove _stock;
        private readonly INumberSequenceService _numbers;
        private readonly MovementType _direction;
        private readonly string _sequenceKey, _permissionAction, _entityName;

        protected StockAdjustmentServiceBase(TRepo repo, IProductRepository products, ILookupRepository<Warehouse> warehouses, IStockMove stock,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentPull links,
            MovementType direction, string sequenceKey, string permissionAction, string entityName)
            : base(permissions, settings, localization, audit, links)
        {
            Repo = repo; _products = products; _warehouses = warehouses; _stock = stock; _numbers = numbers;
            _direction = direction;
            _sequenceKey = sequenceKey; _permissionAction = permissionAction; _entityName = entityName;
        }

        protected override string PermissionPrefix => "Inventory";
        protected override string StringPrefix => "Str.Stock";
        protected override string EntityName => _entityName;
        protected override string PullType => _entityName;

        public string PermissionKey => $"{PermissionPrefix}.{_permissionAction}";

        protected override bool CanDo(string action) => Permissions.Can(PermissionKey);

        protected override StockAdjustment FindHead(int id) => Repo.GetById(id);
        protected override int IdOf(StockAdjustment head) => head.Id;
        protected override object AuditValue(StockAdjustment head) => new { head?.WarehouseId };

        protected override (List<StockAdjustment> Items, int Total) FindPage(int page, int pageSize, StockAdjustmentFilter filter)
        {
            filter ??= new StockAdjustmentFilter();
            return Repo.GetPaged(page, pageSize, filter.SearchText, filter.SortBy, filter.SortDescending);
        }

        protected override List<StockAdjustmentDto> ToRows(List<StockAdjustment> heads)
        {
            var warehouses = _warehouses.NamesOf(heads.Select(d => d.WarehouseId));
            var totals = Repo.TotalQty(heads.Select(d => d.Id));
            return heads.Select(d => ToDto<StockAdjustmentDto>(d, warehouses, totals)).ToList();
        }

        protected override StockAdjustmentDetailDto ToDetail(StockAdjustment head)
        {
            var detail = ToDto<StockAdjustmentDetailDto>(head, _warehouses.NamesOf(new[] { head.WarehouseId }), Repo.TotalQty(new[] { head.Id }));
            detail.Lines = Repo.GetLines(head.Id).Select(l => Rows.Copy(l, new StockAdjustmentLineDto())).ToList();
            Links.Attach(EntityName, head.Id, detail.Lines);
            return detail;
        }

        protected override Result<Func<PrimeDbContext, int>> Plan(CreateStockAdjustmentDto dto)
        {
            var shape = DocumentLines.Check(dto.Lines, l => l.Qty);
            if (shape.IsFailure) return shape.As<Func<PrimeDbContext, int>>();

            var lines = ProductLines.Resolve(_products, dto.Lines, l => l.ProductCode, (l, product, _) =>
                Result.Ok(Rows.Copy(l, new StockAdjustmentLine(), to =>
                {
                    to.UnitCost = InventoryCosting.LineCost(l.UnitCost, product.CostPrice);
                })));
            if (lines.IsFailure) return lines.As<Func<PrimeDbContext, int>>();
            var pulls = Links.ValidatePulls(dto.Lines.Select(l => ((IPullableLine)l, l.Qty)), EntityName, dto.Id);
            if (pulls.IsFailure) return pulls.As<Func<PrimeDbContext, int>>();
            var resolved = lines.Value;

            return Result.Ok<Func<PrimeDbContext, int>>(db =>
            {
                var docNo = _numbers.Next(db, _sequenceKey);
                var id = Repo.InsertHeader(db, Rows.Copy(dto, new StockAdjustment(), to =>
                {
                    to.DocNo = docNo;
                }));

                var inserted = resolved.Select((line, i) =>
                {
                    var lineId = Repo.InsertLine(db, id, line);
                    var moved = _stock.RecordMovement(db, line.ProductId, dto.WarehouseId, _direction, line.Qty, line.UnitCost,
                        _entityName, id, docNo, dto.MovementDate);
                    if (moved.IsFailure) throw new InvalidOperationException(moved.ErrorMessage);
                    return ((IPullableLine)dto.Lines[i], lineId, line.Qty);
                }).ToList();

                Links.RecordPulls(db, _entityName, id, inserted);
                return id;
            });
        }

        protected override void Remove(PrimeDbContext db, StockAdjustment head)
        {
            Links.RemovePull(_entityName, head.Id, db);
            _stock.RemoveMovements(db, _entityName, head.Id);
            Repo.DeleteDocument(db, head.Id);
        }

        private static T ToDto<T>(StockAdjustment d, IReadOnlyDictionary<int, string> warehouses,
            IReadOnlyDictionary<int, decimal> totals) where T : StockAdjustmentDto, new() => Rows.Copy<T>(d, new(), to =>
            {
                to.WarehouseName = warehouses.GetValueOrDefault(d.WarehouseId);
                to.TotalQty = totals.GetValueOrDefault(d.Id);
            });
    }

    public class StockInService : StockAdjustmentServiceBase<IStockInRepository>, IStockInService
    {
        public StockInService(IStockInRepository repo, IProductRepository products, ILookupRepository<Warehouse> warehouses, IStockMove stock,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentPull links)
            : base(repo, products, warehouses, stock, numbers, permissions, settings, localization, audit, links, MovementType.In, "StockIn", "StockIn", "StockIn") { }
    }

    public class StockOutService : StockAdjustmentServiceBase<IStockOutRepository>, IStockOutService
    {
        public StockOutService(IStockOutRepository repo, IProductRepository products, ILookupRepository<Warehouse> warehouses, IStockMove stock,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentPull links)
            : base(repo, products, warehouses, stock, numbers, permissions, settings, localization, audit, links, MovementType.Out, "StockOut", "StockOut", "StockOut") { }
    }

    public class GoodsReceiptService : StockAdjustmentServiceBase<IGoodsReceiptRepository>, IGoodsReceiptService
    {
        public GoodsReceiptService(IGoodsReceiptRepository repo, IProductRepository products, ILookupRepository<Warehouse> warehouses, IStockMove stock,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentPull links)
            : base(repo, products, warehouses, stock, numbers, permissions, settings, localization, audit, links, MovementType.In, "GoodsReceipt", "GoodsReceipt", "GoodsReceipt") { }
    }

    public class GoodsIssueService : StockAdjustmentServiceBase<IGoodsIssueRepository>, IGoodsIssueService
    {
        public GoodsIssueService(IGoodsIssueRepository repo, IProductRepository products, ILookupRepository<Warehouse> warehouses, IStockMove stock,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentPull links)
            : base(repo, products, warehouses, stock, numbers, permissions, settings, localization, audit, links, MovementType.Out, "GoodsIssue", "GoodsIssue", "GoodsIssue") { }
    }

    public class DeliveryNoteService : StockAdjustmentServiceBase<IDeliveryNoteRepository>, IDeliveryNoteService
    {
        public DeliveryNoteService(IDeliveryNoteRepository repo, IProductRepository products, ILookupRepository<Warehouse> warehouses, IStockMove stock,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentPull links)
            : base(repo, products, warehouses, stock, numbers, permissions, settings, localization, audit, links, MovementType.Out, "DeliveryNote", "DeliveryNote", "DeliveryNote") { }
    }

    public class SalesReceiptService : StockAdjustmentServiceBase<ISalesReceiptRepository>, ISalesReceiptService
    {
        public SalesReceiptService(ISalesReceiptRepository repo, IProductRepository products, ILookupRepository<Warehouse> warehouses, IStockMove stock,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IDocumentPull links)
            : base(repo, products, warehouses, stock, numbers, permissions, settings, localization, audit, links, MovementType.In, "SalesReceipt", "SalesReceipt", "SalesReceipt") { }
    }
}
