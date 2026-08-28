using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services.Sales
{
    // الفاتورة تُرحَّل ذرّياً عند الإنشاء (سطور + حركة مخزون صادرة لكل سطر + قيد يومية مُرحَّل) داخل معاملة
    // واحدة — بلا حالة "مسودة" منفصلة (النطاق الحالي لا يعرض زر ترحيل مستقل في الواجهة بعد؛ راجع Journal
    // التي لها نفس الفجوة). Update/Delete مرفوضتان دائماً بعد الإنشاء، بنفس منطق Journal.PostedCannotEdit.
    public class SalesInvoiceService : ISalesInvoiceService
    {
        private readonly ISalesInvoiceRepository _invoices;
        private readonly IProductRepository _products;
        private readonly ICustomerService _customers;
        private readonly IWarehouseService _warehouses;
        private readonly IStockService _stock;
        private readonly IJournalService _journal;
        private readonly INumberSequenceService _numbers;
        private readonly IPermissionService _permissions;
        private readonly ISettingsProvider _settings;
        private readonly IAuditLogger _audit;

        public SalesInvoiceService(ISalesInvoiceRepository invoices, IProductRepository products, ICustomerService customers,
            IWarehouseService warehouses, IStockService stock, IJournalService journal, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, IAuditLogger audit)
        {
            _invoices = invoices; _products = products; _customers = customers; _warehouses = warehouses;
            _stock = stock; _journal = journal; _numbers = numbers; _permissions = permissions; _settings = settings; _audit = audit;
        }

        private bool Can(string action) => _permissions.Can($"Sales.{action}");

        public Result<PagedResult<SalesInvoiceDto>> GetPaged(int page, int pageSize, SalesInvoiceFilter filter = null)
        {
            if (!Can("View")) return Result.Fail<PagedResult<SalesInvoiceDto>>("لا صلاحية", ErrorCode.Unauthorized);
            filter ??= new SalesInvoiceFilter();

            var (items, total) = _invoices.GetPaged(page, pageSize, filter.SearchText, filter.CustomerId, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<SalesInvoiceDto> { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<SalesInvoiceDetailDto> GetById(int id)
        {
            if (!Can("View")) return Result.Fail<SalesInvoiceDetailDto>("لا صلاحية", ErrorCode.Unauthorized);

            var invoice = _invoices.GetById(id);
            if (invoice == null) return Result.Fail<SalesInvoiceDetailDto>("الفاتورة غير موجودة", ErrorCode.NotFound);

            var baseDto = ToDto(invoice);
            var detail = new SalesInvoiceDetailDto
            {
                Id = baseDto.Id, InvoiceNo = baseDto.InvoiceNo, InvoiceDate = baseDto.InvoiceDate, CustomerId = baseDto.CustomerId,
                CustomerName = baseDto.CustomerName, WarehouseId = baseDto.WarehouseId, WarehouseName = baseDto.WarehouseName,
                SubTotal = baseDto.SubTotal, TaxAmount = baseDto.TaxAmount, NetTotal = baseDto.NetTotal, StatusText = baseDto.StatusText,
                CreatedAt = baseDto.CreatedAt,
                Lines = _invoices.GetLines(id).Select(l => new SalesInvoiceLineDto
                {
                    LineNo = l.LineNo, ProductCode = l.ProductCode, ProductName = l.ProductName, Qty = l.Qty,
                    UnitPrice = l.UnitPrice, TaxPercent = l.TaxPercent, LineTotal = l.LineTotal, Notes = l.Notes
                }).ToList()
            };
            return Result.Ok(detail);
        }

        public Result<SalesInvoiceDetailDto> Create(CreateSalesInvoiceDto dto)
        {
            if (!Can("Create")) return Result.Fail<SalesInvoiceDetailDto>("لا صلاحية", ErrorCode.Unauthorized);
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

                var lineTotal = l.Qty * l.UnitPrice;
                var lineTax = lineTotal * l.TaxPercent / 100m;
                resolvedLines.Add((new SalesInvoiceLine
                {
                    LineNo = l.LineNo, ProductId = product.Id, ProductCode = product.Code, ProductName = product.Name,
                    Qty = l.Qty, UnitPrice = l.UnitPrice, TaxPercent = l.TaxPercent, TaxAmount = lineTax, LineTotal = lineTotal, Notes = l.Notes
                }, product.CostPrice));
            }

            var subTotal = resolvedLines.Sum(x => x.Line.LineTotal);
            var taxAmount = resolvedLines.Sum(x => x.Line.TaxAmount);
            var netTotal = subTotal + taxAmount;
            var totalCost = resolvedLines.Sum(x => x.Line.Qty * x.UnitCost);

            var salesAccount = _settings.Get(SettingKeys.Accounts.Sales, "");
            var vatAccount = _settings.Get(SettingKeys.Accounts.VATOutput, "");
            var cogsAccount = _settings.Get(SettingKeys.Accounts.COGS, "");
            var inventoryAccount = _settings.Get(SettingKeys.Accounts.Inventory, "");
            if (string.IsNullOrWhiteSpace(salesAccount) || string.IsNullOrWhiteSpace(cogsAccount) || string.IsNullOrWhiteSpace(inventoryAccount))
                return Result.Fail<SalesInvoiceDetailDto>("حسابات المبيعات/التكلفة/المخزون غير مضبوطة في الإعدادات", ErrorCode.ValidationFailed);
            if (taxAmount > 0 && string.IsNullOrWhiteSpace(vatAccount))
                return Result.Fail<SalesInvoiceDetailDto>("حساب ضريبة المخرجات (VATOutput) غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);

            int invoiceId;
            try
            {
                invoiceId = Db.RunTransaction((conn, tx) =>
                {
                    var invoiceNo = _numbers.Next(conn, tx, "SalesInvoice");
                    var invoice = new SalesInvoice
                    {
                        InvoiceNo = invoiceNo, InvoiceDate = dto.InvoiceDate, CustomerId = dto.CustomerId, WarehouseId = dto.WarehouseId,
                        SubTotal = subTotal, TaxAmount = taxAmount, NetTotal = netTotal, Status = InvoiceStatus.Confirmed, Notes = dto.Notes,
                        CreatedBy = AppSession.Username
                    };
                    var id = _invoices.InsertHeader(conn, tx, invoice);

                    foreach (var (line, unitCost) in resolvedLines)
                    {
                        _invoices.InsertLine(conn, tx, id, line);
                        var moveResult = _stock.RecordMovement(conn, tx, line.ProductId, dto.WarehouseId, MovementType.Out, line.Qty, unitCost,
                            "SalesInvoice", id, invoiceNo, dto.InvoiceDate);
                        if (!moveResult.IsSuccess) throw new InvalidOperationException(moveResult.ErrorMessage);
                    }

                    var journalLines = new List<CreateJournalLineDto>
                    {
                        new() { LineNo = 1, AccountCode = customer.Value.AccountCode, Debit = netTotal },
                        new() { LineNo = 2, AccountCode = salesAccount, Credit = subTotal },
                    };
                    if (taxAmount > 0) journalLines.Add(new() { LineNo = 3, AccountCode = vatAccount, Credit = taxAmount });
                    journalLines.Add(new() { LineNo = journalLines.Count + 1, AccountCode = cogsAccount, Debit = totalCost });
                    journalLines.Add(new() { LineNo = journalLines.Count + 1, AccountCode = inventoryAccount, Credit = totalCost });

                    var journalDto = new CreateJournalDto
                    {
                        EntryDate = dto.InvoiceDate, Description = $"فاتورة بيع {invoiceNo}", Source = nameof(JournalSource.Sales), Lines = journalLines
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
                return Result.Fail<SalesInvoiceDetailDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            _audit.Log("SalesInvoices", invoiceId, AuditAction.Insert, newValue: new { CustomerId = dto.CustomerId, NetTotal = netTotal });

            // رصيد العميل عمود مخزَّن (Customer.Balance) لا يُعاد حسابه تلقائياً عند ترحيل قيد — بعد التزام
            // المعاملة أعلاه فقط (RecalculateBalance يفتح اتصالاً جديداً، يحتاج القيد ملتزَماً ليراه). فشلها
            // (صلاحية/حساب غير مضبوط) لا يُسقِط الفاتورة المُرحَّلة بالفعل — أثر جانبي غير حرج.
            _customers.RecalculateBalance(dto.CustomerId);

            return GetById(invoiceId);
        }

        public Result Update(CreateSalesInvoiceDto dto) => Result.Fail("الفاتورة مُرحَّلة فور إنشائها — لا يمكن تعديلها", ErrorCode.ValidationFailed);

        public Result Delete(int id) => Result.Fail("الفاتورة مُرحَّلة فور إنشائها — لا يمكن حذفها", ErrorCode.ValidationFailed);

        private SalesInvoiceDto ToDto(SalesInvoice i) => new()
        {
            Id = i.Id, InvoiceNo = i.InvoiceNo, InvoiceDate = i.InvoiceDate, CustomerId = i.CustomerId,
            CustomerName = _customers.GetById(i.CustomerId) is { IsSuccess: true } c ? c.Value.Name : null,
            WarehouseId = i.WarehouseId ?? 0, SubTotal = i.SubTotal, TaxAmount = i.TaxAmount, NetTotal = i.NetTotal,
            StatusText = LocalizationService.Get($"Str.Journal.Status.{(i.Status == InvoiceStatus.Confirmed ? "Posted" : "Draft")}"),
            CreatedAt = i.CreatedAt
        };
    }
}
