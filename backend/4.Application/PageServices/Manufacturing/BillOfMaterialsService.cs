using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Manufacturing;
using PrimeERP.Application.PageServices.Documents;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.PageServices.Manufacturing
{
    public interface IBillOfMaterialsService
    {
        Result<PagedResult<BomDto>> GetPaged(int page, int pageSize, BomFilter filter = null);
        Result<BomDetailDto> GetById(int id);
        Result<BomDetailDto> Create(CreateBomDto dto);
        Result Update(CreateBomDto dto);
        Result Delete(int id);
        Result<List<BillOfMaterials>> Active();
    }

    public class BillOfMaterialsService : DocumentService<BillOfMaterials, BomDto, BomDetailDto, CreateBomDto, BomFilter>, IBillOfMaterialsService
    {
        private readonly IBillOfMaterialsRepository _repo;
        private readonly IProductRepository _products;
        private readonly INumberSequenceService _numbers;

        public BillOfMaterialsService(IBillOfMaterialsRepository repo, IProductRepository products, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo; _products = products; _numbers = numbers;
        }

        protected override string PermissionPrefix => "Manufacturing";
        protected override string StringPrefix => "Str.Bom";
        protected override string EntityName => "Bom";
        protected override bool Editable => true;

        private static readonly Field<CreateBomDto>[] Rules =
        {
            new(x => x.Name, "Str.Name", Required: true, Max: 200),
            new(x => x.ProductCode, "Str.Bom.Product", Required: true),
            new(x => x.Qty, "Str.Qty", Positive: true),
            new(x => x.Lines, "", Must: d => d.Lines.All(l => l.Kind != (int)BomLineKind.Operation || l.WorkCenterId > 0), Message: "Str.Bom.OperationNeedsCenter"),
            new(x => x.Lines, "", Must: d => d.Lines.All(l => l.ProductCode != d.ProductCode), Message: "Str.Bom.SelfComponent"),
        };

        public Result<List<BillOfMaterials>> Active() => CanDo("View") ? Ok(_repo.GetAll()) : FailDenied<List<BillOfMaterials>>();

        protected override BillOfMaterials FindHead(int id) => _repo.GetById(id);
        protected override int IdOf(BillOfMaterials head) => head.Id;
        protected override int IdOf(CreateBomDto dto) => dto.Id;
        protected override object AuditValue(BillOfMaterials head) => new { head?.Code, head?.ProductCode };

        protected override Result Guard(BillOfMaterials head) =>
            _repo.UsedByOrders(head.Id) ? Fail("InUse", ErrorCode.ValidationFailed) : Result.Ok();

        protected override (List<BillOfMaterials> Items, int Total) FindPage(int page, int pageSize, BomFilter filter)
        {
            filter ??= new BomFilter();
            return _repo.GetPaged(page, pageSize, filter.SearchText, filter.SortBy, filter.SortDescending);
        }

        protected override List<BomDto> ToRows(List<BillOfMaterials> heads) => heads.Select(h => Rows.Copy(h, new BomDto())).ToList();

        protected override BomDetailDto ToDetail(BillOfMaterials head)
        {
            var detail = Rows.Copy(head, new BomDetailDto());
            detail.Lines = _repo.GetLines(head.Id).Select(l => Rows.Copy(l, new BomLineDto(), to => to.Kind = (int)l.Kind)).ToList();
            return detail;
        }

        protected override Result<Func<PrimeDbContext, int>> Plan(CreateBomDto dto)
        {
            var valid = Check.Valid(dto, Rules).Then(() => DocumentLines.Check(dto.Lines, l => l.Qty));
            if (valid.IsFailure) return valid.As<Func<PrimeDbContext, int>>();

            var product = _products.GetByCode(dto.ProductCode);
            if (product == null) return Fail<Func<PrimeDbContext, int>>("ProductNotFound", ErrorCode.ValidationFailed, dto.ProductCode);

            var stocked = ProductLines.Resolve(_products, dto.Lines.Where(l => l.Kind != (int)BomLineKind.Operation), l => l.ProductCode,
                (l, _, _) => Result.Ok(Rows.Copy(l, new BomLine(), to => to.Kind = (BomLineKind)l.Kind)));
            if (stocked.IsFailure) return stocked.As<Func<PrimeDbContext, int>>();

            var lines = stocked.Value
                .Concat(dto.Lines.Where(l => l.Kind == (int)BomLineKind.Operation)
                    .Select(l => Rows.Copy(l, new BomLine(), to => { to.Kind = BomLineKind.Operation; to.ProductCode = null; })))
                .OrderBy(l => l.LineNo).ToList();

            var code = dto.Id > 0 ? _repo.GetById(dto.Id)?.Code : null;

            return Result.Ok<Func<PrimeDbContext, int>>(db =>
            {
                var head = Rows.Copy(dto, new BillOfMaterials(), to =>
                {
                    to.Code = code ?? _numbers.Next(db, "Bom");
                    to.ProductId = product.Id; to.ProductCode = product.Code; to.ProductName = product.Name;
                });
                var id = _repo.InsertHeader(db, head);
                foreach (var line in lines) _repo.InsertLine(db, id, line);
                return id;
            });
        }

        protected override void Remove(PrimeDbContext db, BillOfMaterials head) => _repo.DeleteDocument(db, head.Id);
    }
}
