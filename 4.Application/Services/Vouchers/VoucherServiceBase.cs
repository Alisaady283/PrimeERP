using System;
using System.Collections.Generic;
using System.Data.Common;
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
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services.Vouchers
{
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

    /// <summary>سند قبض/صرف: يُرحَّل محاسبياً فور الحفظ، وطرف النقدية هو حساب الخزينة المختارة لا حساباً عاماً.
    /// طريقة "شيك" تُنشئ الشيك وحركته الأولى في نفس المعاملة — نقطة إدخال واحدة للرقم، بلا تكرار.</summary>
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
        private readonly VoucherKind _kind;

        protected VoucherServiceBase(IVoucherRepository repo, IChequeRepository cheques, ITreasuryService treasuries,
            ICustomerService customers, ISupplierService suppliers, IJournalService journals, INumberSequenceService numbers,
            ISettingsService settingsService, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, VoucherKind kind)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo; _cheques = cheques; _treasuries = treasuries; _customers = customers; _suppliers = suppliers;
            _journals = journals; _numbers = numbers; _settingsService = settingsService; _kind = kind;
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
            if (method == PaymentMethod.Cheque && string.IsNullOrWhiteSpace(dto.ChequeNo))
                return Result.Fail<VoucherDetailDto>("رقم الشيك مطلوب", ErrorCode.ValidationFailed);

            var allocated = (dto.Allocations ?? new()).Sum(a => a.Amount);
            if (allocated > dto.Amount)
                return Result.Fail<VoucherDetailDto>($"مجموع التخصيصات ({allocated:N2}) يتجاوز مبلغ السند ({dto.Amount:N2})", ErrorCode.ValidationFailed);

            var partyAccount = ResolvePartyAccount(dto.PartyId.Value);
            if (string.IsNullOrWhiteSpace(partyAccount))
                return Result.Fail<VoucherDetailDto>("لا حساب مرتبط بالطرف المختار", ErrorCode.ValidationFailed);

            var cashAccount = ResolveCashAccount(treasury.Value.AccountCode, treasury.Value.Kind, method);
            if (method != PaymentMethod.Cheque && string.IsNullOrWhiteSpace(cashAccount))
                return Result.Fail<VoucherDetailDto>("لا حساب مرتبط بالخزينة المختارة — اربطها بحسابها من شاشة الخزائن", ErrorCode.ValidationFailed);

            int voucherId;
            try
            {
                voucherId = Db.RunTransaction((conn, tx) =>
                {
                    var voucher = new Voucher
                    {
                        VoucherNo = _numbers.Next(conn, tx, EntityName), VoucherDate = dto.VoucherDate, Kind = _kind,
                        PartyKind = PartyOf, PartyId = dto.PartyId, TreasuryId = dto.TreasuryId, Amount = dto.Amount,
                        Method = method, Reference = dto.Reference, Notes = dto.Notes, CreatedBy = AppSession.Username
                    };
                    var id = _repo.InsertHeader(conn, tx, voucher);

                    var lineNo = 1;
                    foreach (var allocation in dto.Allocations ?? new())
                        _repo.InsertAllocation(conn, tx, id, new VoucherAllocation
                        {
                            LineNo = lineNo++, InvoiceType = EntityName, InvoiceNo = allocation.InvoiceNo,
                            Amount = allocation.Amount, Notes = allocation.Notes
                        });

                    voucher.Id = id;
                    var entryId = method == PaymentMethod.Cheque ? (int?)null : PostEntry(conn, tx, voucher, cashAccount, partyAccount);
                    var chequeId = method == PaymentMethod.Cheque ? CreateCheque(conn, tx, dto, id, entryId, treasury.Value.Name) : (int?)null;
                    _repo.SetLinks(conn, tx, id, entryId, chequeId);

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
            if (voucher.ChequeId != null) return Result.Fail("السند مرتبط بشيك — عالج الشيك أولاً", ErrorCode.ValidationFailed);

            Db.RunTransaction((conn, tx) =>
            {
                if (voucher.JournalEntryId != null) _journals.Delete(conn, tx, voucher.JournalEntryId.Value);
                _repo.Delete(conn, tx, id);
            });

            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        // قبض: النقدية مدينة والعميل دائن. صرف: المورد مدين والنقدية دائنة.
        private int PostEntry(DbConnection conn, DbTransaction tx, Voucher voucher, string cashAccount, string partyAccount)
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

            var created = _journals.Create(conn, tx, entry);
            if (created.IsFailure) throw new InvalidOperationException(created.ErrorMessage);

            var posted = _journals.Post(conn, tx, created.Value.Id);
            if (posted.IsFailure) throw new InvalidOperationException(posted.ErrorMessage);

            return created.Value.Id;
        }

        private int CreateCheque(DbConnection conn, DbTransaction tx, CreateVoucherDto dto, int voucherId, int? entryId, string treasuryName)
        {
            var cheque = new Cheque
            {
                ChequeNo = dto.ChequeNo, Direction = IsReceipt ? ChequeDirection.Incoming : ChequeDirection.Outgoing,
                PartyKind = PartyOf, PartyId = dto.PartyId, Amount = dto.Amount,
                IssueDate = dto.VoucherDate, DueDate = dto.ChequeDueDate ?? dto.VoucherDate,
                BankName = string.IsNullOrWhiteSpace(dto.ChequeBank) ? treasuryName : dto.ChequeBank, Status = IsReceipt ? ChequeStatus.InHand : ChequeStatus.Issued,
                TreasuryId = dto.TreasuryId, VoucherId = voucherId, CreatedBy = AppSession.Username
            };
            var chequeId = _cheques.Insert(conn, tx, cheque);

            _cheques.InsertMovement(conn, tx, new ChequeMovement
            {
                ChequeId = chequeId, MovementDate = dto.VoucherDate, FromStatus = cheque.Status, ToStatus = cheque.Status,
                TreasuryId = dto.TreasuryId, JournalEntryId = entryId, Notes = "إنشاء من السند", CreatedBy = AppSession.Username
            });

            return chequeId;
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

        // الشيك بلا قيد إطلاقاً حتى يُسدَّد من البنك — القيد يقع وقتها عبر حركة الشيك (ChequeService)،
        // وحتى ذلك الحين يظهر بكشف حساب الطرف كقيمة استعلامية لا تمسّ الرصيد.
        private string ResolveCashAccount(string treasuryAccount, TreasuryKind treasuryKind, PaymentMethod method)
        {
            if (method == PaymentMethod.Cheque) return null;

            if (!string.IsNullOrWhiteSpace(treasuryAccount)) return treasuryAccount;

            return _settingsService.Get<string>(treasuryKind == TreasuryKind.Bank ? SettingKeys.Accounts.Bank : SettingKeys.Accounts.Cash, "");
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
            ISettingsService settingsService, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit)
            : base(repo, cheques, treasuries, customers, suppliers, journals, numbers, settingsService, permissions,
                   settings, localization, audit, VoucherKind.Receipt) { }
    }

    public class PaymentVoucherService : VoucherServiceBase, IPaymentVoucherService
    {
        public PaymentVoucherService(IVoucherRepository repo, IChequeRepository cheques, ITreasuryService treasuries,
            ICustomerService customers, ISupplierService suppliers, IJournalService journals, INumberSequenceService numbers,
            ISettingsService settingsService, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit)
            : base(repo, cheques, treasuries, customers, suppliers, journals, numbers, settingsService, permissions,
                   settings, localization, audit, VoucherKind.Payment) { }
    }
}
