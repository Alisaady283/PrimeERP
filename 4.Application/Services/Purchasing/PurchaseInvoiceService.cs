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
    /// <summary>فاتورة الشراء وقيدها</summary>
    public class PurchaseInvoiceService : ServiceBase, IPurchaseInvoiceService
    {
        private readonly IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine> _invoices;
        private readonly IProductRepository _products;
        private readonly ISupplierService _suppliers;
        private readonly IStockService _stock;
        private readonly IJournalService _journal;
        private readonly INumberSequenceService _numbers;

        private readonly PrimeERP.Application.Services.Documents.IDocumentLinkService _links;

        public PurchaseInvoiceService(IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine> invoices, IProductRepository products, ISupplierService suppliers,
            IStockService stock, IJournalService journal, INumberSequenceService numbers,
            PrimeERP.Application.Services.Documents.IDocumentLinkService links,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _invoices = invoices; _products = products; _suppliers = suppliers;
            _stock = stock; _journal = journal; _numbers = numbers; _links = links;
        }

        protected override string PermissionPrefix => "Purchases";
        protected override string StringPrefix => "Str.PurchaseInvoice";
        protected override string EntityName => "PurchaseInvoices";


        public Result<PagedResult<PurchaseInvoiceDto>> GetPaged(int page, int pageSize, PurchaseInvoiceFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<PurchaseInvoiceDto>>();
            filter ??= new PurchaseInvoiceFilter();

            var (items, total) = _invoices.GetPaged(page, pageSize, filter.SearchText, filter.SupplierId, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<PurchaseInvoiceDto> { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<PurchaseInvoiceDetailDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<PurchaseInvoiceDetailDto>();

            var invoice = _invoices.GetById(id);
            if (invoice == null) return Result.Fail<PurchaseInvoiceDetailDto>("الفاتورة غير موجودة", ErrorCode.NotFound);

            var baseDto = ToDto(invoice);
            var detail = new PurchaseInvoiceDetailDto
            {
                Id = baseDto.Id, InvoiceNo = baseDto.InvoiceNo, InvoiceDate = baseDto.InvoiceDate, SupplierId = baseDto.SupplierId,
                SupplierName = baseDto.SupplierName, WarehouseId = baseDto.WarehouseId,
                SubTotal = baseDto.SubTotal, DiscountAmount = baseDto.DiscountAmount, VatAmount = baseDto.VatAmount,
                WithholdingAmount = baseDto.WithholdingAmount, NetTotal = baseDto.NetTotal, StatusText = baseDto.StatusText,
                CreatedAt = baseDto.CreatedAt,
                Lines = _invoices.GetLines(id).Select(l => new PurchaseInvoiceLineDto
                {
                    LineNo = l.LineNo, ProductCode = l.ProductCode, ProductName = l.ProductName, Qty = l.Qty,
                    UnitPrice = l.UnitPrice,
                    DiscountPercent = l.DiscountPercent, DiscountAmount = l.DiscountAmount,
                    VatPercent = l.VatPercent, VatAmount = l.VatAmount,
                    WithholdingPercent = l.WithholdingPercent, WithholdingAmount = l.WithholdingAmount,
                    LineTotal = l.LineTotal, NetAmount = l.NetAmount, Notes = l.Notes
                }).ToList()
            };
            return Result.Ok(detail);
        }

        public Result<PurchaseInvoiceDetailDto> Create(CreatePurchaseInvoiceDto dto)
        {
            if (!Can("Create")) return FailDenied<PurchaseInvoiceDetailDto>();
            if (dto.Lines == null || dto.Lines.Count == 0) return Result.Fail<PurchaseInvoiceDetailDto>("الفاتورة تحتاج سطراً واحداً على الأقل", ErrorCode.ValidationFailed);

            var supplier = _suppliers.GetById(dto.SupplierId);
            if (!supplier.IsSuccess) return Result.Fail<PurchaseInvoiceDetailDto>("المورد غير موجود", ErrorCode.ValidationFailed);
            if (string.IsNullOrWhiteSpace(supplier.Value.AccountCode))
                return Result.Fail<PurchaseInvoiceDetailDto>("حساب المورد غير مربوط — لا يمكن ترحيل الفاتورة", ErrorCode.ValidationFailed);

            var resolvedLines = new List<PurchaseInvoiceLine>();
            foreach (var l in dto.Lines)
            {
                var product = _products.GetByCode(l.ProductCode);
                if (product == null) return Result.Fail<PurchaseInvoiceDetailDto>($"الصنف بالكود {l.ProductCode} غير موجود", ErrorCode.ValidationFailed);
                if (l.Qty <= 0) return Result.Fail<PurchaseInvoiceDetailDto>("الكمية يجب أن تكون أكبر من صفر", ErrorCode.ValidationFailed);

                var amounts = DocumentTotals.ForLine(l.Qty, l.UnitPrice, l.DiscountPercent, l.VatPercent, l.WithholdingPercent);
                resolvedLines.Add(new PurchaseInvoiceLine
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
            if (pullCheck.IsFailure) return Result.Fail<PurchaseInvoiceDetailDto>(pullCheck.ErrorMessage, pullCheck.ErrorCode);

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
                return Result.Fail<PurchaseInvoiceDetailDto>("حساب المخزون غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);
            if (vatAmount > 0 && string.IsNullOrWhiteSpace(vatAccount))
                return Result.Fail<PurchaseInvoiceDetailDto>("حساب ضريبة المدخلات (VATInput) غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);
            if (withholdingAmount > 0 && string.IsNullOrWhiteSpace(withholdingAccount))
                return Result.Fail<PurchaseInvoiceDetailDto>("حساب ضريبة الخصم والإضافة (WithholdingPayable) غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);

            int invoiceId;
            try
            {
                var simplifiedFlow = Settings.Get(SettingKeys.Documents.SimplifiedFlow, true);
                invoiceId = Tx(db =>
                {
                    var invoiceNo = _numbers.Next(db, "PurchaseInvoice");
                    var invoice = new PurchaseInvoice
                    {
                        InvoiceNo = invoiceNo, InvoiceDate = dto.InvoiceDate, SupplierId = dto.SupplierId, WarehouseId = dto.WarehouseId,
                        SubTotal = subTotal, DiscountAmount = discountAmount, VatAmount = vatAmount, WithholdingAmount = withholdingAmount, NetTotal = netTotal, Status = InvoiceStatus.Confirmed, Notes = dto.Notes,
                        CreatedBy = AppSession.Username
                    };
                    var id = _invoices.InsertHeader(db, invoice);

                    var inserted = new List<(PrimeERP.Application.DTOs.Documents.IPullableLine Line, int TargetLineId, decimal Qty)>();
                    for (int i = 0; i < resolvedLines.Count; i++)
                    {
                        var line = resolvedLines[i];
                        inserted.Add((dto.Lines[i], _invoices.InsertLine(db, id, line), line.Qty));
                        var moveResult = simplifiedFlow
                            ? _stock.RecordMovement(db, line.ProductId, dto.WarehouseId, MovementType.In, line.Qty, line.UnitPrice,
                            "PurchaseInvoice", id, invoiceNo, dto.InvoiceDate)
                            : Result.Ok();
                        if (!moveResult.IsSuccess) throw new InvalidOperationException(moveResult.ErrorMessage);
                    }

                    _links.RecordPulls(db, EntityName, id, inserted);

                    var journalLines = new List<CreateJournalLineDto>
                    {
                        new() { LineNo = 1, AccountCode = inventoryAccount, Debit = taxableAmount },
                        new() { LineNo = 2, AccountCode = supplier.Value.AccountCode, Credit = netTotal },
                    };
                    if (vatAmount > 0) journalLines.Add(new() { LineNo = 3, AccountCode = vatAccount, Debit = vatAmount });
                    if (withholdingAmount > 0) journalLines.Add(new() { LineNo = journalLines.Count + 1, AccountCode = withholdingAccount, Credit = withholdingAmount });

                    var journalDto = new CreateJournalDto
                    {
                        EntryDate = dto.InvoiceDate, Description = $"فاتورة شراء {invoiceNo}", Source = nameof(JournalSource.Purchase), Lines = journalLines
                    };
                    var createResult = _journal.Create(db, journalDto);
                    if (!createResult.IsSuccess) throw new InvalidOperationException(createResult.ErrorMessage);

                    var postResult = _journal.Post(db, createResult.Value.Id);
                    if (!postResult.IsSuccess) throw new InvalidOperationException(postResult.ErrorMessage);

                    _invoices.SetJournalEntryId(db, id, createResult.Value.Id);
                    return id;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<PurchaseInvoiceDetailDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log("PurchaseInvoices", invoiceId, AuditAction.Insert, newValue: new { SupplierId = dto.SupplierId, NetTotal = netTotal });

            _suppliers.RecalculateBalance(dto.SupplierId);

            return GetById(invoiceId);
        }

        public Result Update(CreatePurchaseInvoiceDto dto) => Result.Fail("الفاتورة مُرحَّلة فور إنشائها — لا يمكن تعديلها", ErrorCode.ValidationFailed);

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var document = _invoices.GetById(id);
            if (document == null) return Result.Fail("الفاتورة غير موجودة", ErrorCode.NotFound);

            if (_links.GetPulledBySource(EntityName, id).Count > 0)
                return Result.Fail("سُحب من هذا المستند — احذف ما سُحب إليه أولاً", ErrorCode.ValidationFailed);

            Tx(db =>
            {
                if (document.JournalEntryId != null) _journal.Delete(db, document.JournalEntryId.Value);
                _stock.RemoveMovements(db, "PurchaseInvoice", id);
                _links.RemovePull(EntityName, id, db);
                _invoices.DeleteDocument(db, id);
            });

            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private PurchaseInvoiceDto ToDto(PurchaseInvoice i) => new()
        {
            Id = i.Id, InvoiceNo = i.InvoiceNo, InvoiceDate = i.InvoiceDate, SupplierId = i.SupplierId,
            SupplierName = _suppliers.GetById(i.SupplierId) is { IsSuccess: true } s ? s.Value.Name : null,
            WarehouseId = i.WarehouseId ?? 0, SubTotal = i.SubTotal, DiscountAmount = i.DiscountAmount, VatAmount = i.VatAmount,
            WithholdingAmount = i.WithholdingAmount, NetTotal = i.NetTotal,
            StatusText = LocalizationService.Get($"Str.Journal.Status.{(i.Status == InvoiceStatus.Confirmed ? "Posted" : "Draft")}"),
            CreatedAt = i.CreatedAt
        };
    }
}
