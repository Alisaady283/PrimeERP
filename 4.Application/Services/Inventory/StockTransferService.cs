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
    // كل سطر = حركتا مخزون (Out من المصدر، In للهدف) بنفس المعاملة — بلا ترحيل محاسبي (نقل داخلي، لا قيمة
    // مالية جديدة). يُعيد استخدام IStockService.RecordMovement مباشرة، لا Transfer(product,...) ذات المنتج
    // الواحد (تلك تخدم نداءً برمجياً مباشراً، هنا مستند متعدد السطور).
    public class StockTransferService : IStockTransferService
    {
        private readonly IStockTransferRepository _repo;
        private readonly IProductRepository _products;
        private readonly IWarehouseService _warehouses;
        private readonly IStockService _stock;
        private readonly INumberSequenceService _numbers;
        private readonly IPermissionService _permissions;
        private readonly IAuditLogger _audit;

        public StockTransferService(IStockTransferRepository repo, IProductRepository products, IWarehouseService warehouses,
            IStockService stock, INumberSequenceService numbers, IPermissionService permissions, IAuditLogger audit)
        {
            _repo = repo; _products = products; _warehouses = warehouses; _stock = stock; _numbers = numbers; _permissions = permissions; _audit = audit;
        }

        private bool Can() => _permissions.Can("Inventory.Transfer");

        public Result<PagedResult<StockTransferDto>> GetPaged(int page, int pageSize, StockTransferFilter filter = null)
        {
            if (!Can()) return Result.Fail<PagedResult<StockTransferDto>>("لا صلاحية", ErrorCode.Unauthorized);
            filter ??= new StockTransferFilter();

            var (items, total) = _repo.GetPaged(page, pageSize, filter.SearchText, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<StockTransferDto> { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<StockTransferDetailDto> GetById(int id)
        {
            if (!Can()) return Result.Fail<StockTransferDetailDto>("لا صلاحية", ErrorCode.Unauthorized);

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
            if (!Can()) return Result.Fail<StockTransferDetailDto>("لا صلاحية", ErrorCode.Unauthorized);
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
                docId = Db.RunTransaction((conn, tx) =>
                {
                    var docNo = _numbers.Next(conn, tx, "StockTransfer");
                    var doc = new StockTransferDocument { DocNo = docNo, MovementDate = dto.MovementDate, FromWarehouseId = dto.FromWarehouseId, ToWarehouseId = dto.ToWarehouseId, Notes = dto.Notes, CreatedBy = AppSession.Username };
                    var id = _repo.InsertHeader(conn, tx, doc);

                    foreach (var line in resolvedLines)
                    {
                        _repo.InsertLine(conn, tx, id, line);

                        var outResult = _stock.RecordMovement(conn, tx, line.ProductId, dto.FromWarehouseId, MovementType.Out, line.Qty, 0, "StockTransfer", id, docNo, dto.MovementDate);
                        if (!outResult.IsSuccess) throw new InvalidOperationException(outResult.ErrorMessage);

                        var inResult = _stock.RecordMovement(conn, tx, line.ProductId, dto.ToWarehouseId, MovementType.In, line.Qty, 0, "StockTransfer", id, docNo, dto.MovementDate);
                        if (!inResult.IsSuccess) throw new InvalidOperationException(inResult.ErrorMessage);
                    }

                    return id;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<StockTransferDetailDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            _audit.Log("StockTransfer", docId, AuditAction.Insert, newValue: new { dto.FromWarehouseId, dto.ToWarehouseId, LineCount = resolvedLines.Count });
            return GetById(docId);
        }

        public Result Update(CreateStockTransferDto dto) => Result.Fail("المستند مُرحَّل فور إنشائه — لا يمكن تعديله", ErrorCode.ValidationFailed);

        public Result Delete(int id) => Result.Fail("المستند مُرحَّل فور إنشائه — لا يمكن حذفه", ErrorCode.ValidationFailed);

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
