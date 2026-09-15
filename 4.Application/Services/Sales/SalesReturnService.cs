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
using PrimeERP.Domain.Helpers;
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
    public class SalesReturnService : ServiceBase, ISalesReturnService
    {
        private readonly ISalesReturnRepository _returns;
        private readonly IProductRepository _products;
        private readonly ICustomerService _customers;
        private readonly IStockService _stock;
        private readonly IJournalService _journal;
        private readonly INumberSequenceService _numbers;

        private readonly PrimeERP.Application.Services.Documents.IDocumentLinkService _links;

        public SalesReturnService(ISalesReturnRepository returns, IProductRepository products, ICustomerService customers,
            IStockService stock, IJournalService journal, INumberSequenceService numbers,
            PrimeERP.Application.Services.Documents.IDocumentLinkService links,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _returns = returns; _products = products; _customers = customers;
            _stock = stock; _journal = journal; _numbers = numbers; _links = links;
        }

        protected override string PermissionPrefix => "Sales";
        protected override string StringPrefix => "Str.SalesReturn";
        protected override string EntityName => "SalesReturns";


        public Result<PagedResult<SalesReturnDto>> GetPaged(int page, int pageSize, SalesReturnFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<SalesReturnDto>>();
            filter ??= new SalesReturnFilter();

            var (items, total) = _returns.GetPaged(page, pageSize, filter.SearchText, filter.CustomerId, filter.SortBy, filter.SortDescending);
            return Result.Ok(new PagedResult<SalesReturnDto> { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<SalesReturnDetailDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<SalesReturnDetailDto>();

            var ret = _returns.GetById(id);
            if (ret == null) return Result.Fail<SalesReturnDetailDto>("المرتجع غير موجود", ErrorCode.NotFound);

            var baseDto = ToDto(ret);
            return Result.Ok(new SalesReturnDetailDto
            {
                Id = baseDto.Id, ReturnNo = baseDto.ReturnNo, ReturnDate = baseDto.ReturnDate, CustomerId = baseDto.CustomerId,
                CustomerName = baseDto.CustomerName, WarehouseId = baseDto.WarehouseId,
                SubTotal = baseDto.SubTotal, DiscountAmount = baseDto.DiscountAmount, VatAmount = baseDto.VatAmount,
                WithholdingAmount = baseDto.WithholdingAmount, NetTotal = baseDto.NetTotal, CreatedAt = baseDto.CreatedAt,
                Lines = _returns.GetLines(id).Select(l => new SalesReturnLineDto
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

        public Result<SalesReturnDetailDto> Create(CreateSalesReturnDto dto)
        {
            if (!Can("Create")) return FailDenied<SalesReturnDetailDto>();
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

                var amounts = DocumentTotals.ForLine(l.Qty, l.UnitPrice, l.DiscountPercent, l.VatPercent, l.WithholdingPercent);
                resolvedLines.Add((new SalesReturnLine
                {
                    LineNo = l.LineNo, ProductId = product.Id, ProductCode = product.Code, ProductName = product.Name,
                    Qty = l.Qty, UnitPrice = l.UnitPrice,
                    DiscountPercent = l.DiscountPercent, DiscountAmount = amounts.Discount,
                    VatPercent = l.VatPercent, VatAmount = amounts.Vat,
                    WithholdingPercent = l.WithholdingPercent, WithholdingAmount = amounts.Withholding,
                    LineTotal = amounts.Gross, NetAmount = amounts.Net, Notes = l.Notes
                }, 0m));
            }

            // المتبقّي على المصدر يُفحص قبل أي كتابة — الواجهة تمنع الخطأ، والخدمة تمنع الالتفاف عليها.
            var pullCheck = _links.ValidatePulls(dto.Lines.Select(l => ((PrimeERP.Application.DTOs.Documents.IPullableLine)l, l.Qty)));
            if (pullCheck.IsFailure) return Result.Fail<SalesReturnDetailDto>(pullCheck.ErrorMessage, pullCheck.ErrorCode);

            var subTotal = resolvedLines.Sum(x => x.Line.LineTotal);
            var discountAmount = resolvedLines.Sum(x => x.Line.DiscountAmount);
            var taxableAmount = subTotal - discountAmount;
            var vatAmount = resolvedLines.Sum(x => x.Line.VatAmount);
            var withholdingAmount = resolvedLines.Sum(x => x.Line.WithholdingAmount);
            var netTotal = resolvedLines.Sum(x => x.Line.NetAmount);

            var salesAccount = Settings.Get(SettingKeys.Accounts.Sales, "");

            // المرتجع عكس المبيعات: يُرحَّل على حساب المرتجعات المقابل إن ضُبط، وإلا عكساً على المبيعات نفسه.
            var returnsAccount = Settings.Get(SettingKeys.Accounts.SalesReturns, "");
            if (string.IsNullOrWhiteSpace(returnsAccount)) returnsAccount = salesAccount;
            var vatAccount = Settings.Get(SettingKeys.Accounts.VATOutput, "");
            var withholdingAccount = Settings.Get(SettingKeys.Accounts.WithholdingReceivable, "");
            var cogsAccount = Settings.Get(SettingKeys.Accounts.COGS, "");
            var inventoryAccount = Settings.Get(SettingKeys.Accounts.Inventory, "");
            if (string.IsNullOrWhiteSpace(salesAccount) || string.IsNullOrWhiteSpace(cogsAccount) || string.IsNullOrWhiteSpace(inventoryAccount))
                return Result.Fail<SalesReturnDetailDto>("حسابات المبيعات/التكلفة/المخزون غير مضبوطة في الإعدادات", ErrorCode.ValidationFailed);
            if (vatAmount > 0 && string.IsNullOrWhiteSpace(vatAccount))
                return Result.Fail<SalesReturnDetailDto>("حساب ضريبة المخرجات (VATOutput) غير مضبوط في الإعدادات", ErrorCode.ValidationFailed);

            int returnId;
            decimal totalCost = 0;   // تُملأ داخل المعاملة بتكلفة صرف المرتجَع أصلاً، ويُبنى عليها قيد المخزون
            try
            {
                var simplifiedFlow = Settings.Get(SettingKeys.Documents.SimplifiedFlow, true);
                returnId = Db.RunTransaction((conn, tx) =>
                {
                    var returnNo = _numbers.Next(conn, tx, "SalesReturn");
                    var ret = new SalesReturn
                    {
                        ReturnNo = returnNo, ReturnDate = dto.ReturnDate, CustomerId = dto.CustomerId, WarehouseId = dto.WarehouseId,
                        SubTotal = subTotal, DiscountAmount = discountAmount, VatAmount = vatAmount, WithholdingAmount = withholdingAmount, NetTotal = netTotal, Notes = dto.Notes, CreatedBy = AppSession.Username
                    };
                    var id = _returns.InsertHeader(conn, tx, ret);

                    var inserted = new List<(PrimeERP.Application.DTOs.Documents.IPullableLine Line, int TargetLineId, decimal Qty)>();
                    for (int i = 0; i < resolvedLines.Count; i++)
                    {
                        var line = resolvedLines[i].Line;
                        var source = dto.Lines[i];

                        // المرتجع يعود بتكلفة صرفه الأصلي، مقروءةً من حركة الفاتورة التي سُحب منها. وبلا
                        // سحبٍ (مرتجعٌ مُدخَل يدوياً) يعود بمتوسط اللحظة — فلا يلوّث الرصيد بسعر بيع.
                        var unitCost =
                            (source.SourceLineId > 0
                                ? _stock.SourceUnitCost(conn, tx, "SalesInvoice", source.SourceId, line.ProductId)
                                : null)
                            ?? _stock.CurrentUnitCost(conn, tx, line.ProductId);

                        totalCost += line.Qty * unitCost;

                        inserted.Add((source, _returns.InsertLine(conn, tx, id, line), line.Qty));
                        // الوضع المبسّط: المرتجع تحرّك المخزون بنفسها (لا موظف مخزن ولا أذون). الوضع الشامل:
                        // إذن الصرف/الاستلام هو من يحرّك المخزون، والمرتجع تُسحب منه — فتحريكها هنا يخصم مرتين.
                        var moveResult = simplifiedFlow
                            ? _stock.RecordMovement(conn, tx, line.ProductId, dto.WarehouseId, MovementType.In, line.Qty, unitCost,
                            "SalesReturn", id, returnNo, dto.ReturnDate)
                            : Result.Ok();
                        if (!moveResult.IsSuccess) throw new InvalidOperationException(moveResult.ErrorMessage);
                    }

                    _links.RecordPulls(conn, tx, EntityName, id, inserted);

                    var journalLines = new List<CreateJournalLineDto>
                    {
                        new() { LineNo = 1, AccountCode = returnsAccount, Debit = taxableAmount },
                        new() { LineNo = 2, AccountCode = customer.Value.AccountCode, Credit = netTotal },
                    };
                    if (vatAmount > 0) journalLines.Add(new() { LineNo = 3, AccountCode = vatAccount, Debit = vatAmount });
                    if (withholdingAmount > 0) journalLines.Add(new() { LineNo = journalLines.Count + 1, AccountCode = withholdingAccount, Credit = withholdingAmount });
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

            Audit.Log("SalesReturns", returnId, AuditAction.Insert, newValue: new { CustomerId = dto.CustomerId, NetTotal = netTotal });
            _customers.RecalculateBalance(dto.CustomerId);

            return GetById(returnId);
        }

        public Result Update(CreateSalesReturnDto dto) => Result.Fail("المرتجع مُرحَّل فور إنشائه — لا يمكن تعديله", ErrorCode.ValidationFailed);

        /// <summary>
        /// نفس تسلسل السند: صلاحية ثم معاملة تحذف القيد وأثر المخزون والمستند معاً — فلا يبقى قيدٌ
        /// ولا حركةٌ بلا مستندها. الصلاحية هي البوابة، والحواجز المحاسبية تبقى حيث كانت.
        /// </summary>
        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var document = _returns.GetById(id);
            if (document == null) return Result.Fail("المرتجع غير موجود", ErrorCode.NotFound);

            // السحب يمنع الحذف: مستندٌ لاحق يقوم عليه، فحذفه يترك الأخير بلا أصل.
            if (_links.GetPulledBySource(EntityName, id).Count > 0)
                return Result.Fail("سُحب من هذا المستند — احذف ما سُحب إليه أولاً", ErrorCode.ValidationFailed);

            Db.RunTransaction((conn, tx) =>
            {
                if (document.JournalEntryId != null) _journal.Delete(conn, tx, document.JournalEntryId.Value);
                _stock.RemoveMovements(conn, tx, "SalesReturn", id);
                // روابط ما سحبه هذا المستند تُزال معه، وإلّا بقيت تحرس مصدراً عن مستندٍ لم يعد موجوداً.
                _links.RemovePull(EntityName, id, conn, tx);
                _returns.DeleteDocument(conn, tx, id);
            });

            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private SalesReturnDto ToDto(SalesReturn r) => new()
        {
            Id = r.Id, ReturnNo = r.ReturnNo, ReturnDate = r.ReturnDate, CustomerId = r.CustomerId,
            CustomerName = _customers.GetById(r.CustomerId) is { IsSuccess: true } c ? c.Value.Name : null,
            WarehouseId = r.WarehouseId, SubTotal = r.SubTotal, DiscountAmount = r.DiscountAmount, VatAmount = r.VatAmount,
            WithholdingAmount = r.WithholdingAmount, NetTotal = r.NetTotal, CreatedAt = r.CreatedAt
        };
    }
}
