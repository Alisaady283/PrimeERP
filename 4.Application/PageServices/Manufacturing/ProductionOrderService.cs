using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Manufacturing;
using PrimeERP.Application.PageServices.Documents;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.PageServices.Manufacturing
{
    public interface IProductionOrderService
    {
        Result<PagedResult<ProductionOrderDto>> GetPaged(int page, int pageSize, ProductionOrderFilter filter = null);
        Result<ProductionOrderDetailDto> GetById(int id);
        Result<ProductionOrderDetailDto> Create(CreateProductionOrderDto dto);
        Result Delete(int id);
        Result<CreateProductionOrderDto> Open(int bomId, decimal qty);
    }

    public interface IUnbuildService : IProductionOrderService { }

    public class ProductionOrderService : DocumentService<ProductionOrder, ProductionOrderDto, ProductionOrderDetailDto, CreateProductionOrderDto, ProductionOrderFilter>,
        IProductionOrderService
    {
        private readonly IProductionOrderRepository _repo;
        private readonly IBillOfMaterialsRepository _boms;
        private readonly IProductRepository _products;
        private readonly ILookupRepository<WorkCenter> _centers;
        private readonly INumberSequenceService _numbers;
        private readonly AccountOf _accountsOf;
        private readonly Production _production;
        private readonly bool _unbuild;

        public ProductionOrderService(IProductionOrderRepository repo, IBillOfMaterialsRepository boms, IProductRepository products,
            ILookupRepository<WorkCenter> centers, INumberSequenceService numbers, AccountOf accountsOf, Production production,
            IStockMove stock, Entries journals, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : this(repo, boms, products, centers, numbers, accountsOf, production, stock, journals, permissions, settings, localization, audit, unbuild: false) { }

        protected ProductionOrderService(IProductionOrderRepository repo, IBillOfMaterialsRepository boms, IProductRepository products,
            ILookupRepository<WorkCenter> centers, INumberSequenceService numbers, AccountOf accountsOf, Production production,
            IStockMove stock, Entries journals, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            bool unbuild)
            : base(permissions, settings, localization, audit, journals: journals, stock: stock)
        {
            _repo = repo; _boms = boms; _products = products; _centers = centers; _numbers = numbers;
            _accountsOf = accountsOf; _production = production; _unbuild = unbuild;
        }

        protected override string PermissionPrefix => "Manufacturing";
        protected override string StringPrefix => "Str.Production";
        protected override string EntityName => _unbuild ? "Unbuild" : "Production";
        protected override string StockSource => EntityName;

        private static readonly Field<CreateProductionOrderDto>[] Rules =
        {
            new(x => x.BomId, "Str.Production.Bom", Positive: true),
            new(x => x.Qty, "Str.Qty", Positive: true),
            new(x => x.RawWarehouseId, "Str.Production.RawWarehouse", Positive: true),
            new(x => x.FinishedWarehouseId, "Str.Production.FinishedWarehouse", Positive: true),
            new(x => x.Lines, "", Must: d => d.Lines.All(l => l.Kind != (int)BomLineKind.Operation || l.WorkCenterId > 0), Message: "Str.Bom.OperationNeedsCenter"),
        };

        protected override ProductionOrder FindHead(int id) => _repo.GetById(id);
        protected override int IdOf(ProductionOrder head) => head.Id;
        protected override int? EntryOf(ProductionOrder head) => head.JournalEntryId;
        protected override object AuditValue(ProductionOrder head) => new { head?.OrderNo, head?.ProductCode, head?.Qty, head?.TotalCost };

        protected override (List<ProductionOrder> Items, int Total) FindPage(int page, int pageSize, ProductionOrderFilter filter)
        {
            filter ??= new ProductionOrderFilter();
            return _repo.GetPaged(page, pageSize, _unbuild, filter.SearchText, filter.SortBy, filter.SortDescending);
        }

        protected override List<ProductionOrderDto> ToRows(List<ProductionOrder> heads) => heads.Select(h => Rows.Copy(h, new ProductionOrderDto())).ToList();

        protected override ProductionOrderDetailDto ToDetail(ProductionOrder head)
        {
            var detail = Rows.Copy(head, new ProductionOrderDetailDto());
            detail.Lines = _repo.GetLines(head.Id).Select(l => Rows.Copy(l, new ProductionOrderLineDto(), to => to.Kind = (int)l.Kind)).ToList();
            return detail;
        }

        public Result<CreateProductionOrderDto> Open(int bomId, decimal qty)
        {
            if (!CanDo("Create")) return FailDenied<CreateProductionOrderDto>();
            var bom = _boms.GetById(bomId);
            if (bom == null) return Fail<CreateProductionOrderDto>("BomNotFound", ErrorCode.NotFound);

            var lines = _boms.GetLines(bomId).Where(l => !_unbuild || l.Kind == BomLineKind.Component).ToList();
            return Ok(new CreateProductionOrderDto
            {
                BomId = bomId, Qty = qty,
                Lines = lines.Select(l => Rows.Copy(l, new CreateProductionOrderLineDto(), to =>
                {
                    to.Kind = (int)l.Kind;
                    to.Qty = InventoryCosting.Scale(l.Qty, bom.Qty, qty, l.Kind == BomLineKind.Component && !_unbuild ? l.WastePercent : 0);
                })).ToList()
            });
        }

        protected override Result<Func<PrimeDbContext, int>> Plan(CreateProductionOrderDto dto)
        {
            var valid = Check.Valid(dto, Rules).Then(() => DocumentLines.Check(dto.Lines, l => l.Qty));
            if (valid.IsFailure) return valid.As<Func<PrimeDbContext, int>>();

            var bom = _boms.GetById(dto.BomId);
            var finishedProduct = bom == null ? null : _products.GetById(bom.ProductId);
            if (finishedProduct == null) return Fail<Func<PrimeDbContext, int>>("BomNotFound", ErrorCode.ValidationFailed);

            var stocked = ProductLines.Resolve(_products, dto.Lines.Where(l => l.Kind != (int)BomLineKind.Operation), l => l.ProductCode,
                (l, product, _) => Result.Ok(Rows.Copy(l, new ProductionOrderLine(), to =>
                {
                    to.Kind = (BomLineKind)l.Kind;
                    to.UnitCost = product.CostPrice;
                })));
            if (stocked.IsFailure) return stocked.As<Func<PrimeDbContext, int>>();

            var centers = _centers.GetAll(includeInactive: true).ToDictionary(c => c.Id, c => c.CostPerHour);
            var operations = dto.Lines.Where(l => l.Kind == (int)BomLineKind.Operation)
                .Select(l => Rows.Copy(l, new ProductionOrderLine(), to =>
                {
                    to.Kind = BomLineKind.Operation; to.ProductCode = null;
                    to.UnitCost = centers.GetValueOrDefault(l.WorkCenterId ?? 0);
                })).ToList();

            var inventory = _accountsOf.Setting(SettingKeys.Accounts.Inventory, "Str.Trade.InventoryMissing");
            if (inventory.IsFailure) return inventory.As<Func<PrimeDbContext, int>>();
            var overhead = operations.Count == 0 ? Result.Ok("") : _accountsOf.Setting(SettingKeys.Accounts.ProductionOverhead, "Str.Production.OverheadMissing");
            if (overhead.IsFailure) return overhead.As<Func<PrimeDbContext, int>>();

            var components = stocked.Value.Where(l => l.Kind == BomLineKind.Component).ToList();
            var byProducts = stocked.Value.Where(l => l.Kind == BomLineKind.ByProduct).ToList();

            return Result.Ok<Func<PrimeDbContext, int>>(db =>
            {
                var order = Rows.Copy(dto, new ProductionOrder(), to =>
                {
                    to.OrderNo = _numbers.Next(db, EntityName);
                    to.Unbuild = _unbuild;
                    to.ProductId = finishedProduct.Id; to.ProductCode = finishedProduct.Code; to.ProductName = finishedProduct.Name;
                });
                order.Id = _repo.InsertHeader(db, order);

                var finished = new ProductionOrderLine
                {
                    ProductId = finishedProduct.Id, ProductCode = finishedProduct.Code, ProductName = finishedProduct.Name,
                    BatchNo = dto.BatchNo, ExpiryDate = dto.ExpiryDate
                };
                var run = new ProductionRun(EntityName, order.Id, order.OrderNo, dto.OrderDate, Msg("EntryDescription", order.OrderNo, finishedProduct.Name),
                    finished, dto.Qty, dto.RawWarehouseId, dto.FinishedWarehouseId, components, byProducts,
                    operations.Select(o => InventoryCosting.OperationCost(o.Qty, o.UnitCost)).ToList(), inventory.Value, overhead.Value);

                var cost = _unbuild ? _production.Unbuild(db, run) : _production.Produce(db, run);
                if (cost.IsFailure) throw new InvalidOperationException(cost.ErrorMessage);

                order.MaterialsCost = cost.Value.Materials;
                order.OperationsCost = cost.Value.Operations;
                order.TotalCost = cost.Value.Total;
                order.JournalEntryId = cost.Value.EntryId;
                _repo.Update(db, order);

                foreach (var line in stocked.Value.Concat(operations).OrderBy(l => l.LineNo)) _repo.InsertLine(db, order.Id, line);
                return order.Id;
            });
        }

        protected override void Remove(PrimeDbContext db, ProductionOrder head)
        {
            Posting.Reverse(Journals, db, head.JournalEntryId);
            Stock.RemoveMovements(db, StockSource, head.Id);
            _repo.DeleteDocument(db, head.Id);
        }
    }

    public class UnbuildService : ProductionOrderService, IUnbuildService
    {
        public UnbuildService(IProductionOrderRepository repo, IBillOfMaterialsRepository boms, IProductRepository products,
            ILookupRepository<WorkCenter> centers, INumberSequenceService numbers, AccountOf accountsOf, Production production,
            IStockMove stock, Entries journals, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(repo, boms, products, centers, numbers, accountsOf, production, stock, journals, permissions, settings, localization, audit, unbuild: true) { }
    }
}
