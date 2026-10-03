using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Application.Legacy.Documents;
using PrimeERP.Data.Core;
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
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Inventory
{
    /// <summary>التحويل بين المخازن</summary>
    public class StockTransferService
        : DocumentService<StockTransferDocument, StockTransferDto, StockTransferDetailDto, CreateStockTransferDto, StockTransferFilter>,
          IStockTransferService
    {
        private readonly IStockTransferRepository _repo;
        private readonly IProductRepository _products;
        private readonly ILookupRepository<Warehouse> _warehouses;
        private readonly INumberSequenceService _numbers;

        public StockTransferService(IStockTransferRepository repo, IProductRepository products, ILookupRepository<Warehouse> warehouses,
            IStockMove stock, INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit, stock: stock)
        {
            _repo = repo; _products = products; _warehouses = warehouses; _numbers = numbers;
        }

        protected override string PermissionPrefix => "Inventory";
        protected override string StringPrefix => "Str.StockTransfer";
        protected override string EntityName => "StockTransfer";
        protected override string StockSource => EntityName;

        protected override bool CanDo(string action) => Permissions.Can("Inventory.Transfer");

        protected override StockTransferDocument FindHead(int id) => _repo.GetById(id);
        protected override int IdOf(StockTransferDocument head) => head.Id;
        protected override object AuditValue(StockTransferDocument head) => new { head?.FromWarehouseId, head?.ToWarehouseId };

        protected override (List<StockTransferDocument> Items, int Total) FindPage(int page, int pageSize, StockTransferFilter filter)
        {
            filter ??= new StockTransferFilter();
            return _repo.GetPaged(page, pageSize, filter.SearchText, filter.SortBy, filter.SortDescending);
        }

        protected override List<StockTransferDto> ToRows(List<StockTransferDocument> heads)
        {
            var warehouses = _warehouses.NamesOf(heads.SelectMany(d => new[] { d.FromWarehouseId, d.ToWarehouseId }));
            return heads.Select(d => ToDto<StockTransferDto>(d, warehouses)).ToList();
        }

        protected override StockTransferDetailDto ToDetail(StockTransferDocument head)
        {
            var detail = ToDto<StockTransferDetailDto>(head, _warehouses.NamesOf(new[] { head.FromWarehouseId, head.ToWarehouseId }));
            detail.Lines = _repo.GetLines(head.Id).Select(l => Rows.Copy(l, new StockTransferLineDto())).ToList();
            return detail;
        }

        protected override Result<Func<PrimeDbContext, int>> Plan(CreateStockTransferDto dto)
        {
            var shape = DocumentLines.Check(dto.Lines, l => l.Qty);
            if (shape.IsFailure) return shape.As<Func<PrimeDbContext, int>>();
            var warehouses = Check.Valid(dto, new Field<CreateStockTransferDto>(x => x.ToWarehouseId, "",
                Must: d => d.FromWarehouseId != d.ToWarehouseId, Message: "Str.StockTransfer.SameWarehouse"));
            if (warehouses.IsFailure) return warehouses.As<Func<PrimeDbContext, int>>();

            var lines = ProductLines.Resolve(_products, dto.Lines, l => l.ProductCode, (l, product, _) => Result.Ok(Rows.Copy(l, new StockTransferLine())));
            if (lines.IsFailure) return lines.As<Func<PrimeDbContext, int>>();
            var resolved = lines.Value;

            return Result.Ok<Func<PrimeDbContext, int>>(db =>
            {
                var docNo = _numbers.Next(db, "StockTransfer");
                var id = _repo.InsertHeader(db, Rows.Copy(dto, new StockTransferDocument(), to =>
                {
                    to.DocNo = docNo;
                }));

                foreach (var line in resolved)
                {
                    _repo.InsertLine(db, id, line);
                    foreach (var (warehouse, direction) in new[] { (dto.FromWarehouseId, MovementType.Out), (dto.ToWarehouseId, MovementType.In) })
                        Move(db, line, warehouse, direction, id, docNo, dto.MovementDate);
                }
                return id;
            });
        }

        protected override void Remove(PrimeDbContext db, StockTransferDocument head)
        {
            Stock.RemoveMovements(db, StockSource, head.Id);
            _repo.DeleteDocument(db, head.Id);
        }

        private void Move(PrimeDbContext db, StockTransferLine line, int warehouseId, MovementType direction, int id, string docNo, DateTime date)
        {
            var moved = Stock.RecordMovement(db, line.ProductId, warehouseId, direction, line.Qty, 0, StockSource, id, docNo, date);
            if (moved.IsFailure) throw new InvalidOperationException(moved.ErrorMessage);
        }

        private static T ToDto<T>(StockTransferDocument d, IReadOnlyDictionary<int, string> warehouses) where T : StockTransferDto, new() => Rows.Copy<T>(d, new(), to =>
        {
            to.FromWarehouseName = warehouses.GetValueOrDefault(d.FromWarehouseId);
            to.ToWarehouseName = warehouses.GetValueOrDefault(d.ToWarehouseId);
        });
    }
}
