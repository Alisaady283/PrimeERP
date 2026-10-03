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
    /// <summary>مرتجع الشراء وقيده</summary>
    public class PurchaseReturnService
        : DocumentService<PurchaseReturn, PurchaseReturnDto, PurchaseReturnDetailDto, CreatePurchaseReturnDto, PurchaseReturnFilter>,
          IPurchaseReturnService
    {
        private readonly AccountOf _accountOf;
        private readonly IReturnRepository<PurchaseReturn, PurchaseReturnLine> _returns;
        private readonly IProductRepository _products;
        private readonly IPartyRepository<Supplier> _suppliers;
        private readonly INumberSequenceService _numbers;

        public PurchaseReturnService(IReturnRepository<PurchaseReturn, PurchaseReturnLine> returns, IProductRepository products,
            IPartyRepository<Supplier> suppliers, IStockMove stock, Entries journal,
            INumberSequenceService numbers, IDocumentPull links,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            AccountOf accountOf)
            : base(permissions, settings, localization, audit, links, journal, stock)
        {
            _returns = returns; _products = products; _suppliers = suppliers;
            _numbers = numbers; _accountOf = accountOf;
        }

        protected override string PermissionPrefix => "Purchases";
        protected override string StringPrefix => "Str.PurchaseReturn";
        protected override string EntityName => "PurchaseReturns";
        protected override string PullType => EntityName;
        protected override string StockSource => "PurchaseReturn";

        protected override PurchaseReturn FindHead(int id) => _returns.GetById(id);
        protected override int IdOf(PurchaseReturn head) => head.Id;
        protected override int? EntryOf(PurchaseReturn head) => head.JournalEntryId;
        protected override object AuditValue(PurchaseReturn head) => new { head?.SupplierId, head?.NetTotal };

        protected override (List<PurchaseReturn> Items, int Total) FindPage(int page, int pageSize, PurchaseReturnFilter filter)
        {
            filter ??= new PurchaseReturnFilter();
            return _returns.GetPaged(page, pageSize, filter.SearchText, filter.SupplierId, filter.SortBy, filter.SortDescending);
        }

        protected override List<PurchaseReturnDto> ToRows(List<PurchaseReturn> heads)
        {
            var names = _suppliers.NamesOf(heads.Select(x => x.SupplierId));
            return heads.Select(x => ToDto<PurchaseReturnDto>(x, names)).ToList();
        }

        protected override PurchaseReturnDetailDto ToDetail(PurchaseReturn head)
        {
            var detail = ToDto<PurchaseReturnDetailDto>(head, _suppliers.NamesOf(new[] { head.SupplierId }));
            detail.Lines = TradeLines.ToDtos<PurchaseReturnLineDto>(_returns.GetLines(head.Id));
            Links.Attach(EntityName, head.Id, detail.Lines);
            return detail;
        }

        protected override Result<Func<PrimeDbContext, int>> Plan(CreatePurchaseReturnDto dto)
        {
            var shape = DocumentLines.Check(dto.Lines, l => l.Qty, "Str.Trade.ReturnNoLines", l => l.UnitPrice);
            if (shape.IsFailure) return shape.As<Func<PrimeDbContext, int>>();

            var partyAccount = _accountOf.Party(PartyKind.Supplier, dto.SupplierId, "Str.Trade.SupplierAccountReturn");
            if (partyAccount.IsFailure) return partyAccount.As<Func<PrimeDbContext, int>>();

            var prepared = TradeLines.Prepare<PurchaseReturnLine>(_products, Links, EntityName, dto.Id, dto.Lines, totals => TradeAccounts.Purchases(Settings, totals));
            if (prepared.IsFailure) return prepared.As<Func<PrimeDbContext, int>>();
            var (lines, totals, accounts) = prepared.Value;

            var simplifiedFlow = Settings.Get(SettingKeys.Documents.SimplifiedFlow, true);
            return Result.Ok<Func<PrimeDbContext, int>>(db =>
            {
                var returnNo = _numbers.Next(db, "PurchaseReturn");
                var id = _returns.InsertHeader(db, Rows.Copy(dto, new PurchaseReturn(), to =>
                {
                    to.ReturnNo = returnNo;
                    to.SubTotal = totals.Gross;
                    to.DiscountAmount = totals.Discount;
                    to.VatAmount = totals.Vat;
                    to.WithholdingAmount = totals.Withholding;
                    to.NetTotal = totals.Net;
                }));

                var inserted = new List<(IPullableLine Line, int TargetLineId, decimal Qty)>();
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    inserted.Add((dto.Lines[i], _returns.InsertLine(db, id, line), line.Qty));
                    var moveResult = simplifiedFlow
                        ? Stock.RecordMovement(db, line.ProductId, dto.WarehouseId, MovementType.Out, line.Qty, line.UnitPrice,
                            StockSource, id, returnNo, dto.ReturnDate)
                        : Result.Ok();
                    if (moveResult.IsFailure) throw new InvalidOperationException(moveResult.ErrorMessage);
                }

                Links.RecordPulls(db, EntityName, id, inserted);

                var journalLines = TradeEntry.Lines(true, partyAccount.Value, accounts.Inventory, accounts.Vat, accounts.Withholding, totals);
                _returns.SetJournalEntryId(db, id, Posting.Entry(Journals, db, dto.ReturnDate, Msg("EntryDescription", returnNo),
                    nameof(JournalSource.Purchase), journalLines));
                return id;
            });
        }

        protected override void Remove(PrimeDbContext db, PurchaseReturn head)
        {
            Posting.Reverse(Journals, db, head.JournalEntryId);
            Stock.RemoveMovements(db, StockSource, head.Id);
            Links.RemovePull(EntityName, head.Id, db);
            _returns.DeleteDocument(db, head.Id);
        }

        private static T ToDto<T>(PurchaseReturn r, IReadOnlyDictionary<int, string> names) where T : PurchaseReturnDto, new() => Rows.Copy<T>(r, new(), to =>
        {
            to.SupplierName = names.GetValueOrDefault(r.SupplierId);
        });
    }
}
