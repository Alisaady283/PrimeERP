using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Helpers;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services.Purchasing
{
    // نسخة طبق الأصل من SalesInvoiceService — الفرق: Debit Inventory (لا COGS، الشراء يُرسمَل في المخزون
    // مباشرة بسعر الشراء)، Debit VATInput (لا VATOutput)، Credit المورد (لا Debit العميل)، حركة مخزون In (لا Out).
    public class PurchaseInvoiceService : IPurchaseInvoiceService
    {
        private readonly IPurchaseInvoiceRepository _invoices;
        private readonly IProductRepository _products;
        private readonly ISupplierService _suppliers;
        private readonly IStockService _stock;
        private readonly IJournalService _journal;
        private readonly INumberSequenceService _numbers;
        private readonly IPermissionService _permissions;
        private readonly ISettingsProvider _settings;
        private readonly IAuditLogger _audit;

        public PurchaseInvoiceService(IPurchaseInvoiceRepository invoices, IProductRepository products, ISupplierService suppliers,
            IStockService stock, IJournalService journal, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, IAuditLogger audit)
        {
            _invoices = invoices; _products = products; _suppliers = suppliers;
            _stock = stock; _journal = journal; _numbers = numbers; _permissions = permissions; _settings = settings; _audit = audit;
        }

        private bool Can(string action) => _permissions.Can($"Purchases.{action}");

        public Result<PagedResult<PurchaseInvoiceDto>> GetPaged(int page, int pageSize, PurchaseInvoiceFilter filter = null)
        {
            if (!Can("View")) return Result.Fail<PagedResult<PurchaseInvoiceDto>>("لا صلاحية", ErrorCode.Unauthorized);
            filter ??= new PurchaseInvoiceFilter();

            var (items, total) = _invoices.GetPaged(page, pageSize, filter.SearchText, filter.SupplierId, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<PurchaseInvoiceDto> { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<PurchaseInvoiceDetailDto> GetById(int id)
        {
            if (!Can("View")) return Result.Fail<PurchaseInvoiceDetailDto>("لا صلاحية", ErrorCode.Unauthorized);

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
            if (!Can("Create")) return Result.Fail<PurchaseInvoiceDetailDto>("لا صلاحية", ErrorCode.Unauthorized);
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

            var subTotal = resolvedLines.Sum(x => x.LineTotal);
            var discountAmount = resolvedLines.Sum(x => x.DiscountAmount);
            var taxableAmount = subTotal - discountAmount;
            var vatAmount = resolvedLines.Sum(x => x.VatAmount);
            var withholdingAmount = resolvedLines.Sum(x => x.WithholdingAmount);
            var netTotal = resolvedLines.Sum(x => x.NetAmount);

            var inventoryAccount = _settings.Get(SettingKeys.Accounts.Inventory, "");
            var vatAccount = _settings.Get(SettingKeys.Accounts.VATInput, "");
            var withholdingAccount = _settings.Get(SettingKeys.Accounts.WithholdingPayable, "");
            if (string.IsNullOrWhiteSpace(inventoryAccount))
                return Result.Fail<PurchaseInvoiceDetailDto>("حساب المخزون غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);
            if (vatAmount > 0 && string.IsNullOrWhiteSpace(vatAccount))
                return Result.Fail<PurchaseInvoiceDetailDto>("حساب ضريبة المدخلات (VATInput) غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);
            if (withholdingAmount > 0 && string.IsNullOrWhiteSpace(withholdingAccount))
                return Result.Fail<PurchaseInvoiceDetailDto>("حساب ضريبة الخصم والإضافة (WithholdingPayable) غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);

            int invoiceId;
            try
            {
                var simplifiedFlow = _settings.Get(SettingKeys.Documents.SimplifiedFlow, true);
                invoiceId = Db.RunTransaction((conn, tx) =>
                {
                    var invoiceNo = _numbers.Next(conn, tx, "PurchaseInvoice");
                    var invoice = new PurchaseInvoice
                    {
                        InvoiceNo = invoiceNo, InvoiceDate = dto.InvoiceDate, SupplierId = dto.SupplierId, WarehouseId = dto.WarehouseId,
                        SubTotal = subTotal, DiscountAmount = discountAmount, VatAmount = vatAmount, WithholdingAmount = withholdingAmount, NetTotal = netTotal, Status = InvoiceStatus.Confirmed, Notes = dto.Notes,
                        CreatedBy = AppSession.Username
                    };
                    var id = _invoices.InsertHeader(conn, tx, invoice);

                    foreach (var line in resolvedLines)
                    {
                        _invoices.InsertLine(conn, tx, id, line);
                        // الوضع المبسّط: الفاتورة تحرّك المخزون بنفسها (لا موظف مخزن ولا أذون). الوضع الشامل:
                        // إذن الصرف/الاستلام هو من يحرّك المخزون، والفاتورة تُسحب منه — فتحريكها هنا يخصم مرتين.
                        var moveResult = simplifiedFlow
                            ? _stock.RecordMovement(conn, tx, line.ProductId, dto.WarehouseId, MovementType.In, line.Qty, line.UnitPrice,
                            "PurchaseInvoice", id, invoiceNo, dto.InvoiceDate)
                            : Result.Ok();
                        if (!moveResult.IsSuccess) throw new InvalidOperationException(moveResult.ErrorMessage);
                    }

                    var journalLines = new List<CreateJournalLineDto>
                    {
                        new() { LineNo = 1, AccountCode = inventoryAccount, Debit = taxableAmount },
                        new() { LineNo = 2, AccountCode = supplier.Value.AccountCode, Credit = netTotal },
                    };
                    if (vatAmount > 0) journalLines.Add(new() { LineNo = 3, AccountCode = vatAccount, Debit = vatAmount });
                    // ما نحجزه من المورد نُورّده للمصلحة نيابةً عنه — التزام علينا لا خصم من قيمة الشراء.
                    if (withholdingAmount > 0) journalLines.Add(new() { LineNo = journalLines.Count + 1, AccountCode = withholdingAccount, Credit = withholdingAmount });

                    var journalDto = new CreateJournalDto
                    {
                        EntryDate = dto.InvoiceDate, Description = $"فاتورة شراء {invoiceNo}", Source = nameof(JournalSource.Purchase), Lines = journalLines
                    };
                    var createResult = _journal.Create(conn, tx, journalDto);
                    if (!createResult.IsSuccess) throw new InvalidOperationException(createResult.ErrorMessage);

                    var postResult = _journal.Post(conn, tx, createResult.Value.Id);
                    if (!postResult.IsSuccess) throw new InvalidOperationException(postResult.ErrorMessage);

                    _invoices.SetJournalEntryId(conn, tx, id, createResult.Value.Id);
                    return id;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<PurchaseInvoiceDetailDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            _audit.Log("PurchaseInvoices", invoiceId, AuditAction.Insert, newValue: new { SupplierId = dto.SupplierId, NetTotal = netTotal });

            // نفس ملاحظة SalesInvoiceService — Supplier.Balance عمود مخزَّن، يُعاد حسابه بعد التزام المعاملة فقط.
            _suppliers.RecalculateBalance(dto.SupplierId);

            return GetById(invoiceId);
        }

        public Result Update(CreatePurchaseInvoiceDto dto) => Result.Fail("الفاتورة مُرحَّلة فور إنشائها — لا يمكن تعديلها", ErrorCode.ValidationFailed);

        public Result Delete(int id) => Result.Fail("الفاتورة مُرحَّلة فور إنشائها — لا يمكن حذفها", ErrorCode.ValidationFailed);

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
