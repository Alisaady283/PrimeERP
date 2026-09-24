using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Services.Documents
{
    /// <summary>مستندات الدورة: أساسٌ وستّ خدمات</summary>
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

    public abstract class CycleDocumentServiceBase<TRepo> : ServiceBase, ICycleDocumentService where TRepo : ICycleDocumentRepository
    {
        protected readonly TRepo Repo;
        private readonly IProductRepository _products;
        private readonly INumberSequenceService _numbers;
        private readonly IDocumentLinkService _links;
        private readonly string _sequenceKey, _permissionPrefix, _entityName;
        private readonly bool _partyRequired;

        protected CycleDocumentServiceBase(TRepo repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            IDocumentLinkService links, string sequenceKey, string permissionPrefix, string entityName, bool partyRequired)
            : base(permissions, settings, localization, audit)
        {
            Repo = repo; _products = products; _numbers = numbers; _links = links;
            _sequenceKey = sequenceKey; _permissionPrefix = permissionPrefix; _entityName = entityName; _partyRequired = partyRequired;
        }

        protected override string PermissionPrefix => _permissionPrefix;
        protected override string StringPrefix => "Str.Document";
        protected override string EntityName => _entityName;

        public Result<PagedResult<CycleDocumentDto>> GetPaged(int page, int pageSize, CycleDocumentFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<CycleDocumentDto>>();
            filter ??= new CycleDocumentFilter();

            var (items, total) = Repo.GetPaged(page, pageSize, filter.SearchText, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<CycleDocumentDto>
            { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<CycleDocumentDetailDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<CycleDocumentDetailDto>();

            var doc = Repo.GetById(id);
            if (doc == null) return Result.Fail<CycleDocumentDetailDto>("المستند غير موجود", ErrorCode.NotFound);

            var lines = Repo.GetLines(id);
            return Result.Ok(new CycleDocumentDetailDto
            {
                Id = doc.Id, DocNo = doc.DocNo, DocDate = doc.DocDate, PartyId = doc.PartyId,
                TotalQty = lines.Sum(l => l.Qty), Total = lines.Sum(l => l.Qty * l.UnitPrice), CreatedAt = doc.CreatedAt,
                Lines = lines.Select(l => new CycleDocumentLineDto
                {
                    Id = l.Id, LineNo = l.LineNo, ProductCode = l.ProductCode, ProductName = l.ProductName,
                    Qty = l.Qty, UnitPrice = l.UnitPrice, Notes = l.Notes
                }).ToList()
            });
        }

        public Result<CycleDocumentDetailDto> Create(CreateCycleDocumentDto dto)
        {
            if (!Can("Create")) return FailDenied<CycleDocumentDetailDto>();
            if (dto.Lines == null || dto.Lines.Count == 0)
                return Result.Fail<CycleDocumentDetailDto>("المستند يحتاج سطراً واحداً على الأقل", ErrorCode.ValidationFailed);
            if (_partyRequired && dto.PartyId == null)
                return Result.Fail<CycleDocumentDetailDto>("الطرف إلزامي في هذا المستند", ErrorCode.ValidationFailed);

            var resolved = new List<CycleDocumentLine>();
            var pulls = new List<CreateCycleDocumentLineDto>();
            var lineNo = 1;
            foreach (var l in dto.Lines)
            {
                var product = _products.GetByCode(l.ProductCode);
                if (product == null)
                    return Result.Fail<CycleDocumentDetailDto>($"الصنف بالكود {l.ProductCode} غير موجود", ErrorCode.ValidationFailed);
                if (l.Qty <= 0)
                    return Result.Fail<CycleDocumentDetailDto>("الكمية يجب أن تكون أكبر من صفر", ErrorCode.ValidationFailed);

                var pullCheck = _links.ValidatePull(l.SourceType, l.SourceId, l.SourceLineId, l.Qty, l.SourceNo);
                if (pullCheck.IsFailure)
                    return Result.Fail<CycleDocumentDetailDto>(pullCheck.ErrorMessage, pullCheck.ErrorCode);

                resolved.Add(new CycleDocumentLine
                {
                    LineNo = lineNo++, ProductId = product.Id, ProductCode = product.Code, ProductName = product.Name,
                    Qty = l.Qty, UnitPrice = l.UnitPrice, Notes = l.Notes
                });
                pulls.Add(l);
            }

            var docId = Tx(db =>
            {
                var doc = new CycleDocument
                {
                    DocNo = _numbers.Next(db, _sequenceKey),
                    DocDate = dto.DocDate,
                    PartyId = dto.PartyId,
                    Notes = dto.Notes,
                    CreatedBy = AppSession.Username
                };

                var id = Repo.InsertHeader(db, doc);
                var inserted = new List<(IPullableLine Line, int TargetLineId, decimal Qty)>();
                for (int i = 0; i < resolved.Count; i++)
                    inserted.Add((pulls[i], Repo.InsertLine(db, id, resolved[i]), resolved[i].Qty));

                _links.RecordPulls(db, _entityName, id, inserted);
                return id;
            });

            Audit.Log(_entityName, docId, AuditAction.Insert, newValue: new { dto.PartyId, LineCount = resolved.Count });
            return GetById(docId);
        }

        public Result Update(CreateCycleDocumentDto dto)
        {
            if (!Can("Edit")) return FailDenied();
            if (Repo.GetById(dto.Id) == null) return Result.Fail("المستند غير موجود", ErrorCode.NotFound);

            DeleteWithLinks(dto.Id);
            var recreated = Create(dto);
            return recreated.IsSuccess ? Result.Ok() : Result.Fail(recreated.ErrorMessage, recreated.ErrorCode);
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();
            if (Repo.GetById(id) == null) return Result.Fail("المستند غير موجود", ErrorCode.NotFound);

            if (_links.GetPulledBySource(_entityName, id).Count > 0)
                return Result.Fail("سُحب من هذا المستند — احذف ما سُحب إليه أولاً", ErrorCode.ValidationFailed);

            DeleteWithLinks(id);
            Audit.Log(_entityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private void DeleteWithLinks(int id) => Tx(db =>
        {
            _links.RemovePull(_entityName, id, db);
            Repo.DeleteDocument(db, id);
        });

        private CycleDocumentDto ToDto(CycleDocument d)
        {
            var lines = Repo.GetLines(d.Id);
            return new CycleDocumentDto
            {
                Id = d.Id, DocNo = d.DocNo, DocDate = d.DocDate, PartyId = d.PartyId,
                TotalQty = lines.Sum(l => l.Qty), Total = lines.Sum(l => l.Qty * l.UnitPrice),
                Notes = d.Notes, CreatedAt = d.CreatedAt
            };
        }
    }

    public class PurchaseRequestService : CycleDocumentServiceBase<IPurchaseRequestRepository>, IPurchaseRequestService
    {
        public PurchaseRequestService(IPurchaseRequestRepository repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IDocumentLinkService links)
            : base(repo, products, numbers, permissions, settings, localization, audit, links, "PurchaseRequest", "Purchases", "PurchaseRequest", partyRequired: false) { }
    }

    public class PurchaseOrderService : CycleDocumentServiceBase<IPurchaseOrderRepository>, IPurchaseOrderService
    {
        public PurchaseOrderService(IPurchaseOrderRepository repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IDocumentLinkService links)
            : base(repo, products, numbers, permissions, settings, localization, audit, links, "PurchaseOrder", "Purchases", "PurchaseOrder", partyRequired: true) { }
    }

    public class QuotationService : CycleDocumentServiceBase<IQuotationRepository>, IQuotationService
    {
        public QuotationService(IQuotationRepository repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IDocumentLinkService links)
            : base(repo, products, numbers, permissions, settings, localization, audit, links, "Quotation", "Sales", "Quotation", partyRequired: false) { }
    }

    public class SalesOrderService : CycleDocumentServiceBase<ISalesOrderRepository>, ISalesOrderService
    {
        public SalesOrderService(ISalesOrderRepository repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IDocumentLinkService links)
            : base(repo, products, numbers, permissions, settings, localization, audit, links, "SalesOrder", "Sales", "SalesOrder", partyRequired: true) { }
    }
}
