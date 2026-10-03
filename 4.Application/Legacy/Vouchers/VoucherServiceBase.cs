using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Application.Legacy.Documents;
using PrimeERP.Application.Services.Ledger;

namespace PrimeERP.Application.Legacy.Vouchers
{
    /// <summary>سند قبض/صرف</summary>
    public interface IVoucherService
    {
        Result<PagedResult<VoucherDto>> GetPaged(int page, int pageSize, VoucherFilter filter = null);
        Result<VoucherDetailDto> GetById(int id);
        Result<VoucherDetailDto> Create(CreateVoucherDto dto);
        Result Update(CreateVoucherDto dto);
        Result Delete(int id);
    }

    public interface IReceiptVoucherService : IVoucherService { }
    public interface IPaymentVoucherService : IVoucherService { }

    public abstract class VoucherServiceBase
        : DocumentService<Voucher, VoucherDto, VoucherDetailDto, CreateVoucherDto, VoucherFilter>, IVoucherService
    {
        private readonly IVoucherRepository _repo;
        private readonly INumberSequenceService _numbers;
        private readonly VoucherKind _kind;
        private readonly PartyByKind _parties;
        private readonly ITreasuryRepository _treasuries;
        private readonly AccountOf _accountOf;

        protected VoucherServiceBase(IVoucherRepository repo, Entries journals,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, VoucherKind kind,
            PartyByKind parties, ITreasuryRepository treasuries,
            AccountOf accountOf)
            : base(permissions, settings, localization, audit, journals: journals)
        {
            _repo = repo; _numbers = numbers; _kind = kind;
            _parties = parties; _treasuries = treasuries; _accountOf = accountOf;
        }

        private bool IsReceipt => _kind == VoucherKind.Receipt;
        private PartyKind PartyOf => IsReceipt ? PartyKind.Customer : PartyKind.Supplier;

        protected override string PermissionPrefix => IsReceipt ? "Receipts" : "Payments";
        protected override string StringPrefix => "Str.Voucher";
        protected override string EntityName => IsReceipt ? "ReceiptVoucher" : "PaymentVoucher";
        protected override bool Editable => true;

        protected override Voucher FindHead(int id) => _repo.GetById(id) is { } v && v.Kind == _kind ? v : null;
        protected override int IdOf(Voucher head) => head.Id;
        protected override int IdOf(CreateVoucherDto dto) => dto.Id;
        protected override int? EntryOf(Voucher head) => head.JournalEntryId;
        protected override object AuditValue(Voucher head) => new { head?.Amount, head?.PartyId, head?.TreasuryId };

        protected override (List<Voucher> Items, int Total) FindPage(int page, int pageSize, VoucherFilter filter)
        {
            filter ??= new VoucherFilter();
            return _repo.GetPaged(_kind, page, pageSize, filter.SearchText, filter.SortDescending);
        }

        protected override List<VoucherDto> ToRows(List<Voucher> heads)
        {
            var parties = PartyNames(heads.Select(v => v.PartyId));
            var treasuries = _treasuries.NamesOf(heads.Select(v => v.TreasuryId));
            return heads.Select(v => ToDto<VoucherDto>(v, parties, treasuries)).ToList();
        }

        protected override VoucherDetailDto ToDetail(Voucher head)
        {
            var detail = ToDto<VoucherDetailDto>(head, PartyNames(new[] { head.PartyId }), _treasuries.NamesOf(new[] { head.TreasuryId }));
            detail.Allocations = _repo.GetAllocations(head.Id).Select(a => Rows.Copy(a, new VoucherAllocationDto())).ToList();
            return detail;
        }

        protected override Result<Func<PrimeDbContext, int>> Plan(CreateVoucherDto dto)
        {
            var input = Result.Combine(
                Check.Valid(dto,
                    new Field<CreateVoucherDto>(x => x.Amount, "Str.Amount", Positive: true),
                    new Field<CreateVoucherDto>(x => x.PartyId, "", Required: true, Message: IsReceipt ? "Str.Voucher.CustomerRequired" : "Str.Voucher.SupplierRequired")),
                DocumentLines.Within(dto.Allocations, a => a.Amount, dto.Amount, "Str.Voucher.AllocationsExceed"));
            if (input.IsFailure) return input.As<Func<PrimeDbContext, int>>();

            var accounts = _accountOf.Treasury(dto.TreasuryId, "Str.Voucher.TreasuryAccountMissing").Then(cash =>
                _accountOf.Party(PartyOf, dto.PartyId.Value, "Str.Voucher.PartyAccountMissing").Then(party => Result.Ok((cash, party))));
            if (accounts.IsFailure) return accounts.As<Func<PrimeDbContext, int>>();
            var (cashAccount, partyAccount) = accounts.Value;

            return Result.Ok<Func<PrimeDbContext, int>>(db =>
            {
                var voucher = Rows.Copy(dto, new Voucher(), to =>
                {
                    to.VoucherNo = _numbers.Next(db, EntityName);
                    to.Kind = _kind;
                    to.PartyKind = PartyOf;
                    to.Method = (PaymentMethod)dto.Method;
                });
                var id = _repo.InsertHeader(db, voucher);

                var lineNo = 1;
                foreach (var allocation in dto.Allocations ?? new())
                    _repo.InsertAllocation(db, id, Rows.Copy(allocation, new VoucherAllocation(), to =>
                    {
                        to.LineNo = lineNo++;
                        to.InvoiceType = EntityName;
                    }));

                var (debit, credit) = TwoSided.By(IsReceipt, cashAccount, partyAccount);
                var entryId = Posting.Entry(Journals, db, voucher.VoucherDate, Msg(IsReceipt ? "ReceiptEntry" : "PaymentEntry", voucher.VoucherNo),
                    EntityName, debit, credit, voucher.Amount);
                _repo.SetLinks(db, id, entryId, null);
                return id;
            });
        }

        protected override void Remove(PrimeDbContext db, Voucher head)
        {
            Posting.Reverse(Journals, db, head.JournalEntryId);
            _repo.Delete(db, head.Id);
        }

        private static T ToDto<T>(Voucher v, IReadOnlyDictionary<int, string> parties, IReadOnlyDictionary<int, string> treasuries)
            where T : VoucherDto, new() => Rows.Copy<T>(v, new(), to =>
            {
                to.PartyName = v.PartyId is int party ? parties.GetValueOrDefault(party) : null;
                to.TreasuryName = treasuries.GetValueOrDefault(v.TreasuryId);
                to.MethodName = v.Method switch { PaymentMethod.Bank => LocalizationService.Get("Str.Voucher.Method.Bank"), PaymentMethod.Cheque => LocalizationService.Get("Str.Voucher.Method.Cheque"), _ => LocalizationService.Get("Str.Voucher.Method.Cash") };
            });

        private Dictionary<int, string> PartyNames(IEnumerable<int?> ids) => _parties.NamesOf(PartyOf, ids);
    }

    public class ReceiptVoucherService : VoucherServiceBase, IReceiptVoucherService
    {
        public ReceiptVoucherService(IVoucherRepository repo, Entries journals,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit,
            PartyByKind parties, ITreasuryRepository treasuries,
            AccountOf accountOf)
            : base(repo, journals, numbers, permissions, settings, localization, audit, VoucherKind.Receipt,
                   parties, treasuries, accountOf) { }
    }

    public class PaymentVoucherService : VoucherServiceBase, IPaymentVoucherService
    {
        public PaymentVoucherService(IVoucherRepository repo, Entries journals,
            INumberSequenceService numbers, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit,
            PartyByKind parties, ITreasuryRepository treasuries,
            AccountOf accountOf)
            : base(repo, journals, numbers, permissions, settings, localization, audit, VoucherKind.Payment,
                   parties, treasuries, accountOf) { }
    }
}
