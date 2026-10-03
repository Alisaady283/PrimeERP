using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System;
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

namespace PrimeERP.Application.Legacy.Documents
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

    public abstract class CycleDocumentServiceBase<TRepo>
        : DocumentService<CycleDocument, CycleDocumentDto, CycleDocumentDetailDto, CreateCycleDocumentDto, CycleDocumentFilter>, ICycleDocumentService
        where TRepo : ICycleDocumentRepository
    {
        protected readonly TRepo Repo;
        private readonly IProductRepository _products;
        private readonly INumberSequenceService _numbers;
        private readonly string _sequenceKey, _permissionPrefix, _entityName;
        private readonly bool _partyRequired;

        protected CycleDocumentServiceBase(TRepo repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            IDocumentPull links, string sequenceKey, string permissionPrefix, string entityName, bool partyRequired)
            : base(permissions, settings, localization, audit, links)
        {
            Repo = repo; _products = products; _numbers = numbers;
            _sequenceKey = sequenceKey; _permissionPrefix = permissionPrefix; _entityName = entityName; _partyRequired = partyRequired;
        }

        protected override string PermissionPrefix => _permissionPrefix;
        protected override string StringPrefix => "Str.Document";
        protected override string EntityName => _entityName;
        protected override bool Editable => true;
        protected override string PullType => _entityName;

        protected override CycleDocument FindHead(int id) => Repo.GetById(id);
        protected override int IdOf(CycleDocument head) => head.Id;
        protected override int IdOf(CreateCycleDocumentDto dto) => dto.Id;
        protected override object AuditValue(CycleDocument head) => new { head?.PartyId };

        protected override (List<CycleDocument> Items, int Total) FindPage(int page, int pageSize, CycleDocumentFilter filter)
        {
            filter ??= new CycleDocumentFilter();
            return Repo.GetPaged(page, pageSize, filter.SearchText, filter.SortBy, filter.SortDescending);
        }

        protected override List<CycleDocumentDto> ToRows(List<CycleDocument> heads)
        {
            var totals = Repo.Totals(heads.Select(d => d.Id));
            return heads.Select(d => ToDto<CycleDocumentDto>(d, totals.GetValueOrDefault(d.Id))).ToList();
        }

        protected override CycleDocumentDetailDto ToDetail(CycleDocument head)
        {
            var detail = ToDto<CycleDocumentDetailDto>(head, Repo.Totals(new[] { head.Id }).GetValueOrDefault(head.Id));
            detail.Lines = Repo.GetLines(head.Id).Select(l => Rows.Copy(l, new CycleDocumentLineDto())).ToList();
            Links.Attach(EntityName, head.Id, detail.Lines);
            return detail;
        }

        protected override Result<Func<PrimeDbContext, int>> Plan(CreateCycleDocumentDto dto)
        {
            var shape = DocumentLines.Check(dto.Lines, l => l.Qty);
            if (shape.IsFailure) return shape.As<Func<PrimeDbContext, int>>();
            var party = Check.Valid(dto, new Field<CreateCycleDocumentDto>(x => x.PartyId, "", Must: d => !_partyRequired || d.PartyId != null,
                Message: "Str.Document.PartyRequired"));
            if (party.IsFailure) return party.As<Func<PrimeDbContext, int>>();

            var lines = ProductLines.Resolve(_products, dto.Lines, l => l.ProductCode, (l, product, no) =>
                Result.Ok(Rows.Copy(l, new CycleDocumentLine(), to =>
                {
                    to.LineNo = no;
                })));
            if (lines.IsFailure) return lines.As<Func<PrimeDbContext, int>>();
            var pulls = Links.ValidatePulls(dto.Lines.Select(l => ((IPullableLine)l, l.Qty)), EntityName, dto.Id);
            if (pulls.IsFailure) return pulls.As<Func<PrimeDbContext, int>>();
            var resolved = lines.Value;

            return Result.Ok<Func<PrimeDbContext, int>>(db =>
            {
                var id = Repo.InsertHeader(db, Rows.Copy(dto, new CycleDocument(), to =>
                {
                    to.DocNo = _numbers.Next(db, _sequenceKey);
                }));

                var inserted = resolved.Select((line, i) => ((IPullableLine)dto.Lines[i], Repo.InsertLine(db, id, line), line.Qty)).ToList();
                Links.RecordPulls(db, _entityName, id, inserted);
                return id;
            });
        }

        protected override void Remove(PrimeDbContext db, CycleDocument head)
        {
            Links.RemovePull(_entityName, head.Id, db);
            Repo.DeleteDocument(db, head.Id);
        }

        private static T ToDto<T>(CycleDocument d, (decimal Qty, decimal Total) totals) where T : CycleDocumentDto, new() => Rows.Copy<T>(d, new(), to =>
        {
            to.TotalQty = totals.Qty;
            to.Total = totals.Total;
        });
    }

    public class PurchaseRequestService : CycleDocumentServiceBase<IPurchaseRequestRepository>, IPurchaseRequestService
    {
        public PurchaseRequestService(IPurchaseRequestRepository repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IDocumentPull links)
            : base(repo, products, numbers, permissions, settings, localization, audit, links, "PurchaseRequest", "Purchases", "PurchaseRequest", partyRequired: false) { }
    }

    public class PurchaseOrderService : CycleDocumentServiceBase<IPurchaseOrderRepository>, IPurchaseOrderService
    {
        public PurchaseOrderService(IPurchaseOrderRepository repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IDocumentPull links)
            : base(repo, products, numbers, permissions, settings, localization, audit, links, "PurchaseOrder", "Purchases", "PurchaseOrder", partyRequired: true) { }
    }

    public class QuotationService : CycleDocumentServiceBase<IQuotationRepository>, IQuotationService
    {
        public QuotationService(IQuotationRepository repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IDocumentPull links)
            : base(repo, products, numbers, permissions, settings, localization, audit, links, "Quotation", "Sales", "Quotation", partyRequired: false) { }
    }

    public class SalesOrderService : CycleDocumentServiceBase<ISalesOrderRepository>, ISalesOrderService
    {
        public SalesOrderService(ISalesOrderRepository repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IDocumentPull links)
            : base(repo, products, numbers, permissions, settings, localization, audit, links, "SalesOrder", "Sales", "SalesOrder", partyRequired: true) { }
    }
}
