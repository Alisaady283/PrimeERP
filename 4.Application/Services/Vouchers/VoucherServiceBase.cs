using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Treasury;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Application.Services.Admin;

namespace PrimeERP.Application.Services.Vouchers
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

    public abstract class VoucherServiceBase : ServiceBase, IVoucherService
    {
        private readonly IVoucherRepository _repo;
        private readonly IChequeRepository _cheques;
        private readonly ITreasuryService _treasuries;
        private readonly ICustomerService _customers;
        private readonly ISupplierService _suppliers;
        private readonly IJournalService _journals;
        private readonly INumberSequenceService _numbers;
        private readonly ISettingsService _settingsService;
        private readonly PrimeERP.Application.Services.Accounting.IAccountService _accounts;
        private readonly VoucherKind _kind;

        protected VoucherServiceBase(IVoucherRepository repo, IChequeRepository cheques, ITreasuryService treasuries,
            ICustomerService customers, ISupplierService suppliers, IJournalService journals, INumberSequenceService numbers,
            ISettingsService settingsService, PrimeERP.Application.Services.Accounting.IAccountService accounts,
            IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, VoucherKind kind)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo; _cheques = cheques; _treasuries = treasuries; _customers = customers; _suppliers = suppliers;
            _journals = journals; _numbers = numbers; _settingsService = settingsService; _accounts = accounts; _kind = kind;
        }

        private bool IsReceipt => _kind == VoucherKind.Receipt;
        private PartyKind PartyOf => IsReceipt ? PartyKind.Customer : PartyKind.Supplier;

        protected override string PermissionPrefix => IsReceipt ? "Receipts" : "Payments";
        protected override string StringPrefix => "Str.Voucher";
        protected override string EntityName => IsReceipt ? "ReceiptVoucher" : "PaymentVoucher";

        public Result<PagedResult<VoucherDto>> GetPaged(int page, int pageSize, VoucherFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<VoucherDto>>();
            filter ??= new VoucherFilter();

            var (items, total) = _repo.GetPaged(_kind, page, pageSize, filter.SearchText, filter.SortDescending);
            return Result.Ok(new PagedResult<VoucherDto>
            { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<VoucherDetailDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<VoucherDetailDto>();

            var voucher = _repo.GetById(id);
            if (voucher == null || voucher.Kind != _kind) return Result.Fail<VoucherDetailDto>("السند غير موجود", ErrorCode.NotFound);

            var dto = ToDto(voucher);
            return Result.Ok(new VoucherDetailDto
            {
                Id = dto.Id, VoucherNo = dto.VoucherNo, VoucherDate = dto.VoucherDate, PartyName = dto.PartyName,
                TreasuryName = dto.TreasuryName, Amount = dto.Amount, MethodName = dto.MethodName, Reference = dto.Reference,
                Notes = dto.Notes, CreatedAt = dto.CreatedAt, PartyId = voucher.PartyId, TreasuryId = voucher.TreasuryId,
                Method = voucher.Method,
                Allocations = _repo.GetAllocations(id).Select(a => new VoucherAllocationDto
                { Id = a.Id, LineNo = a.LineNo, InvoiceNo = a.InvoiceNo, Amount = a.Amount, Notes = a.Notes }).ToList()
            });
        }

        public Result<VoucherDetailDto> Create(CreateVoucherDto dto)
        {
            if (!Can("Create")) return FailDenied<VoucherDetailDto>();
            if (dto.Amount <= 0) return Result.Fail<VoucherDetailDto>("المبلغ يجب أن يكون أكبر من صفر", ErrorCode.ValidationFailed);
            if (dto.PartyId == null) return Result.Fail<VoucherDetailDto>(IsReceipt ? "العميل مطلوب" : "المورد مطلوب", ErrorCode.ValidationFailed);

            var treasury = _treasuries.GetById(dto.TreasuryId);
            if (treasury.IsFailure) return Result.Fail<VoucherDetailDto>("الخزينة غير موجودة", ErrorCode.ValidationFailed);

            var method = (PaymentMethod)dto.Method;

            var allocated = (dto.Allocations ?? new()).Sum(a => a.Amount);
            if (allocated > dto.Amount)
                return Result.Fail<VoucherDetailDto>($"مجموع التخصيصات ({allocated:N2}) يتجاوز مبلغ السند ({dto.Amount:N2})", ErrorCode.ValidationFailed);

            var partyAccount = ResolvePartyAccount(dto.PartyId.Value);
            if (string.IsNullOrWhiteSpace(partyAccount))
                return Result.Fail<VoucherDetailDto>("لا حساب مرتبط بالطرف المختار", ErrorCode.ValidationFailed);

            var cashAccount = treasury.Value.AccountCode;
            if (string.IsNullOrWhiteSpace(cashAccount))
                return Result.Fail<VoucherDetailDto>("لا حساب مرتبط بالخزينة المختارة — اربطها بحسابها من شاشة الخزائن", ErrorCode.ValidationFailed);

            var funds = EnsureFunds(cashAccount, dto.Amount);
            if (funds.IsFailure) return Result.Fail<VoucherDetailDto>(funds.ErrorMessage, funds.ErrorCode);

            int voucherId;
            try
            {
                voucherId = Tx(db =>
                {
                    var voucher = new Voucher
                    {
                        VoucherNo = _numbers.Next(db, EntityName), VoucherDate = dto.VoucherDate, Kind = _kind,
                        PartyKind = PartyOf, PartyId = dto.PartyId, TreasuryId = dto.TreasuryId, Amount = dto.Amount,
                        Method = method, Reference = dto.Reference, Notes = dto.Notes, CreatedBy = AppSession.Username
                    };
                    var id = _repo.InsertHeader(db, voucher);

                    var lineNo = 1;
                    foreach (var allocation in dto.Allocations ?? new())
                        _repo.InsertAllocation(db, id, new VoucherAllocation
                        {
                            LineNo = lineNo++, InvoiceType = EntityName, InvoiceNo = allocation.InvoiceNo,
                            Amount = allocation.Amount, Notes = allocation.Notes
                        });

                    voucher.Id = id;
                    var entryId = PostEntry(db, voucher, cashAccount, partyAccount);
                    _repo.SetLinks(db, id, entryId, null);

                    return id;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<VoucherDetailDto>(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log(EntityName, voucherId, AuditAction.Insert, newValue: new { dto.Amount, dto.PartyId, dto.TreasuryId });
            return GetById(voucherId);
        }

        public Result Update(CreateVoucherDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var deleted = Delete(dto.Id);
            if (deleted.IsFailure) return deleted;

            var recreated = Create(dto);
            return recreated.IsSuccess ? Result.Ok() : Result.Fail(recreated.ErrorMessage, recreated.ErrorCode);
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var voucher = _repo.GetById(id);
            if (voucher == null || voucher.Kind != _kind) return Result.Fail("السند غير موجود", ErrorCode.NotFound);
            if (voucher.JournalEntryId != null)
            {
                var funds = _journals.EnsureRemovable(voucher.JournalEntryId.Value);
                if (funds.IsFailure) return funds;
            }

            Tx(db =>
            {
                if (voucher.ChequeId != null)
                {
                    _cheques.DeleteMovements(db, voucher.ChequeId.Value);
                    _cheques.Delete(db, voucher.ChequeId.Value);
                }

                if (voucher.JournalEntryId != null) _journals.Delete(db, voucher.JournalEntryId.Value);
                _repo.Delete(db, id);
            });

            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private int PostEntry(PrimeDbContext db, Voucher voucher, string cashAccount, string partyAccount)
        {
            var description = $"{(IsReceipt ? "سند قبض" : "سند صرف")} {voucher.VoucherNo}";
            var entry = new CreateJournalDto
            {
                EntryDate = voucher.VoucherDate, Description = description, Source = EntityName,
                Lines = new List<CreateJournalLineDto>
                {
                    new() { LineNo = 1, AccountCode = IsReceipt ? cashAccount : partyAccount, Debit = voucher.Amount, Credit = 0, Notes = description },
                    new() { LineNo = 2, AccountCode = IsReceipt ? partyAccount : cashAccount, Debit = 0, Credit = voucher.Amount, Notes = description },
                }
            };

            var created = _journals.Create(db, entry);
            if (created.IsFailure) throw new InvalidOperationException(created.ErrorMessage);

            var posted = _journals.Post(db, created.Value.Id);
            if (posted.IsFailure) throw new InvalidOperationException(posted.ErrorMessage);

            return created.Value.Id;
        }


        private string ResolvePartyAccount(int partyId)
        {
            if (IsReceipt)
            {
                var customer = _customers.GetById(partyId);
                return customer.IsSuccess ? customer.Value.AccountCode : null;
            }

            var supplier = _suppliers.GetById(partyId);
            return supplier.IsSuccess ? supplier.Value.AccountCode : null;
        }

        private Result EnsureFunds(string cashAccount, decimal amount)
        {
            if (IsReceipt || string.IsNullOrWhiteSpace(cashAccount)) return Result.Ok();

            var account = _accounts.GetByCode(cashAccount);
            if (account.IsFailure) return Result.Fail(account.ErrorMessage, account.ErrorCode);

            if (account.Value.Balance < amount)
                return Result.Fail($"رصيد «{account.Value.Name}» لا يكفي", ErrorCode.ValidationFailed);

            return Result.Ok();
        }


        private VoucherDto ToDto(Voucher v) => new()
        {
            Id = v.Id, VoucherNo = v.VoucherNo, VoucherDate = v.VoucherDate, Amount = v.Amount,
            PartyName = PartyNameOf(v.PartyId),
            TreasuryName = _treasuries.GetById(v.TreasuryId).Value?.Name,
            MethodName = v.Method switch { PaymentMethod.Bank => "تحويل بنكي", PaymentMethod.Cheque => "شيك", _ => "نقدي" },
            Reference = v.Reference, Notes = v.Notes, CreatedAt = v.CreatedAt
        };

        private string PartyNameOf(int? partyId)
        {
            if (partyId == null) return null;

            if (IsReceipt)
            {
                var customer = _customers.GetById(partyId.Value);
                return customer.IsSuccess ? customer.Value.Name : null;
            }

            var supplier = _suppliers.GetById(partyId.Value);
            return supplier.IsSuccess ? supplier.Value.Name : null;
        }
    }

    public class ReceiptVoucherService : VoucherServiceBase, IReceiptVoucherService
    {
        public ReceiptVoucherService(IVoucherRepository repo, IChequeRepository cheques, ITreasuryService treasuries,
            ICustomerService customers, ISupplierService suppliers, IJournalService journals, INumberSequenceService numbers,
            ISettingsService settingsService, PrimeERP.Application.Services.Accounting.IAccountService accounts,
            IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit)
            : base(repo, cheques, treasuries, customers, suppliers, journals, numbers, settingsService, accounts,
                   permissions, settings, localization, audit, VoucherKind.Receipt) { }
    }

    public class PaymentVoucherService : VoucherServiceBase, IPaymentVoucherService
    {
        public PaymentVoucherService(IVoucherRepository repo, IChequeRepository cheques, ITreasuryService treasuries,
            ICustomerService customers, ISupplierService suppliers, IJournalService journals, INumberSequenceService numbers,
            ISettingsService settingsService, PrimeERP.Application.Services.Accounting.IAccountService accounts,
            IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit)
            : base(repo, cheques, treasuries, customers, suppliers, journals, numbers, settingsService, accounts,
                   permissions, settings, localization, audit, VoucherKind.Payment) { }
    }
}
