using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.PageServices.Vouchers;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.PageServices.Sales
{
    public record PosReceipt(int DocumentId, string DocumentNo, decimal Net, decimal Collected, decimal Change);

    public record HeldEntry(int Id, string Label, DateTime HeldAt);

    public interface IPosService
    {
        Result<PosShift> Current();
        Result<PosShift> Open(decimal openingCash, int? treasuryId, int? warehouseId);
        Result<PosShift> Close(decimal countedCash);
        Result<PosReceipt> Sell(CreateSalesInvoiceDto sale, decimal paid);
        Result<PosReceipt> Return(CreateSalesReturnDto sale);
        Result Hold(CreateSalesInvoiceDto sale, string label);
        Result<List<HeldEntry>> Held();
        Result<CreateSalesInvoiceDto> Take(int heldId);
        Result DeleteHeld(int heldId);
    }

    public class PosService : ServiceBase, IPosService
    {
        protected override string PermissionPrefix => "Pos";
        protected override string StringPrefix => "Str.Pos";
        protected override string EntityName => "Pos";

        private readonly ISalesInvoiceService _invoices;
        private readonly ISalesReturnService _returns;
        private readonly IReceiptVoucherService _receipts;
        private readonly IPaymentVoucherService _payments;
        private readonly IPosShiftRepository _shifts;
        private readonly IHeldSaleRepository _held;
        private readonly ILookupRepository<Branch> _branches;
        private readonly AccountOf _accountsOf;
        private readonly Entries _journals;
        private readonly PosReturnScope _returnScope;

        public PosService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            ISalesInvoiceService invoices, ISalesReturnService returns, IReceiptVoucherService receipts, IPaymentVoucherService payments,
            IPosShiftRepository shifts, IHeldSaleRepository held, ILookupRepository<Branch> branches, AccountOf accountsOf, Entries journals,
            PosReturnScope returnScope)
            : base(permissions, settings, localization, audit)
        {
            _returnScope = returnScope;
            _accountsOf = accountsOf;
            _journals = journals;
            _invoices = invoices;
            _returns = returns;
            _receipts = receipts;
            _payments = payments;
            _shifts = shifts;
            _held = held;
            _branches = branches;
        }

        public Result<PosShift> Current()
        {
            var shift = _shifts.OpenOf(AppSession.UserId);
            return shift == null ? Fail<PosShift>("NoShift", ErrorCode.NotFound) : Ok(shift);
        }

        public Result<PosShift> Open(decimal openingCash, int? treasuryId, int? warehouseId)
        {
            if (!Can("Open")) return FailDenied<PosShift>();
            if (_shifts.OpenOf(AppSession.UserId) is { } open) return Ok(open);

            var branch = AppSession.BranchId is { } id ? _branches.GetById(id) : null;
            var treasury = branch != null ? branch.TreasuryId : treasuryId;
            var warehouse = branch != null ? branch.WarehouseId : warehouseId;
            if (treasury == null || warehouse == null) return Fail<PosShift>("ShiftNeedsPlace", ErrorCode.ValidationFailed);

            var shift = new PosShift
            {
                BranchId = AppSession.BranchId, UserId = AppSession.UserId, UserName = AppSession.DisplayName,
                TreasuryId = treasury.Value, WarehouseId = warehouse.Value, OpenedAt = DateTime.Now, OpeningCash = openingCash
            };
            shift.Id = _shifts.Insert(shift);
            Audit.Log(EntityName, shift.Id, AuditAction.Insert, newValue: new { shift.OpeningCash, shift.TreasuryId, shift.WarehouseId });
            return Ok(shift);
        }

        public Result<PosShift> Close(decimal countedCash)
        {
            if (!Can("Close")) return FailDenied<PosShift>();
            var current = Current();
            if (current.IsFailure) return current;

            var shift = current.Value;
            var difference = LineCalc.DrawerDifference(countedCash, shift.OpeningCash, shift.CashIn, shift.CashOut);

            var accounts = difference == 0 ? Result.Ok(("", "")) :
                _accountsOf.Treasury(shift.TreasuryId, "Str.Pos.TreasuryAccountMissing").Then(treasury =>
                    _accountsOf.Setting(SettingKeys.Accounts.CashOverShort, "Str.Pos.OverShortMissing").Then(overShort => Result.Ok((treasury, overShort))));
            if (accounts.IsFailure) return accounts.As<PosShift>();

            shift.ClosedAt = DateTime.Now;
            shift.CountedCash = countedCash;
            shift.Difference = difference;
            var closed = Commit(db =>
            {
                if (difference != 0)
                {
                    var (treasury, overShort) = accounts.Value;
                    shift.JournalEntryId = Posting.Entry(_journals, db, DateTime.Today, Msg("ShiftEntry", shift.UserName), EntityName,
                        difference > 0 ? treasury : overShort, difference > 0 ? overShort : treasury, Math.Abs(difference));
                }
                _shifts.Update(shift, db);
                return Result.Ok();
            });
            if (closed.IsFailure) return closed.As<PosShift>();
            Audit.Log(EntityName, shift.Id, AuditAction.Update, newValue: new { shift.CountedCash, shift.Difference });
            return Ok(shift);
        }

        public Result<PosReceipt> Sell(CreateSalesInvoiceDto sale, decimal paid)
        {
            var current = Current();
            if (current.IsFailure) return current.As<PosReceipt>();
            var shift = current.Value;

            sale.WarehouseId = shift.WarehouseId;
            sale.PosShiftId = shift.Id;
            // العميل النقدي افتراضاً ومُقفلاً، إلا إن سمحت الإعدادات بتغييره (Documents.PosCustomerEditable)
            var hasCash = int.TryParse(Setting(SettingKeys.Documents.CashCustomer, ""), out var cash);
            if ((sale.CustomerId == 0 || !Setting(SettingKeys.Documents.PosCustomerEditable, false)) && hasCash)
                sale.CustomerId = cash;

            // العميل النقدي لا يُدين: ما نقص من الدفع عجزٌ على حساب العجز والزيادة، والقبض بالفاتورة كاملة
            var isCash = hasCash && sale.CustomerId == cash;
            var shortAccounts = !isCash ? Result.Ok(("", "")) :
                _accountsOf.Treasury(shift.TreasuryId, "Str.Pos.TreasuryAccountMissing").Then(treasury =>
                    _accountsOf.Setting(SettingKeys.Accounts.CashOverShort, "Str.Pos.OverShortMissing").Then(overShort => Result.Ok((treasury, overShort))));
            if (shortAccounts.IsFailure) return shortAccounts.As<PosReceipt>();

            var created = _invoices.Create(sale);
            if (created.IsFailure) return created.As<PosReceipt>();

            var invoice = created.Value;
            var (collected, change) = LineCalc.Tender(paid, invoice.NetTotal);
            var shortfall = isCash ? LineCalc.Shortfall(collected, invoice.NetTotal) : 0;
            var settled = collected + shortfall;

            var received = _receipts.Create(new CreateVoucherDto
            {
                VoucherDate = invoice.InvoiceDate, PartyId = invoice.CustomerId, TreasuryId = shift.TreasuryId,
                Amount = settled, Method = (int)PaymentMethod.Cash, Reference = invoice.InvoiceNo,
                Allocations = new List<CreateVoucherAllocationDto> { new() { LineNo = 1, InvoiceNo = invoice.InvoiceNo, Amount = settled } }
            });
            if (received.IsFailure)
            {
                _invoices.Delete(invoice.Id);
                return received.As<PosReceipt>();
            }

            if (shortfall > 0)
            {
                var (treasury, overShort) = shortAccounts.Value;
                Commit(db =>
                {
                    Posting.Entry(_journals, db, invoice.InvoiceDate, Msg("ShortfallEntry", invoice.InvoiceNo), EntityName, overShort, treasury, shortfall);
                    return Result.Ok();
                });
            }

            _shifts.Record(shift.Id, collected, 0, 1, 0);
            return Ok(new PosReceipt(invoice.Id, invoice.InvoiceNo, invoice.NetTotal, collected, change));
        }

        public Result<PosReceipt> Return(CreateSalesReturnDto sale)
        {
            var current = Current();
            if (current.IsFailure) return current.As<PosReceipt>();
            if (sale.Lines.Any(l => l.SourceId == 0) && !Can("ManualReturn")) return FailDenied<PosReceipt>();
            if (sale.Lines.Where(l => l.SourceId != 0).Select(l => l.SourceId).Distinct().Any(id => !_returnScope.Allows("SalesInvoices", id)))
                return Fail<PosReceipt>("ReturnNotAllowed", ErrorCode.ValidationFailed);
            var shift = current.Value;

            sale.WarehouseId = shift.WarehouseId;
            var created = _returns.Create(sale);
            if (created.IsFailure) return created.As<PosReceipt>();

            var returned = created.Value;
            var paidOut = _payments.Create(new CreateVoucherDto
            {
                VoucherDate = returned.ReturnDate, PartyId = returned.CustomerId, TreasuryId = shift.TreasuryId,
                Amount = returned.NetTotal, Method = (int)PaymentMethod.Cash, Reference = returned.ReturnNo
            });
            if (paidOut.IsFailure)
            {
                _returns.Delete(returned.Id);
                return paidOut.As<PosReceipt>();
            }

            _shifts.Record(shift.Id, 0, returned.NetTotal, 0, 1);
            return Ok(new PosReceipt(returned.Id, returned.ReturnNo, returned.NetTotal, returned.NetTotal, 0));
        }

        public Result Hold(CreateSalesInvoiceDto sale, string label)
        {
            if (!Can("View")) return FailDenied();
            _held.Insert(new HeldSale
            {
                BranchId = AppSession.BranchId, HeldAt = DateTime.Now, Payload = JsonSerializer.Serialize(sale),
                Label = string.IsNullOrWhiteSpace(label) ? DateTime.Now.ToString("HH:mm") : label.Trim()
            });
            return Result.Ok();
        }

        public Result<List<HeldEntry>> Held() =>
            Can("View") ? Ok(_held.Of(AppSession.BranchId).Select(h => new HeldEntry(h.Id, h.Label, h.HeldAt)).ToList())
                        : FailDenied<List<HeldEntry>>();

        public Result<CreateSalesInvoiceDto> Take(int heldId)
        {
            if (!Can("View")) return FailDenied<CreateSalesInvoiceDto>();
            var held = _held.GetById(heldId);
            if (held == null) return Fail<CreateSalesInvoiceDto>("NotFound", ErrorCode.NotFound);

            _held.Delete(heldId);
            return Ok(JsonSerializer.Deserialize<CreateSalesInvoiceDto>(held.Payload));
        }

        public Result DeleteHeld(int heldId)
        {
            if (!Can("View")) return FailDenied();
            _held.Delete(heldId);
            return Result.Ok();
        }
    }
}
