using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Rules;
using PrimeERP.Domain.Helpers;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Sales
{
    /// <summary>فاتورة البيع وقيدها</summary>
    public class SalesInvoiceService : ServiceBase, ISalesInvoiceService
    {
        private readonly IInvoiceRepository<SalesInvoice, SalesInvoiceLine> _invoices;
        private readonly IProductRepository _products;
        private readonly ICustomerService _customers;
        private readonly IWarehouseService _warehouses;
        private readonly IStockService _stock;
        private readonly IJournalService _journal;
        private readonly INumberSequenceService _numbers;
        private readonly PrimeERP.Application.Services.Documents.IDocumentLinkService _links;

        public SalesInvoiceService(IInvoiceRepository<SalesInvoice, SalesInvoiceLine> invoices, IProductRepository products, ICustomerService customers,
            IWarehouseService warehouses, IStockService stock, IJournalService journal, INumberSequenceService numbers,
            PrimeERP.Application.Services.Documents.IDocumentLinkService links,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _invoices = invoices; _products = products; _customers = customers; _warehouses = warehouses;
            _stock = stock; _journal = journal; _numbers = numbers; _links = links;
        }

        protected override string PermissionPrefix => "Sales";
        protected override string StringPrefix => "Str.SalesInvoice";
        protected override string EntityName => "SalesInvoices";


        public Result<PagedResult<SalesInvoiceDto>> GetPaged(int page, int pageSize, SalesInvoiceFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<SalesInvoiceDto>>();
            filter ??= new SalesInvoiceFilter();

            var (items, total) = _invoices.GetPaged(page, pageSize, filter.SearchText, filter.CustomerId, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<SalesInvoiceDto> { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<SalesInvoiceDetailDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<SalesInvoiceDetailDto>();

            var invoice = _invoices.GetById(id);
            if (invoice == null) return Result.Fail<SalesInvoiceDetailDto>("الفاتورة غير موجودة", ErrorCode.NotFound);

            var baseDto = ToDto(invoice);
            var detail = new SalesInvoiceDetailDto
            {
                Id = baseDto.Id, InvoiceNo = baseDto.InvoiceNo, InvoiceDate = baseDto.InvoiceDate, CustomerId = baseDto.CustomerId,
                CustomerName = baseDto.CustomerName, WarehouseId = baseDto.WarehouseId, WarehouseName = baseDto.WarehouseName,
                SubTotal = baseDto.SubTotal, DiscountAmount = baseDto.DiscountAmount, VatAmount = baseDto.VatAmount,
                WithholdingAmount = baseDto.WithholdingAmount, NetTotal = baseDto.NetTotal, StatusText = baseDto.StatusText,
                CreatedAt = baseDto.CreatedAt,
                Lines = _invoices.GetLines(id).Select(l => new SalesInvoiceLineDto
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

        public Result<SalesInvoiceDetailDto> Create(CreateSalesInvoiceDto dto)
        {
            if (!Can("Create")) return FailDenied<SalesInvoiceDetailDto>();
            if (dto.Lines == null || dto.Lines.Count == 0) return Result.Fail<SalesInvoiceDetailDto>("الفاتورة تحتاج سطراً واحداً على الأقل", ErrorCode.ValidationFailed);

            var customer = _customers.GetById(dto.CustomerId);
            if (!customer.IsSuccess) return Result.Fail<SalesInvoiceDetailDto>("العميل غير موجود", ErrorCode.ValidationFailed);
            if (string.IsNullOrWhiteSpace(customer.Value.AccountCode))
                return Result.Fail<SalesInvoiceDetailDto>("حساب العميل غير مربوط — لا يمكن ترحيل الفاتورة", ErrorCode.ValidationFailed);

            var resolvedLines = new List<(SalesInvoiceLine Line, decimal UnitCost)>();
            foreach (var l in dto.Lines)
            {
                var product = _products.GetByCode(l.ProductCode);
                if (product == null) return Result.Fail<SalesInvoiceDetailDto>($"الصنف بالكود {l.ProductCode} غير موجود", ErrorCode.ValidationFailed);
                if (l.Qty <= 0) return Result.Fail<SalesInvoiceDetailDto>("الكمية يجب أن تكون أكبر من صفر", ErrorCode.ValidationFailed);

                var amounts = DocumentTotals.ForLine(l.Qty, l.UnitPrice, l.DiscountPercent, l.VatPercent, l.WithholdingPercent);
                resolvedLines.Add((new SalesInvoiceLine
                {
                    LineNo = l.LineNo, ProductId = product.Id, ProductCode = product.Code, ProductName = product.Name,
                    Qty = l.Qty, UnitPrice = l.UnitPrice,
                    DiscountPercent = l.DiscountPercent, DiscountAmount = amounts.Discount,
                    VatPercent = l.VatPercent, VatAmount = amounts.Vat,
                    WithholdingPercent = l.WithholdingPercent, WithholdingAmount = amounts.Withholding,
                    LineTotal = amounts.Gross, NetAmount = amounts.Net, Notes = l.Notes
                }, 0m));
            }

            var pullCheck = _links.ValidatePulls(dto.Lines.Select(l => ((PrimeERP.Application.DTOs.Documents.IPullableLine)l, l.Qty)));
            if (pullCheck.IsFailure) return Result.Fail<SalesInvoiceDetailDto>(pullCheck.ErrorMessage, pullCheck.ErrorCode);

            var subTotal = resolvedLines.Sum(x => x.Line.LineTotal);
            var discountAmount = resolvedLines.Sum(x => x.Line.DiscountAmount);
            var taxableAmount = subTotal - discountAmount;
            var vatAmount = resolvedLines.Sum(x => x.Line.VatAmount);
            var withholdingAmount = resolvedLines.Sum(x => x.Line.WithholdingAmount);
            var netTotal = resolvedLines.Sum(x => x.Line.NetAmount);

            var salesAccount = Settings.Get(SettingKeys.Accounts.Sales, "");
            var vatAccount = Settings.Get(SettingKeys.Accounts.VATOutput, "");
            var withholdingAccount = Settings.Get(SettingKeys.Accounts.WithholdingReceivable, "");
            var cogsAccount = Settings.Get(SettingKeys.Accounts.COGS, "");
            var inventoryAccount = Settings.Get(SettingKeys.Accounts.Inventory, "");
            if (string.IsNullOrWhiteSpace(salesAccount) || string.IsNullOrWhiteSpace(cogsAccount) || string.IsNullOrWhiteSpace(inventoryAccount))
                return Result.Fail<SalesInvoiceDetailDto>("حسابات المبيعات/التكلفة/المخزون غير مضبوطة في الإعدادات", ErrorCode.ValidationFailed);
            if (vatAmount > 0 && string.IsNullOrWhiteSpace(vatAccount))
                return Result.Fail<SalesInvoiceDetailDto>("حساب ضريبة المخرجات (VATOutput) غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);
            if (withholdingAmount > 0 && string.IsNullOrWhiteSpace(withholdingAccount))
                return Result.Fail<SalesInvoiceDetailDto>("حساب ضريبة الخصم والإضافة (WithholdingReceivable) غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);

            int invoiceId;
            decimal totalCost = 0;   // تُملأ داخل المعاملة من متوسط اللحظة، ويُبنى عليها قيد تكلفة المبيعات
            try
            {
                var simplifiedFlow = Settings.Get(SettingKeys.Documents.SimplifiedFlow, true);
                invoiceId = Tx(db =>
                {
                    var invoiceNo = _numbers.Next(db, "SalesInvoice");
                    var invoice = new SalesInvoice
                    {
                        InvoiceNo = invoiceNo, InvoiceDate = dto.InvoiceDate, CustomerId = dto.CustomerId, WarehouseId = dto.WarehouseId,
                        SubTotal = subTotal, DiscountAmount = discountAmount, VatAmount = vatAmount, WithholdingAmount = withholdingAmount, NetTotal = netTotal, Status = InvoiceStatus.Confirmed, Notes = dto.Notes,
                        CreatedBy = AppSession.Username
                    };
                    var id = _invoices.InsertHeader(db, invoice);

                    var costs = _stock.GetIssueCosts(db, resolvedLines.Select(x => (x.Line.ProductId, x.Line.Qty)).ToList());
                    if (costs.IsFailure) throw new InvalidOperationException(costs.ErrorMessage);
                    totalCost = costs.Value.Sum();

                    var inserted = new List<(PrimeERP.Application.DTOs.Documents.IPullableLine Line, int TargetLineId, decimal Qty)>();
                    for (int i = 0; i < resolvedLines.Count; i++)
                    {
                        var line = resolvedLines[i].Line;
                        var lineId = _invoices.InsertLine(db, id, line);
                        inserted.Add((dto.Lines[i], lineId, line.Qty));

                        var moveResult = simplifiedFlow
                            ? _stock.RecordMovement(db, line.ProductId, dto.WarehouseId, MovementType.Out, line.Qty,
                            InventoryCosting.UnitCostOf(costs.Value[i], line.Qty),
                            "SalesInvoice", id, invoiceNo, dto.InvoiceDate)
                            : Result.Ok();
                        if (!moveResult.IsSuccess) throw new InvalidOperationException(moveResult.ErrorMessage);
                    }

                    _links.RecordPulls(db, EntityName, id, inserted);

                    var journalLines = new List<CreateJournalLineDto>
                    {
                        new() { LineNo = 1, AccountCode = customer.Value.AccountCode, Debit = netTotal },
                        new() { LineNo = 2, AccountCode = salesAccount, Credit = taxableAmount },
                    };
                    if (vatAmount > 0) journalLines.Add(new() { LineNo = 3, AccountCode = vatAccount, Credit = vatAmount });
                    if (withholdingAmount > 0) journalLines.Add(new() { LineNo = journalLines.Count + 1, AccountCode = withholdingAccount, Debit = withholdingAmount });
                    journalLines.Add(new() { LineNo = journalLines.Count + 1, AccountCode = cogsAccount, Debit = totalCost });
                    journalLines.Add(new() { LineNo = journalLines.Count + 1, AccountCode = inventoryAccount, Credit = totalCost });

                    var journalDto = new CreateJournalDto
                    {
                        EntryDate = dto.InvoiceDate, Description = $"فاتورة بيع {invoiceNo}", Source = nameof(JournalSource.Sales), Lines = journalLines
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
                return Result.Fail<SalesInvoiceDetailDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log("SalesInvoices", invoiceId, AuditAction.Insert, newValue: new { CustomerId = dto.CustomerId, NetTotal = netTotal });

            _customers.RecalculateBalance(dto.CustomerId);

            return GetById(invoiceId);
        }

        public Result Update(CreateSalesInvoiceDto dto) => Result.Fail("الفاتورة مُرحَّلة فور إنشائها — لا يمكن تعديلها", ErrorCode.ValidationFailed);

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
                _stock.RemoveMovements(db, "SalesInvoice", id);
                _links.RemovePull(EntityName, id, db);
                _invoices.DeleteDocument(db, id);
            });

            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private SalesInvoiceDto ToDto(SalesInvoice i) => new()
        {
            Id = i.Id, InvoiceNo = i.InvoiceNo, InvoiceDate = i.InvoiceDate, CustomerId = i.CustomerId,
            CustomerName = _customers.GetById(i.CustomerId) is { IsSuccess: true } c ? c.Value.Name : null,
            WarehouseId = i.WarehouseId ?? 0,
            WarehouseName = _warehouses.GetAll(true) is { IsSuccess: true } ws ? ws.Value.FirstOrDefault(w => w.Id == i.WarehouseId)?.Name : null,
            SubTotal = i.SubTotal, DiscountAmount = i.DiscountAmount, VatAmount = i.VatAmount,
            WithholdingAmount = i.WithholdingAmount, NetTotal = i.NetTotal,
            StatusText = LocalizationService.Get($"Str.Journal.Status.{(i.Status == InvoiceStatus.Confirmed ? "Posted" : "Draft")}"),
            CreatedAt = i.CreatedAt
        };
    }
}
