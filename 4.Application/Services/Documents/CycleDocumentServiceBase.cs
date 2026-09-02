using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Permissions;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services.Documents
{
    public interface ICycleDocumentService
    {
        Result<PagedResult<CycleDocumentDto>> GetPaged(int page, int pageSize, CycleDocumentFilter filter = null);
        Result<CycleDocumentDetailDto> GetById(int id);
        Result<CycleDocumentDetailDto> Create(CreateCycleDocumentDto dto);
        Result Update(CreateCycleDocumentDto dto);
        Result Delete(int id);
    }

    public interface IPurchaseRequestService : ICycleDocumentService { }
    public interface IPurchaseOrderService : ICycleDocumentService { }
    public interface IQuotationService : ICycleDocumentService { }
    public interface ISalesOrderService : ICycleDocumentService { }

    // نسخة CycleDocument من StockAdjustmentServiceBase: بلا ترحيل وبلا أثر مخزني، وطرف اختياري بدل مخزن.
    public abstract class CycleDocumentServiceBase<TRepo> : ICycleDocumentService where TRepo : ICycleDocumentRepository
    {
        protected readonly TRepo Repo;
        private readonly IProductRepository _products;
        private readonly INumberSequenceService _numbers;
        private readonly IPermissionService _permissions;
        private readonly IAuditLogger _audit;
        private readonly string _sequenceKey, _permissionPrefix, _entityName;
        private readonly bool _partyRequired;

        protected CycleDocumentServiceBase(TRepo repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, IAuditLogger audit, string sequenceKey, string permissionPrefix,
            string entityName, bool partyRequired)
        {
            Repo = repo; _products = products; _numbers = numbers; _permissions = permissions; _audit = audit;
            _sequenceKey = sequenceKey; _permissionPrefix = permissionPrefix; _entityName = entityName; _partyRequired = partyRequired;
        }

        private bool Can(string action) => _permissions.Can($"{_permissionPrefix}.{action}");

        public Result<PagedResult<CycleDocumentDto>> GetPaged(int page, int pageSize, CycleDocumentFilter filter = null)
        {
            if (!Can("View")) return Result.Fail<PagedResult<CycleDocumentDto>>("لا صلاحية", ErrorCode.Unauthorized);
            filter ??= new CycleDocumentFilter();

            var (items, total) = Repo.GetPaged(page, pageSize, filter.SearchText, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<CycleDocumentDto>
            { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<CycleDocumentDetailDto> GetById(int id)
        {
            if (!Can("View")) return Result.Fail<CycleDocumentDetailDto>("لا صلاحية", ErrorCode.Unauthorized);

            var doc = Repo.GetById(id);
            if (doc == null) return Result.Fail<CycleDocumentDetailDto>("المستند غير موجود", ErrorCode.NotFound);

            var lines = Repo.GetLines(id);
            return Result.Ok(new CycleDocumentDetailDto
            {
                Id = doc.Id, DocNo = doc.DocNo, DocDate = doc.DocDate, PartyId = doc.PartyId,
                TotalQty = lines.Sum(l => l.Qty), Total = lines.Sum(l => l.Qty * l.UnitPrice), CreatedAt = doc.CreatedAt,
                Lines = lines.Select(l => new CycleDocumentLineDto
                {
                    LineNo = l.LineNo, ProductCode = l.ProductCode, ProductName = l.ProductName,
                    Qty = l.Qty, UnitPrice = l.UnitPrice, Notes = l.Notes
                }).ToList()
            });
        }

        public Result<CycleDocumentDetailDto> Create(CreateCycleDocumentDto dto)
        {
            if (!Can("Create")) return Result.Fail<CycleDocumentDetailDto>("لا صلاحية", ErrorCode.Unauthorized);
            if (dto.Lines == null || dto.Lines.Count == 0)
                return Result.Fail<CycleDocumentDetailDto>("المستند يحتاج سطراً واحداً على الأقل", ErrorCode.ValidationFailed);
            if (_partyRequired && dto.PartyId == null)
                return Result.Fail<CycleDocumentDetailDto>("الطرف إلزامي في هذا المستند", ErrorCode.ValidationFailed);

            var resolved = new List<CycleDocumentLine>();
            var lineNo = 1;
            foreach (var l in dto.Lines)
            {
                var product = _products.GetByCode(l.ProductCode);
                if (product == null)
                    return Result.Fail<CycleDocumentDetailDto>($"الصنف بالكود {l.ProductCode} غير موجود", ErrorCode.ValidationFailed);
                if (l.Qty <= 0)
                    return Result.Fail<CycleDocumentDetailDto>("الكمية يجب أن تكون أكبر من صفر", ErrorCode.ValidationFailed);

                resolved.Add(new CycleDocumentLine
                {
                    LineNo = lineNo++, ProductId = product.Id, ProductCode = product.Code, ProductName = product.Name,
                    Qty = l.Qty, UnitPrice = l.UnitPrice, Notes = l.Notes
                });
            }

            var docId = Db.RunTransaction((conn, tx) =>
            {
                var doc = new CycleDocument
                {
                    DocNo = _numbers.Next(conn, tx, _sequenceKey),
                    DocDate = dto.DocDate,
                    PartyId = dto.PartyId,
                    Notes = dto.Notes,
                    CreatedBy = AppSession.Username
                };

                var id = Repo.InsertHeader(conn, tx, doc);
                foreach (var line in resolved) Repo.InsertLine(conn, tx, id, line);
                return id;
            });

            _audit.Log(_entityName, docId, AuditAction.Insert, newValue: new { dto.PartyId, LineCount = resolved.Count });
            return GetById(docId);
        }

        public Result Update(CreateCycleDocumentDto dto)
        {
            if (!Can("Edit")) return Result.Fail("لا صلاحية", ErrorCode.Unauthorized);
            if (Repo.GetById(dto.Id) == null) return Result.Fail("المستند غير موجود", ErrorCode.NotFound);

            Repo.DeleteDocument(dto.Id);
            var recreated = Create(dto);
            return recreated.IsSuccess ? Result.Ok() : Result.Fail(recreated.ErrorMessage, recreated.ErrorCode);
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return Result.Fail("لا صلاحية", ErrorCode.Unauthorized);
            if (Repo.GetById(id) == null) return Result.Fail("المستند غير موجود", ErrorCode.NotFound);

            Repo.DeleteDocument(id);
            _audit.Log(_entityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private CycleDocumentDto ToDto(CycleDocument d)
        {
            var lines = Repo.GetLines(d.Id);
            return new CycleDocumentDto
            {
                Id = d.Id, DocNo = d.DocNo, DocDate = d.DocDate, PartyId = d.PartyId,
                TotalQty = lines.Sum(l => l.Qty), Total = lines.Sum(l => l.Qty * l.UnitPrice), CreatedAt = d.CreatedAt
            };
        }
    }

    public class PurchaseRequestService : CycleDocumentServiceBase<IPurchaseRequestRepository>, IPurchaseRequestService
    {
        public PurchaseRequestService(IPurchaseRequestRepository repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, IAuditLogger audit)
            : base(repo, products, numbers, permissions, audit, "PurchaseRequest", "Purchases", "PurchaseRequest", partyRequired: false) { }
    }

    public class PurchaseOrderService : CycleDocumentServiceBase<IPurchaseOrderRepository>, IPurchaseOrderService
    {
        public PurchaseOrderService(IPurchaseOrderRepository repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, IAuditLogger audit)
            : base(repo, products, numbers, permissions, audit, "PurchaseOrder", "Purchases", "PurchaseOrder", partyRequired: true) { }
    }

    public class QuotationService : CycleDocumentServiceBase<IQuotationRepository>, IQuotationService
    {
        public QuotationService(IQuotationRepository repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, IAuditLogger audit)
            : base(repo, products, numbers, permissions, audit, "Quotation", "Sales", "Quotation", partyRequired: false) { }
    }

    public class SalesOrderService : CycleDocumentServiceBase<ISalesOrderRepository>, ISalesOrderService
    {
        public SalesOrderService(ISalesOrderRepository repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, IAuditLogger audit)
            : base(repo, products, numbers, permissions, audit, "SalesOrder", "Sales", "SalesOrder", partyRequired: true) { }
    }
}
