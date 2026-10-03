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
using PrimeERP.Application.DTOs.Sales;
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

namespace PrimeERP.Application.Legacy.Sales
{
    /// <summary>مرتجع البيع وقيده</summary>
    public class SalesReturnService
        : DocumentService<SalesReturn, SalesReturnDto, SalesReturnDetailDto, CreateSalesReturnDto, SalesReturnFilter>,
          ISalesReturnService
    {
        private readonly AccountOf _accountOf;
        private readonly IReturnRepository<SalesReturn, SalesReturnLine> _returns;
        private readonly IProductRepository _products;
        private readonly IPartyRepository<Customer> _customers;
        private readonly IStockMove _stock;
        private readonly INumberSequenceService _numbers;

        public SalesReturnService(IReturnRepository<SalesReturn, SalesReturnLine> returns, IProductRepository products,
            IPartyRepository<Customer> customers, IStockMove stock, Entries journal,
            INumberSequenceService numbers, IDocumentPull links,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            AccountOf accountOf)
            : base(permissions, settings, localization, audit, links, journal)
        {
            _returns = returns; _products = products; _customers = customers;
            _stock = stock; _numbers = numbers; _accountOf = accountOf;
        }

        protected override string PermissionPrefix => "Sales";
        protected override string StringPrefix => "Str.SalesReturn";
        protected override string EntityName => "SalesReturns";
        protected override string PullType => EntityName;

        protected override SalesReturn FindHead(int id) => _returns.GetById(id);
        protected override int IdOf(SalesReturn head) => head.Id;
        protected override int? EntryOf(SalesReturn head) => head.JournalEntryId;
        protected override object AuditValue(SalesReturn head) => new { head?.CustomerId, head?.NetTotal };

        protected override (List<SalesReturn> Items, int Total) FindPage(int page, int pageSize, SalesReturnFilter filter)
        {
            filter ??= new SalesReturnFilter();
            return _returns.GetPaged(page, pageSize, filter.SearchText, filter.CustomerId, filter.SortBy, filter.SortDescending);
        }

        protected override List<SalesReturnDto> ToRows(List<SalesReturn> heads)
        {
            var names = _customers.NamesOf(heads.Select(x => x.CustomerId));
            return heads.Select(x => ToDto<SalesReturnDto>(x, names)).ToList();
        }

        protected override SalesReturnDetailDto ToDetail(SalesReturn head)
        {
            var detail = ToDto<SalesReturnDetailDto>(head, _customers.NamesOf(new[] { head.CustomerId }));
            detail.Lines = TradeLines.ToDtos<SalesReturnLineDto>(_returns.GetLines(head.Id));
            Links.Attach(EntityName, head.Id, detail.Lines);
            return detail;
        }

        protected override Result<Func<PrimeDbContext, int>> Plan(CreateSalesReturnDto dto)
        {
            var shape = DocumentLines.Check(dto.Lines, l => l.Qty, "Str.Trade.ReturnNoLines");
            if (shape.IsFailure) return shape.As<Func<PrimeDbContext, int>>();

            var partyAccount = _accountOf.Party(PartyKind.Customer, dto.CustomerId, "Str.Trade.CustomerAccountReturn");
            if (partyAccount.IsFailure) return partyAccount.As<Func<PrimeDbContext, int>>();

            var prepared = TradeLines.Prepare<SalesReturnLine>(_products, Links, EntityName, dto.Id, dto.Lines, totals => TradeAccounts.SalesReturns(Settings, totals));
            if (prepared.IsFailure) return prepared.As<Func<PrimeDbContext, int>>();
            var (lines, totals, accounts) = prepared.Value;

            var simplifiedFlow = Settings.Get(SettingKeys.Documents.SimplifiedFlow, true);
            return Result.Ok<Func<PrimeDbContext, int>>(db =>
            {
                var returnNo = _numbers.Next(db, "SalesReturn");
                var id = _returns.InsertHeader(db, Rows.Copy(dto, new SalesReturn(), to =>
                {
                    to.ReturnNo = returnNo;
                    to.SubTotal = totals.Gross;
                    to.DiscountAmount = totals.Discount;
                    to.VatAmount = totals.Vat;
                    to.WithholdingAmount = totals.Withholding;
                    to.NetTotal = totals.Net;
                }));

                var costs = _stock.GetReturnCosts(db, "SalesInvoice",
                    lines.Select((line, i) => (line.ProductId, line.Qty, dto.Lines[i].SourceId, dto.Lines[i].SourceLineId)).ToList(), simplifiedFlow);

                var inserted = new List<(IPullableLine Line, int TargetLineId, decimal Qty)>();
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    var source = dto.Lines[i];

                    inserted.Add((source, _returns.InsertLine(db, id, line), line.Qty));
                    var moveResult = simplifiedFlow
                        ? _stock.RecordMovement(db, line.ProductId, dto.WarehouseId, MovementType.In, line.Qty, costs.UnitCosts[i],
                            "SalesReturn", id, returnNo, dto.ReturnDate)
                        : Result.Ok();
                    if (moveResult.IsFailure) throw new InvalidOperationException(moveResult.ErrorMessage);
                }

                Links.RecordPulls(db, EntityName, id, inserted);

                var journalLines = TradeEntry.Lines(false, partyAccount.Value, accounts.Main, accounts.Vat, accounts.Withholding, totals, accounts.Cogs, accounts.Inventory, costs.Total);
                _returns.SetJournalEntryId(db, id, Posting.Entry(Journals, db, dto.ReturnDate, Msg("EntryDescription", returnNo),
                    nameof(JournalSource.Sales), journalLines));
                return id;
            });
        }

        protected override void Remove(PrimeDbContext db, SalesReturn head)
        {
            Posting.Reverse(Journals, db, head.JournalEntryId);
            _stock.RemoveMovements(db, "SalesReturn", head.Id);
            Links.RemovePull(EntityName, head.Id, db);
            _returns.DeleteDocument(db, head.Id);
        }

        private static T ToDto<T>(SalesReturn r, IReadOnlyDictionary<int, string> names) where T : SalesReturnDto, new() => Rows.Copy<T>(r, new(), to =>
        {
            to.CustomerName = names.GetValueOrDefault(r.CustomerId);
        });
    }
}
