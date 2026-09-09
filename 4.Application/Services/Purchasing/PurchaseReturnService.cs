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
    // عكس PurchaseInvoiceService: Debit المورد (تخفيض مديونيته)، Credit Inventory/Credit VATInput (عكس ما
    // سُجِّل عند الشراء). حركة مخزون Out (تتحقق من كفاية الرصيد تلقائياً — لا يمكن إرجاع أكثر مما بالمخزون).
    public class PurchaseReturnService : ServiceBase, IPurchaseReturnService
    {
        private readonly IPurchaseReturnRepository _returns;
        private readonly IProductRepository _products;
        private readonly ISupplierService _suppliers;
        private readonly IStockService _stock;
        private readonly IJournalService _journal;
        private readonly INumberSequenceService _numbers;

        public PurchaseReturnService(IPurchaseReturnRepository returns, IProductRepository products, ISupplierService suppliers,
            IStockService stock, IJournalService journal, INumberSequenceService numbers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _returns = returns; _products = products; _suppliers = suppliers;
            _stock = stock; _journal = journal; _numbers = numbers;
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
                returnId = Db.RunTransaction((conn, tx) =>
                {
                    var returnNo = _numbers.Next(conn, tx, "PurchaseReturn");
                    var ret = new PurchaseReturn
                    {
                        ReturnNo = returnNo, ReturnDate = dto.ReturnDate, SupplierId = dto.SupplierId, WarehouseId = dto.WarehouseId,
                        SubTotal = subTotal, DiscountAmount = discountAmount, VatAmount = vatAmount, WithholdingAmount = withholdingAmount, NetTotal = netTotal, Notes = dto.Notes, CreatedBy = AppSession.Username
                    };
                    var id = _returns.InsertHeader(conn, tx, ret);

                    foreach (var line in resolvedLines)
                    {
                        _returns.InsertLine(conn, tx, id, line);
                        // الوضع المبسّط: المرتجع تحرّك المخزون بنفسها (لا موظف مخزن ولا أذون). الوضع الشامل:
                        // إذن الصرف/الاستلام هو من يحرّك المخزون، والمرتجع تُسحب منه — فتحريكها هنا يخصم مرتين.
                        var moveResult = simplifiedFlow
                            ? _stock.RecordMovement(conn, tx, line.ProductId, dto.WarehouseId, MovementType.Out, line.Qty, line.UnitPrice,
                            "PurchaseReturn", id, returnNo, dto.ReturnDate)
                            : Result.Ok();
                        if (!moveResult.IsSuccess) throw new InvalidOperationException(moveResult.ErrorMessage);
                    }

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
                return Result.Fail<PurchaseReturnDetailDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log("PurchaseReturns", returnId, AuditAction.Insert, newValue: new { SupplierId = dto.SupplierId, NetTotal = netTotal });
            _suppliers.RecalculateBalance(dto.SupplierId);

            return GetById(returnId);
        }

        public Result Update(CreatePurchaseReturnDto dto) => Result.Fail("المرتجع مُرحَّل فور إنشائه — لا يمكن تعديله", ErrorCode.ValidationFailed);

        public Result Delete(int id) => Result.Fail("المرتجع مُرحَّل فور إنشائه — لا يمكن حذفه", ErrorCode.ValidationFailed);

        private PurchaseReturnDto ToDto(PurchaseReturn r) => new()
        {
            Id = r.Id, ReturnNo = r.ReturnNo, ReturnDate = r.ReturnDate, SupplierId = r.SupplierId,
            SupplierName = _suppliers.GetById(r.SupplierId) is { IsSuccess: true } s ? s.Value.Name : null,
            WarehouseId = r.WarehouseId, SubTotal = r.SubTotal, DiscountAmount = r.DiscountAmount, VatAmount = r.VatAmount,
            WithholdingAmount = r.WithholdingAmount, NetTotal = r.NetTotal, CreatedAt = r.CreatedAt
        };
    }
}
