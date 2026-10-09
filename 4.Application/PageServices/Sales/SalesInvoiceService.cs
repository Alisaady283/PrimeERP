using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Validation;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Documents;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
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

namespace PrimeERP.Application.PageServices.Sales
{
    /// <summary>فاتورة البيع وقيدها</summary>
    public class SalesInvoiceService
        : DocumentService<SalesInvoice, SalesInvoiceDto, SalesInvoiceDetailDto, CreateSalesInvoiceDto, SalesInvoiceFilter>,
          ISalesInvoiceService
    {
        private readonly AccountOf _accountOf;
        private readonly IInvoiceRepository<SalesInvoice, SalesInvoiceLine> _invoices;
        private readonly IProductRepository _products;
        private readonly IPartyRepository<Customer> _customers;
        private readonly ILookupRepository<Warehouse> _warehouses;
        private readonly INumberSequenceService _numbers;

        public SalesInvoiceService(IInvoiceRepository<SalesInvoice, SalesInvoiceLine> invoices, IProductRepository products,
            IPartyRepository<Customer> customers, ILookupRepository<Warehouse> warehouses, IStockMove stock,
            Entries journal, INumberSequenceService numbers, IDocumentPull links,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            AccountOf accountOf)
            : base(permissions, settings, localization, audit, links, journal, stock)
        {
            _invoices = invoices; _products = products; _customers = customers; _warehouses = warehouses;
            _numbers = numbers; _accountOf = accountOf;
        }

        protected override string PermissionPrefix => "Sales";
        protected override string StringPrefix => "Str.SalesInvoice";
        protected override string EntityName => "SalesInvoices";
        protected override string PullType => EntityName;
        protected override string StockSource => "SalesInvoice";

        protected override SalesInvoice FindHead(int id) => _invoices.GetById(id);
        protected override int IdOf(SalesInvoice head) => head.Id;
        protected override int? EntryOf(SalesInvoice head) => head.JournalEntryId;
        protected override object AuditValue(SalesInvoice head) => new { head?.CustomerId, head?.NetTotal };

        protected override (List<SalesInvoice> Items, int Total) FindPage(int page, int pageSize, SalesInvoiceFilter filter)
        {
            filter ??= new SalesInvoiceFilter();
            return _invoices.GetPaged(page, pageSize, filter.SearchText, filter.CustomerId, filter.SortBy, filter.SortDescending);
        }

        protected override List<SalesInvoiceDto> ToRows(List<SalesInvoice> heads)
        {
            var names = _customers.NamesOf(heads.Select(x => x.CustomerId));
            var warehouses = _warehouses.NamesOf(heads.Select(x => x.WarehouseId ?? 0));
            return heads.Select(x => ToDto<SalesInvoiceDto>(x, names, warehouses)).ToList();
        }

        protected override SalesInvoiceDetailDto ToDetail(SalesInvoice head)
        {
            var detail = ToDto<SalesInvoiceDetailDto>(head, _customers.NamesOf(new[] { head.CustomerId }), _warehouses.NamesOf(new[] { head.WarehouseId ?? 0 }));
            detail.Lines = TradeLines.ToDtos<SalesInvoiceLineDto>(_invoices.GetLines(head.Id));
            Links.Attach(EntityName, head.Id, detail.Lines);
            return detail;
        }

        protected override Result<Func<PrimeDbContext, int>> Plan(CreateSalesInvoiceDto dto)
        {
            var shape = DocumentLines.Check(dto.Lines, l => l.Qty, "Str.Trade.InvoiceNoLines", l => l.UnitPrice);
            if (shape.IsFailure) return shape.As<Func<PrimeDbContext, int>>();

            var partyAccount = _accountOf.Party(PartyKind.Customer, dto.CustomerId, "Str.Trade.CustomerAccountInvoice");
            if (partyAccount.IsFailure) return partyAccount.As<Func<PrimeDbContext, int>>();

            var prepared = TradeLines.Prepare<SalesInvoiceLine>(_products, Links, EntityName, dto.Id, dto.Lines, totals => TradeAccounts.Sales(Settings, totals));
            if (prepared.IsFailure) return prepared.As<Func<PrimeDbContext, int>>();
            var (lines, totals, accounts) = prepared.Value;

            var simplifiedFlow = Settings.Get(SettingKeys.Documents.SimplifiedFlow, true);
            return Result.Ok<Func<PrimeDbContext, int>>(db =>
            {
                var invoiceNo = _numbers.Next(db, "SalesInvoice");
                var id = _invoices.InsertHeader(db, Rows.Copy(dto, new SalesInvoice(), to =>
                {
                    to.InvoiceNo = invoiceNo;
                    to.SubTotal = totals.Gross;
                    to.DiscountAmount = totals.Discount;
                    to.VatAmount = totals.Vat;
                    to.WithholdingAmount = totals.Withholding;
                    to.NetTotal = totals.Net;
                    to.Status = InvoiceStatus.Confirmed;
                }));

                // تكلفة الصرف من متوسط اللحظة
                var costs = Stock.GetIssueCosts(db, lines.Select(x => (x.ProductId, x.Qty)).ToList());
                if (costs.IsFailure) throw new InvalidOperationException(costs.ErrorMessage);

                var inserted = new List<(IPullableLine Line, int TargetLineId, decimal Qty)>();
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    inserted.Add((dto.Lines[i], _invoices.InsertLine(db, id, line), line.Qty));

                    var moveResult = simplifiedFlow
                        ? Stock.RecordMovement(db, line.ProductId, dto.WarehouseId, MovementType.Out, line.Qty,
                            InventoryCosting.UnitCostOf(costs.Value.Lines[i], line.Qty), StockSource, id, invoiceNo, dto.InvoiceDate)
                        : Result.Ok();
                    if (moveResult.IsFailure) throw new InvalidOperationException(moveResult.ErrorMessage);
                }

                Links.RecordPulls(db, EntityName, id, inserted);

                var journalLines = TradeEntry.Lines(true, partyAccount.Value, accounts.Main, accounts.Vat, accounts.Withholding, totals, accounts.Cogs, accounts.Inventory, costs.Value.Total);
                _invoices.SetJournalEntryId(db, id, Posting.Entry(Journals, db, dto.InvoiceDate, Msg("EntryDescription", invoiceNo),
                    nameof(JournalSource.Sales), journalLines));
                return id;
            });
        }

        protected override void Remove(PrimeDbContext db, SalesInvoice head)
        {
            Posting.Reverse(Journals, db, head.JournalEntryId);
            Stock.RemoveMovements(db, StockSource, head.Id);
            Links.RemovePull(EntityName, head.Id, db);
            _invoices.DeleteDocument(db, head.Id);
        }

        private T ToDto<T>(SalesInvoice i, IReadOnlyDictionary<int, string> names, IReadOnlyDictionary<int, string> warehouses)
            where T : SalesInvoiceDto, new() => Rows.Copy<T>(i, new(), to =>
            {
                to.CustomerName = names.GetValueOrDefault(i.CustomerId);
                to.WarehouseId = i.WarehouseId ?? 0;
                to.WarehouseName = i.WarehouseId is int w ? warehouses.GetValueOrDefault(w) : null;
                to.StatusText = LocalizationService.Get($"Str.Journal.Status.{(i.Status == InvoiceStatus.Confirmed ? "Posted" : "Draft")}");
            });
    }
}
