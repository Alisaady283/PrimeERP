using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Helpers;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Purchasing
{
    /// <summary>مرتجع الشراء وقيده</summary>
    public class PurchaseReturnService : ServiceBase, IPurchaseReturnService
    {
        private readonly IReturnRepository<PurchaseReturn, PurchaseReturnLine> _returns;
        private readonly IProductRepository _products;
        private readonly ISupplierService _suppliers;
        private readonly IStockService _stock;
        private readonly IJournalService _journal;
        private readonly INumberSequenceService _numbers;

        private readonly PrimeERP.Application.Services.Documents.IDocumentLinkService _links;

        public PurchaseReturnService(IReturnRepository<PurchaseReturn, PurchaseReturnLine> returns, IProductRepository products, ISupplierService suppliers,
            IStockService stock, IJournalService journal, INumberSequenceService numbers,
            PrimeERP.Application.Services.Documents.IDocumentLinkService links,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _returns = returns; _products = products; _suppliers = suppliers;
            _stock = stock; _journal = journal; _numbers = numbers; _links = links;
        }

        protected override string PermissionPrefix => "Purchases";
        protected override string StringPrefix => "Str.PurchaseReturn";
        protected override string EntityName => "PurchaseReturns";


        public Result<PagedResult<PurchaseReturnDto>> GetPaged(int page, int pageSize, PurchaseReturnFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<PurchaseReturnDto>>();
            filter ??= new PurchaseReturnFilter();

            var (items, total) = _returns.GetPaged(page, pageSize, filter.SearchText, filter.SupplierId, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<PurchaseReturnDto> { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<PurchaseReturnDetailDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<PurchaseReturnDetailDto>();

            var ret = _returns.GetById(id);
            if (ret == null) return Result.Fail<PurchaseReturnDetailDto>("المرتجع غير موجود", ErrorCode.NotFound);

            var baseDto = ToDto(ret);
            return Result.Ok(new PurchaseReturnDetailDto
            {
                Id = baseDto.Id, ReturnNo = baseDto.ReturnNo, ReturnDate = baseDto.ReturnDate, SupplierId = baseDto.SupplierId,
                SupplierName = baseDto.SupplierName, WarehouseId = baseDto.WarehouseId,
                SubTotal = baseDto.SubTotal, DiscountAmount = baseDto.DiscountAmount, VatAmount = baseDto.VatAmount,
                WithholdingAmount = baseDto.WithholdingAmount, NetTotal = baseDto.NetTotal, CreatedAt = baseDto.CreatedAt,
                Lines = _returns.GetLines(id).Select(l => new PurchaseReturnLineDto
                {
                    LineNo = l.LineNo, ProductCode = l.ProductCode, ProductName = l.ProductName, Qty = l.Qty,
                    UnitPrice = l.UnitPrice,
                    DiscountPercent = l.DiscountPercent, DiscountAmount = l.DiscountAmount,
                    VatPercent = l.VatPercent, VatAmount = l.VatAmount,
                    WithholdingPercent = l.WithholdingPercent, WithholdingAmount = l.WithholdingAmount,
                    LineTotal = l.LineTotal, NetAmount = l.NetAmount, Notes = l.Notes
                }).ToList()
            });
        }

        public Result<PurchaseReturnDetailDto> Create(CreatePurchaseReturnDto dto)
        {
            if (!Can("Create")) return FailDenied<PurchaseReturnDetailDto>();
            if (dto.Lines == null || dto.Lines.Count == 0) return Result.Fail<PurchaseReturnDetailDto>("المرتجع يحتاج سطراً واحداً على الأقل", ErrorCode.ValidationFailed);

            var supplier = _suppliers.GetById(dto.SupplierId);
            if (!supplier.IsSuccess) return Result.Fail<PurchaseReturnDetailDto>("المورد غير موجود", ErrorCode.ValidationFailed);
            if (string.IsNullOrWhiteSpace(supplier.Value.AccountCode))
                return Result.Fail<PurchaseReturnDetailDto>("حساب المورد غير مربوط — لا يمكن ترحيل المرتجع", ErrorCode.ValidationFailed);

            var resolvedLines = new List<PurchaseReturnLine>();
            foreach (var l in dto.Lines)
            {
                var product = _products.GetByCode(l.ProductCode);
                if (product == null) return Result.Fail<PurchaseReturnDetailDto>($"الصنف بالكود {l.ProductCode} غير موجود", ErrorCode.ValidationFailed);
                if (l.Qty <= 0) return Result.Fail<PurchaseReturnDetailDto>("الكمية يجب أن تكون أكبر من صفر", ErrorCode.ValidationFailed);

                var amounts = DocumentTotals.ForLine(l.Qty, l.UnitPrice, l.DiscountPercent, l.VatPercent, l.WithholdingPercent);
                resolvedLines.Add(new PurchaseReturnLine
                {
                    LineNo = l.LineNo, ProductId = product.Id, ProductCode = product.Code, ProductName = product.Name,
                    Qty = l.Qty, UnitPrice = l.UnitPrice,
                    DiscountPercent = l.DiscountPercent, DiscountAmount = amounts.Discount,
                    VatPercent = l.VatPercent, VatAmount = amounts.Vat,
                    WithholdingPercent = l.WithholdingPercent, WithholdingAmount = amounts.Withholding,
                    LineTotal = amounts.Gross, NetAmount = amounts.Net, Notes = l.Notes
                });
            }

            var pullCheck = _links.ValidatePulls(dto.Lines.Select(l => ((PrimeERP.Application.DTOs.Documents.IPullableLine)l, l.Qty)));
            if (pullCheck.IsFailure) return Result.Fail<PurchaseReturnDetailDto>(pullCheck.ErrorMessage, pullCheck.ErrorCode);

            var subTotal = resolvedLines.Sum(x => x.LineTotal);
            var discountAmount = resolvedLines.Sum(x => x.DiscountAmount);
            var taxableAmount = subTotal - discountAmount;
            var vatAmount = resolvedLines.Sum(x => x.VatAmount);
            var withholdingAmount = resolvedLines.Sum(x => x.WithholdingAmount);
            var netTotal = resolvedLines.Sum(x => x.NetAmount);

            var inventoryAccount = Settings.Get(SettingKeys.Accounts.Inventory, "");
            var vatAccount = Settings.Get(SettingKeys.Accounts.VATInput, "");
            var withholdingAccount = Settings.Get(SettingKeys.Accounts.WithholdingPayable, "");
            if (string.IsNullOrWhiteSpace(inventoryAccount))
                return Result.Fail<PurchaseReturnDetailDto>("حساب المخزون غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);
            if (vatAmount > 0 && string.IsNullOrWhiteSpace(vatAccount))
                return Result.Fail<PurchaseReturnDetailDto>("حساب ضريبة المدخلات (VATInput) غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);

            int returnId;
            try
            {
                var simplifiedFlow = Settings.Get(SettingKeys.Documents.SimplifiedFlow, true);
                returnId = Tx(db =>
                {
                    var returnNo = _numbers.Next(db, "PurchaseReturn");
                    var ret = new PurchaseReturn
                    {
                        ReturnNo = returnNo, ReturnDate = dto.ReturnDate, SupplierId = dto.SupplierId, WarehouseId = dto.WarehouseId,
                        SubTotal = subTotal, DiscountAmount = discountAmount, VatAmount = vatAmount, WithholdingAmount = withholdingAmount, NetTotal = netTotal, Notes = dto.Notes, CreatedBy = AppSession.Username
                    };
                    var id = _returns.InsertHeader(db, ret);

                    var inserted = new List<(PrimeERP.Application.DTOs.Documents.IPullableLine Line, int TargetLineId, decimal Qty)>();
                    for (int i = 0; i < resolvedLines.Count; i++)
                    {
                        var line = resolvedLines[i];
                        inserted.Add((dto.Lines[i], _returns.InsertLine(db, id, line), line.Qty));
                        var moveResult = simplifiedFlow
                            ? _stock.RecordMovement(db, line.ProductId, dto.WarehouseId, MovementType.Out, line.Qty, line.UnitPrice,
                            "PurchaseReturn", id, returnNo, dto.ReturnDate)
                            : Result.Ok();
                        if (!moveResult.IsSuccess) throw new InvalidOperationException(moveResult.ErrorMessage);
                    }

                    _links.RecordPulls(db, EntityName, id, inserted);

                    var journalLines = new List<CreateJournalLineDto>
                    {
                        new() { LineNo = 1, AccountCode = supplier.Value.AccountCode, Debit = netTotal },
                        new() { LineNo = 2, AccountCode = inventoryAccount, Credit = taxableAmount },
                    };
                    if (vatAmount > 0) journalLines.Add(new() { LineNo = 3, AccountCode = vatAccount, Credit = vatAmount });
                    if (withholdingAmount > 0) journalLines.Add(new() { LineNo = journalLines.Count + 1, AccountCode = withholdingAccount, Debit = withholdingAmount });

                    var journalDto = new CreateJournalDto
                    {
                        EntryDate = dto.ReturnDate, Description = $"مرتجع شراء {returnNo}", Source = nameof(JournalSource.Purchase), Lines = journalLines
                    };
                    var createResult = _journal.Create(db, journalDto);
                    if (!createResult.IsSuccess) throw new InvalidOperationException(createResult.ErrorMessage);

                    var postResult = _journal.Post(db, createResult.Value.Id);
                    if (!postResult.IsSuccess) throw new InvalidOperationException(postResult.ErrorMessage);

                    _returns.SetJournalEntryId(db, id, createResult.Value.Id);
                    return id;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<PurchaseReturnDetailDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log("PurchaseReturns", returnId, AuditAction.Insert, newValue: new { SupplierId = dto.SupplierId, NetTotal = netTotal });
            _suppliers.RecalculateBalance(dto.SupplierId);

            return GetById(returnId);
        }

        public Result Update(CreatePurchaseReturnDto dto) => Result.Fail("المرتجع مُرحَّل فور إنشائه — لا يمكن تعديله", ErrorCode.ValidationFailed);

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var document = _returns.GetById(id);
            if (document == null) return Result.Fail("المرتجع غير موجود", ErrorCode.NotFound);

            if (_links.GetPulledBySource(EntityName, id).Count > 0)
                return Result.Fail("سُحب من هذا المستند — احذف ما سُحب إليه أولاً", ErrorCode.ValidationFailed);

            Tx(db =>
            {
                if (document.JournalEntryId != null) _journal.Delete(db, document.JournalEntryId.Value);
                _stock.RemoveMovements(db, "PurchaseReturn", id);
                _links.RemovePull(EntityName, id, db);
                _returns.DeleteDocument(db, id);
            });

            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private PurchaseReturnDto ToDto(PurchaseReturn r) => new()
        {
            Id = r.Id, ReturnNo = r.ReturnNo, ReturnDate = r.ReturnDate, SupplierId = r.SupplierId,
            SupplierName = _suppliers.GetById(r.SupplierId) is { IsSuccess: true } s ? s.Value.Name : null,
            WarehouseId = r.WarehouseId, SubTotal = r.SubTotal, DiscountAmount = r.DiscountAmount, VatAmount = r.VatAmount,
            WithholdingAmount = r.WithholdingAmount, NetTotal = r.NetTotal, CreatedAt = r.CreatedAt
        };
    }
}
