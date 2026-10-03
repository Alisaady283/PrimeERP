using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Legacy.Documents;
using PrimeERP.Application.Legacy.Inventory;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Purchasing
{
    /// <summary>فاتورة الشراء وقيدها</summary>
    public class PurchaseInvoiceService
        : DocumentService<PurchaseInvoice, PurchaseInvoiceDto, PurchaseInvoiceDetailDto, CreatePurchaseInvoiceDto, PurchaseInvoiceFilter>,
          IPurchaseInvoiceService
    {
        private readonly AccountOf _accountOf;
        private readonly IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine> _invoices;
        private readonly IProductRepository _products;
        private readonly IPartyRepository<Supplier> _suppliers;
        private readonly INumberSequenceService _numbers;

        public PurchaseInvoiceService(IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine> invoices, IProductRepository products,
            IPartyRepository<Supplier> suppliers, IStockMove stock, Entries journal,
            INumberSequenceService numbers, IDocumentPull links,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            AccountOf accountOf)
            : base(permissions, settings, localization, audit, links, journal, stock)
        {
            _invoices = invoices; _products = products; _suppliers = suppliers;
            _numbers = numbers; _accountOf = accountOf;
        }

        protected override string PermissionPrefix => "Purchases";
        protected override string StringPrefix => "Str.PurchaseInvoice";
        protected override string EntityName => "PurchaseInvoices";
        protected override string PullType => EntityName;
        protected override string StockSource => "PurchaseInvoice";

        protected override PurchaseInvoice FindHead(int id) => _invoices.GetById(id);
        protected override int IdOf(PurchaseInvoice head) => head.Id;
        protected override int? EntryOf(PurchaseInvoice head) => head.JournalEntryId;
        protected override object AuditValue(PurchaseInvoice head) => new { head?.SupplierId, head?.NetTotal };

        protected override (List<PurchaseInvoice> Items, int Total) FindPage(int page, int pageSize, PurchaseInvoiceFilter filter)
        {
            filter ??= new PurchaseInvoiceFilter();
            return _invoices.GetPaged(page, pageSize, filter.SearchText, filter.SupplierId, filter.SortBy, filter.SortDescending);
        }

        protected override List<PurchaseInvoiceDto> ToRows(List<PurchaseInvoice> heads)
        {
            var names = _suppliers.NamesOf(heads.Select(x => x.SupplierId));
            return heads.Select(x => ToDto<PurchaseInvoiceDto>(x, names)).ToList();
        }

        protected override PurchaseInvoiceDetailDto ToDetail(PurchaseInvoice head)
        {
            var detail = ToDto<PurchaseInvoiceDetailDto>(head, _suppliers.NamesOf(new[] { head.SupplierId }));
            detail.Lines = TradeLines.ToDtos<PurchaseInvoiceLineDto>(_invoices.GetLines(head.Id));
            Links.Attach(EntityName, head.Id, detail.Lines);
            return detail;
        }

        protected override Result<Func<PrimeDbContext, int>> Plan(CreatePurchaseInvoiceDto dto)
        {
            var shape = DocumentLines.Check(dto.Lines, l => l.Qty, "Str.Trade.InvoiceNoLines", l => l.UnitPrice);
            if (shape.IsFailure) return shape.As<Func<PrimeDbContext, int>>();

            var partyAccount = _accountOf.Party(PartyKind.Supplier, dto.SupplierId, "Str.Trade.SupplierAccountInvoice");
            if (partyAccount.IsFailure) return partyAccount.As<Func<PrimeDbContext, int>>();

            var prepared = TradeLines.Prepare<PurchaseInvoiceLine>(_products, Links, EntityName, dto.Id, dto.Lines, totals => TradeAccounts.Purchases(Settings, totals));
            if (prepared.IsFailure) return prepared.As<Func<PrimeDbContext, int>>();
            var (lines, totals, accounts) = prepared.Value;

            var simplifiedFlow = Settings.Get(SettingKeys.Documents.SimplifiedFlow, true);
            return Result.Ok<Func<PrimeDbContext, int>>(db =>
            {
                var invoiceNo = _numbers.Next(db, "PurchaseInvoice");
                var id = _invoices.InsertHeader(db, Rows.Copy(dto, new PurchaseInvoice(), to =>
                {
                    to.InvoiceNo = invoiceNo;
                    to.SubTotal = totals.Gross;
                    to.DiscountAmount = totals.Discount;
                    to.VatAmount = totals.Vat;
                    to.WithholdingAmount = totals.Withholding;
                    to.NetTotal = totals.Net;
                    to.Status = InvoiceStatus.Confirmed;
                }));

                var inserted = new List<(IPullableLine Line, int TargetLineId, decimal Qty)>();
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    inserted.Add((dto.Lines[i], _invoices.InsertLine(db, id, line), line.Qty));
                    var moveResult = simplifiedFlow
                        ? Stock.RecordMovement(db, line.ProductId, dto.WarehouseId, MovementType.In, line.Qty, line.UnitPrice,
                            StockSource, id, invoiceNo, dto.InvoiceDate)
                        : Result.Ok();
                    if (moveResult.IsFailure) throw new InvalidOperationException(moveResult.ErrorMessage);
                }

                Links.RecordPulls(db, EntityName, id, inserted);

                var journalLines = TradeEntry.Lines(false, partyAccount.Value, accounts.Inventory, accounts.Vat, accounts.Withholding, totals);
                _invoices.SetJournalEntryId(db, id, Posting.Entry(Journals, db, dto.InvoiceDate, Msg("EntryDescription", invoiceNo),
                    nameof(JournalSource.Purchase), journalLines));
                return id;
            });
        }

        protected override void Remove(PrimeDbContext db, PurchaseInvoice head)
        {
            Posting.Reverse(Journals, db, head.JournalEntryId);
            Stock.RemoveMovements(db, StockSource, head.Id);
            Links.RemovePull(EntityName, head.Id, db);
            _invoices.DeleteDocument(db, head.Id);
        }

        private T ToDto<T>(PurchaseInvoice i, IReadOnlyDictionary<int, string> names) where T : PurchaseInvoiceDto, new() => Rows.Copy<T>(i, new(), to =>
        {
            to.SupplierName = names.GetValueOrDefault(i.SupplierId);
            to.WarehouseId = i.WarehouseId ?? 0;
            to.StatusText = LocalizationService.Get($"Str.Journal.Status.{(i.Status == InvoiceStatus.Confirmed ? "Posted" : "Draft")}");
        });
    }
}
