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
    // عكس SalesInvoiceService حرفياً: Credit العميل (تخفيض مديونيته)، Debit المبيعات (عكس الإيراد)، Debit
    // VATOutput (عكس الضريبة المُحصَّلة)، Credit COGS/Debit Inventory (البضاعة ترجع للمخزون). حركة مخزون In.
    public class SalesReturnService : ISalesReturnService
    {
        private readonly ISalesReturnRepository _returns;
        private readonly IProductRepository _products;
        private readonly ICustomerService _customers;
        private readonly IStockService _stock;
        private readonly IJournalService _journal;
        private readonly INumberSequenceService _numbers;
        private readonly IPermissionService _permissions;
        private readonly ISettingsProvider _settings;
        private readonly IAuditLogger _audit;

        public SalesReturnService(ISalesReturnRepository returns, IProductRepository products, ICustomerService customers,
            IStockService stock, IJournalService journal, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, IAuditLogger audit)
        {
            _returns = returns; _products = products; _customers = customers;
            _stock = stock; _journal = journal; _numbers = numbers; _permissions = permissions; _settings = settings; _audit = audit;
        }

        private bool Can(string action) => _permissions.Can($"Sales.{action}");

        public Result<PagedResult<SalesReturnDto>> GetPaged(int page, int pageSize, SalesReturnFilter filter = null)
        {
            if (!Can("View")) return Result.Fail<PagedResult<SalesReturnDto>>("لا صلاحية", ErrorCode.Unauthorized);
            filter ??= new SalesReturnFilter();

            var (items, total) = _returns.GetPaged(page, pageSize, filter.SearchText, filter.CustomerId, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<SalesReturnDto> { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<SalesReturnDetailDto> GetById(int id)
        {
            if (!Can("View")) return Result.Fail<SalesReturnDetailDto>("لا صلاحية", ErrorCode.Unauthorized);

            var ret = _returns.GetById(id);
            if (ret == null) return Result.Fail<SalesReturnDetailDto>("المرتجع غير موجود", ErrorCode.NotFound);

            var baseDto = ToDto(ret);
            return Result.Ok(new SalesReturnDetailDto
            {
                Id = baseDto.Id, ReturnNo = baseDto.ReturnNo, ReturnDate = baseDto.ReturnDate, CustomerId = baseDto.CustomerId,
                CustomerName = baseDto.CustomerName, WarehouseId = baseDto.WarehouseId,
                SubTotal = baseDto.SubTotal, TaxAmount = baseDto.TaxAmount, NetTotal = baseDto.NetTotal, CreatedAt = baseDto.CreatedAt,
                Lines = _returns.GetLines(id).Select(l => new SalesReturnLineDto
                {
                    LineNo = l.LineNo, ProductCode = l.ProductCode, ProductName = l.ProductName, Qty = l.Qty,
                    UnitPrice = l.UnitPrice, TaxPercent = l.TaxPercent, LineTotal = l.LineTotal, Notes = l.Notes
                }).ToList()
            });
        }

        public Result<SalesReturnDetailDto> Create(CreateSalesReturnDto dto)
        {
            if (!Can("Create")) return Result.Fail<SalesReturnDetailDto>("لا صلاحية", ErrorCode.Unauthorized);
            if (dto.Lines == null || dto.Lines.Count == 0) return Result.Fail<SalesReturnDetailDto>("المرتجع يحتاج سطراً واحداً على الأقل", ErrorCode.ValidationFailed);

            var customer = _customers.GetById(dto.CustomerId);
            if (!customer.IsSuccess) return Result.Fail<SalesReturnDetailDto>("العميل غير موجود", ErrorCode.ValidationFailed);
            if (string.IsNullOrWhiteSpace(customer.Value.AccountCode))
                return Result.Fail<SalesReturnDetailDto>("حساب العميل غير مربوط — لا يمكن ترحيل المرتجع", ErrorCode.ValidationFailed);

            var resolvedLines = new List<(SalesReturnLine Line, decimal UnitCost)>();
            foreach (var l in dto.Lines)
            {
                var product = _products.GetByCode(l.ProductCode);
                if (product == null) return Result.Fail<SalesReturnDetailDto>($"الصنف بالكود {l.ProductCode} غير موجود", ErrorCode.ValidationFailed);
                if (l.Qty <= 0) return Result.Fail<SalesReturnDetailDto>("الكمية يجب أن تكون أكبر من صفر", ErrorCode.ValidationFailed);

                var lineTotal = l.Qty * l.UnitPrice;
                var lineTax = lineTotal * l.TaxPercent / 100m;
                resolvedLines.Add((new SalesReturnLine
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
                return Result.Fail<SalesReturnDetailDto>("حسابات المبيعات/التكلفة/المخزون غير مضبوطة في الإعدادات", ErrorCode.ValidationFailed);
            if (taxAmount > 0 && string.IsNullOrWhiteSpace(vatAccount))
                return Result.Fail<SalesReturnDetailDto>("حساب ضريبة المخرجات (VATOutput) غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);

            int returnId;
            try
            {
                returnId = Db.RunTransaction((conn, tx) =>
                {
                    var returnNo = _numbers.Next(conn, tx, "SalesReturn");
                    var ret = new SalesReturn
                    {
                        ReturnNo = returnNo, ReturnDate = dto.ReturnDate, CustomerId = dto.CustomerId, WarehouseId = dto.WarehouseId,
                        SubTotal = subTotal, TaxAmount = taxAmount, NetTotal = netTotal, Notes = dto.Notes, CreatedBy = AppSession.Username
                    };
                    var id = _returns.InsertHeader(conn, tx, ret);

                    foreach (var (line, unitCost) in resolvedLines)
                    {
                        _returns.InsertLine(conn, tx, id, line);
                        var moveResult = _stock.RecordMovement(conn, tx, line.ProductId, dto.WarehouseId, MovementType.In, line.Qty, unitCost,
                            "SalesReturn", id, returnNo, dto.ReturnDate);
                        if (!moveResult.IsSuccess) throw new InvalidOperationException(moveResult.ErrorMessage);
                    }

                    var journalLines = new List<CreateJournalLineDto>
                    {
                        new() { LineNo = 1, AccountCode = salesAccount, Debit = subTotal },
                        new() { LineNo = 2, AccountCode = customer.Value.AccountCode, Credit = netTotal },
                    };
                    if (taxAmount > 0) journalLines.Add(new() { LineNo = 3, AccountCode = vatAccount, Debit = taxAmount });
                    journalLines.Add(new() { LineNo = journalLines.Count + 1, AccountCode = inventoryAccount, Debit = totalCost });
                    journalLines.Add(new() { LineNo = journalLines.Count + 1, AccountCode = cogsAccount, Credit = totalCost });

                    var journalDto = new CreateJournalDto
                    {
                        EntryDate = dto.ReturnDate, Description = $"مرتجع بيع {returnNo}", Source = nameof(JournalSource.Sales), Lines = journalLines
                    };
                    var createResult = _journal.Create(conn, tx, journalDto);
                    if (!createResult.IsSuccess) throw new InvalidOperationException(createResult.ErrorMessage);

                    var postResult = _journal.Post(conn, tx, createResult.Value.Id);
                    if (!postResult.IsSuccess) throw new InvalidOperationException(postResult.ErrorMessage);

                    _returns.SetJournalEntryId(conn, tx, id, createResult.Value.Id);
                    return id;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<SalesReturnDetailDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            _audit.Log("SalesReturns", returnId, AuditAction.Insert, newValue: new { CustomerId = dto.CustomerId, NetTotal = netTotal });
            _customers.RecalculateBalance(dto.CustomerId);

            return GetById(returnId);
        }

        public Result Update(CreateSalesReturnDto dto) => Result.Fail("المرتجع مُرحَّل فور إنشائه — لا يمكن تعديله", ErrorCode.ValidationFailed);

        public Result Delete(int id) => Result.Fail("المرتجع مُرحَّل فور إنشائه — لا يمكن حذفه", ErrorCode.ValidationFailed);

        private SalesReturnDto ToDto(SalesReturn r) => new()
        {
            Id = r.Id, ReturnNo = r.ReturnNo, ReturnDate = r.ReturnDate, CustomerId = r.CustomerId,
            CustomerName = _customers.GetById(r.CustomerId) is { IsSuccess: true } c ? c.Value.Name : null,
            WarehouseId = r.WarehouseId, SubTotal = r.SubTotal, TaxAmount = r.TaxAmount, NetTotal = r.NetTotal, CreatedAt = r.CreatedAt
        };
    }
}
