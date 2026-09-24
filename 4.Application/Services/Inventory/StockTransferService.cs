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

namespace PrimeERP.Application.Services.Inventory
{
    /// <summary>التحويل بين المخازن</summary>
    public class StockTransferService : ServiceBase, IStockTransferService
    {
        private readonly IStockTransferRepository _repo;
        private readonly IProductRepository _products;
        private readonly IWarehouseService _warehouses;
        private readonly IStockService _stock;
        private readonly INumberSequenceService _numbers;

        public StockTransferService(IStockTransferRepository repo, IProductRepository products, IWarehouseService warehouses,
            IStockService stock, INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo; _products = products; _warehouses = warehouses; _stock = stock; _numbers = numbers;
        }

        protected override string PermissionPrefix => "Inventory";
        protected override string StringPrefix => "Str.StockTransfer";
        protected override string EntityName => "StockTransfers";

        private bool Can() => Permissions.Can("Inventory.Transfer");

        public Result<PagedResult<StockTransferDto>> GetPaged(int page, int pageSize, StockTransferFilter filter = null)
        {
            if (!Can()) return FailDenied<PagedResult<StockTransferDto>>();
            filter ??= new StockTransferFilter();

            var (items, total) = _repo.GetPaged(page, pageSize, filter.SearchText, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<StockTransferDto> { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<StockTransferDetailDto> GetById(int id)
        {
            if (!Can()) return FailDenied<StockTransferDetailDto>();

            var doc = _repo.GetById(id);
            if (doc == null) return Result.Fail<StockTransferDetailDto>("المستند غير موجود", ErrorCode.NotFound);

            var baseDto = ToDto(doc);
            return Result.Ok(new StockTransferDetailDto
            {
                Id = baseDto.Id, DocNo = baseDto.DocNo, MovementDate = baseDto.MovementDate,
                FromWarehouseId = baseDto.FromWarehouseId, FromWarehouseName = baseDto.FromWarehouseName,
                ToWarehouseId = baseDto.ToWarehouseId, ToWarehouseName = baseDto.ToWarehouseName, CreatedAt = baseDto.CreatedAt,
                Lines = _repo.GetLines(id).Select(l => new StockTransferLineDto { LineNo = l.LineNo, ProductCode = l.ProductCode, ProductName = l.ProductName, Qty = l.Qty, Notes = l.Notes }).ToList()
            });
        }

        public Result<StockTransferDetailDto> Create(CreateStockTransferDto dto)
        {
            if (!Can()) return FailDenied<StockTransferDetailDto>();
            if (dto.Lines == null || dto.Lines.Count == 0) return Result.Fail<StockTransferDetailDto>("المستند يحتاج سطراً واحداً على الأقل", ErrorCode.ValidationFailed);
            if (dto.FromWarehouseId == dto.ToWarehouseId) return Result.Fail<StockTransferDetailDto>("المخزن المصدر والهدف لا يمكن أن يكونا نفس المخزن", ErrorCode.ValidationFailed);

            var resolvedLines = new List<StockTransferLine>();
            foreach (var l in dto.Lines)
            {
                var product = _products.GetByCode(l.ProductCode);
                if (product == null) return Result.Fail<StockTransferDetailDto>($"الصنف بالكود {l.ProductCode} غير موجود", ErrorCode.ValidationFailed);
                if (l.Qty <= 0) return Result.Fail<StockTransferDetailDto>("الكمية يجب أن تكون أكبر من صفر", ErrorCode.ValidationFailed);

                resolvedLines.Add(new StockTransferLine { LineNo = l.LineNo, ProductId = product.Id, ProductCode = product.Code, ProductName = product.Name, Qty = l.Qty, Notes = l.Notes });
            }

            int docId;
            try
            {
                docId = Tx(db =>
                {
                    var docNo = _numbers.Next(db, "StockTransfer");
                    var doc = new StockTransferDocument { DocNo = docNo, MovementDate = dto.MovementDate, FromWarehouseId = dto.FromWarehouseId, ToWarehouseId = dto.ToWarehouseId, Notes = dto.Notes, CreatedBy = AppSession.Username };
                    var id = _repo.InsertHeader(db, doc);

                    foreach (var line in resolvedLines)
                    {
                        _repo.InsertLine(db, id, line);

                        var outResult = _stock.RecordMovement(db, line.ProductId, dto.FromWarehouseId, MovementType.Out, line.Qty, 0, "StockTransfer", id, docNo, dto.MovementDate);
                        if (!outResult.IsSuccess) throw new InvalidOperationException(outResult.ErrorMessage);

                        var inResult = _stock.RecordMovement(db, line.ProductId, dto.ToWarehouseId, MovementType.In, line.Qty, 0, "StockTransfer", id, docNo, dto.MovementDate);
                        if (!inResult.IsSuccess) throw new InvalidOperationException(inResult.ErrorMessage);
                    }

                    return id;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<StockTransferDetailDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log("StockTransfer", docId, AuditAction.Insert, newValue: new { dto.FromWarehouseId, dto.ToWarehouseId, LineCount = resolvedLines.Count });
            return GetById(docId);
        }

        public Result Update(CreateStockTransferDto dto) => Result.Fail("المستند مُرحَّل فور إنشائه — لا يمكن تعديله", ErrorCode.ValidationFailed);

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();
            if (_repo.GetById(id) == null) return Result.Fail("المستند غير موجود", ErrorCode.NotFound);

            Tx(db =>
            {
                _stock.RemoveMovements(db, EntityName, id);
                _repo.DeleteDocument(db, id);
            });

            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private StockTransferDto ToDto(StockTransferDocument d)
        {
            var warehouses = _warehouses.GetAll().Value;
            return new StockTransferDto
            {
                Id = d.Id, DocNo = d.DocNo, MovementDate = d.MovementDate,
                FromWarehouseId = d.FromWarehouseId, FromWarehouseName = warehouses.FirstOrDefault(w => w.Id == d.FromWarehouseId)?.Name,
                ToWarehouseId = d.ToWarehouseId, ToWarehouseName = warehouses.FirstOrDefault(w => w.Id == d.ToWarehouseId)?.Name,
                CreatedAt = d.CreatedAt
            };
        }
    }
}
