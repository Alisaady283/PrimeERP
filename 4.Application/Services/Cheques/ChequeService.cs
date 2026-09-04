using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Cheques;
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

namespace PrimeERP.Application.Services.Cheques
{
    public interface IChequeService
    {
        Result<PagedResult<ChequeDto>> GetPaged(int page, int pageSize, ChequeFilter filter = null);
        Result<ChequeDetailDto> GetById(int id);
        Result<List<ChequeStatus>> GetAllowedTransitions(int id);
        Result Move(MoveChequeDto dto);
    }

    /// <summary>دورة الشيك كاملة. القاعدة الوحيدة: لا تتغيّر الحالة بلا سطر حركة وقيد مقابلين في نفس المعاملة —
    /// فيبقى "أين الشيك الآن ولماذا" مقروءاً من السجل لا مستنتَجاً.</summary>
    public class ChequeService : ServiceBase, IChequeService
    {
        private readonly IChequeRepository _repo;
        private readonly ITreasuryService _treasuries;
        private readonly ICustomerService _customers;
        private readonly ISupplierService _suppliers;
        private readonly IJournalService _journals;
        private readonly ISettingsService _settingsService;

        public ChequeService(IChequeRepository repo, ITreasuryService treasuries, ICustomerService customers,
            ISupplierService suppliers, IJournalService journals, ISettingsService settingsService,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo; _treasuries = treasuries; _customers = customers; _suppliers = suppliers;
            _journals = journals; _settingsService = settingsService;
        }

        protected override string PermissionPrefix => "Cheques";
        protected override string StringPrefix => "Str.Cheque";
        protected override string EntityName => "Cheque";

        // الانتقالات المسموحة — الوارد يسار، الصادر يمين. أي انتقال خارجها مرفوض بلا استثناء.
        private static readonly Dictionary<ChequeStatus, ChequeStatus[]> Allowed = new()
        {
            [ChequeStatus.InHand]    = new[] { ChequeStatus.Deposited, ChequeStatus.Collected, ChequeStatus.Returned },
            [ChequeStatus.Deposited] = new[] { ChequeStatus.Collected, ChequeStatus.Bounced },
            [ChequeStatus.Bounced]   = new[] { ChequeStatus.Deposited, ChequeStatus.Returned },
            [ChequeStatus.Collected] = Array.Empty<ChequeStatus>(),
            [ChequeStatus.Returned]  = Array.Empty<ChequeStatus>(),
            [ChequeStatus.Issued]    = new[] { ChequeStatus.Paid, ChequeStatus.Bounced },
            [ChequeStatus.Paid]      = Array.Empty<ChequeStatus>(),
        };

        public Result<PagedResult<ChequeDto>> GetPaged(int page, int pageSize, ChequeFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<ChequeDto>>();
            filter ??= new ChequeFilter();

            var direction = filter.Direction == 0 ? (ChequeDirection?)null : (ChequeDirection)filter.Direction;
            var status = filter.Status == 0 ? (ChequeStatus?)null : (ChequeStatus)filter.Status;

            var (items, total) = _repo.GetPaged(direction, status, page, pageSize, filter.SearchText);
            return Result.Ok(new PagedResult<ChequeDto>
            { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<ChequeDetailDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<ChequeDetailDto>();

            var cheque = _repo.GetById(id);
            if (cheque == null) return Result.Fail<ChequeDetailDto>("الشيك غير موجود", ErrorCode.NotFound);

            var dto = ToDto(cheque);
            return Result.Ok(new ChequeDetailDto
            {
                Id = dto.Id, ChequeNo = dto.ChequeNo, DirectionName = dto.DirectionName, PartyName = dto.PartyName,
                Amount = dto.Amount, IssueDate = dto.IssueDate, DueDate = dto.DueDate, BankName = dto.BankName,
                StatusName = dto.StatusName, TreasuryName = dto.TreasuryName, Notes = dto.Notes,
                Status = cheque.Status, Direction = cheque.Direction,
                Movements = _repo.GetMovements(id).Select(m => new ChequeMovementDto
                {
                    MovementDate = m.MovementDate, FromStatusName = StatusName(m.FromStatus), ToStatusName = StatusName(m.ToStatus),
                    Notes = m.Notes, CreatedBy = m.CreatedBy
                }).ToList()
            });
        }

        public Result<List<ChequeStatus>> GetAllowedTransitions(int id)
        {
            var cheque = _repo.GetById(id);
            if (cheque == null) return Result.Fail<List<ChequeStatus>>("الشيك غير موجود", ErrorCode.NotFound);

            return Result.Ok(Allowed[cheque.Status].ToList());
        }

        public Result Move(MoveChequeDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var cheque = _repo.GetById(dto.ChequeId);
            if (cheque == null) return Result.Fail("الشيك غير موجود", ErrorCode.NotFound);

            var target = (ChequeStatus)dto.ToStatus;
            if (!Allowed[cheque.Status].Contains(target))
                return Result.Fail($"لا يمكن نقل الشيك من {StatusName(cheque.Status)} إلى {StatusName(target)}", ErrorCode.ValidationFailed);

            var (debit, credit) = ResolveAccounts(cheque, target, dto.TreasuryId);
            if (string.IsNullOrWhiteSpace(debit) || string.IsNullOrWhiteSpace(credit))
                return Result.Fail("حسابات دورة الشيكات غير مضبوطة — راجع تبويب الحسابات في الإعدادات", ErrorCode.ValidationFailed);

            try
            {
                Db.RunTransaction((conn, tx) =>
                {
                    var entryId = PostEntry(conn, tx, cheque, target, debit, credit, dto.MovementDate);

                    _repo.InsertMovement(conn, tx, new ChequeMovement
                    {
                        ChequeId = cheque.Id, MovementDate = dto.MovementDate, FromStatus = cheque.Status, ToStatus = target,
                        TreasuryId = dto.TreasuryId ?? cheque.TreasuryId, JournalEntryId = entryId,
                        Notes = dto.Notes, CreatedBy = AppSession.Username
                    });

                    _repo.SetStatus(conn, tx, cheque.Id, target, dto.TreasuryId ?? cheque.TreasuryId);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log(EntityName, cheque.Id, AuditAction.Update, newValue: new { From = cheque.Status, To = target });
            return Result.Ok();
        }

        /// <summary>يحدّد طرفَي القيد لكل انتقال: الخروج من الحساب القديم والدخول في الجديد.</summary>
        private (string Debit, string Credit) ResolveAccounts(Cheque cheque, ChequeStatus target, int? treasuryId)
        {
            var inHand     = _settingsService.Get<string>(SettingKeys.Accounts.ChequesInHand, "");
            var collection = _settingsService.Get<string>(SettingKeys.Accounts.ChequesUnderCollection, "");
            var payable    = _settingsService.Get<string>(SettingKeys.Accounts.ChequesPayable, "");
            var cash       = TreasuryAccount(treasuryId ?? cheque.TreasuryId);
            var party      = PartyAccount(cheque);

            return target switch
            {
                ChequeStatus.Deposited => (collection, cheque.Status == ChequeStatus.Bounced ? inHand : inHand),
                ChequeStatus.Collected => (cash, cheque.Status == ChequeStatus.Deposited ? collection : inHand),
                ChequeStatus.Bounced   => cheque.Direction == ChequeDirection.Incoming ? (inHand, collection) : (payable, payable),
                ChequeStatus.Returned  => (party, inHand),
                ChequeStatus.Paid      => (payable, cash),
                _ => (null, null)
            };
        }

        private int PostEntry(DbConnection conn, DbTransaction tx, Cheque cheque, ChequeStatus target,
            string debitAccount, string creditAccount, DateTime date)
        {
            var description = $"شيك {cheque.ChequeNo} — {StatusName(target)}";
            var entry = new CreateJournalDto
            {
                EntryDate = date, Description = description, Source = EntityName,
                Lines = new List<CreateJournalLineDto>
                {
                    new() { LineNo = 1, AccountCode = debitAccount,  Debit = cheque.Amount, Credit = 0, Notes = description },
                    new() { LineNo = 2, AccountCode = creditAccount, Debit = 0, Credit = cheque.Amount, Notes = description },
                }
            };

            var created = _journals.Create(conn, tx, entry);
            if (created.IsFailure) throw new InvalidOperationException(created.ErrorMessage);

            var posted = _journals.Post(conn, tx, created.Value.Id);
            if (posted.IsFailure) throw new InvalidOperationException(posted.ErrorMessage);

            return created.Value.Id;
        }

        private string TreasuryAccount(int? treasuryId)
        {
            if (treasuryId == null) return _settingsService.Get<string>(SettingKeys.Accounts.Bank, "");

            var treasury = _treasuries.GetById(treasuryId.Value);
            if (treasury.IsFailure) return _settingsService.Get<string>(SettingKeys.Accounts.Bank, "");

            return !string.IsNullOrWhiteSpace(treasury.Value.AccountCode)
                ? treasury.Value.AccountCode
                : _settingsService.Get<string>(treasury.Value.Kind == TreasuryKind.Bank ? SettingKeys.Accounts.Bank : SettingKeys.Accounts.Cash, "");
        }

        private string PartyAccount(Cheque cheque)
        {
            if (cheque.PartyId == null) return null;

            if (cheque.PartyKind == PartyKind.Customer)
            {
                var customer = _customers.GetById(cheque.PartyId.Value);
                return customer.IsSuccess ? customer.Value.AccountCode : null;
            }

            var supplier = _suppliers.GetById(cheque.PartyId.Value);
            return supplier.IsSuccess ? supplier.Value.AccountCode : null;
        }

        private ChequeDto ToDto(Cheque c) => new()
        {
            Id = c.Id, ChequeNo = c.ChequeNo, Amount = c.Amount, IssueDate = c.IssueDate, DueDate = c.DueDate,
            BankName = c.BankName, Notes = c.Notes,
            DirectionName = c.Direction == ChequeDirection.Incoming ? "وارد" : "صادر",
            StatusName = StatusName(c.Status),
            PartyName = PartyNameOf(c),
            TreasuryName = c.TreasuryId == null ? null : _treasuries.GetById(c.TreasuryId.Value).Value?.Name
        };

        private string PartyNameOf(Cheque c)
        {
            if (c.PartyId == null) return null;

            if (c.PartyKind == PartyKind.Customer)
            {
                var customer = _customers.GetById(c.PartyId.Value);
                return customer.IsSuccess ? customer.Value.Name : null;
            }

            var supplier = _suppliers.GetById(c.PartyId.Value);
            return supplier.IsSuccess ? supplier.Value.Name : null;
        }

        public static string StatusName(ChequeStatus status) => status switch
        {
            ChequeStatus.InHand    => "بالمحفظة",
            ChequeStatus.Deposited => "مودع بالبنك",
            ChequeStatus.Collected => "محصّل",
            ChequeStatus.Bounced   => "مرتد",
            ChequeStatus.Returned  => "مردود للطرف",
            ChequeStatus.Issued    => "صادر",
            ChequeStatus.Paid      => "مصروف",
            _ => status.ToString()
        };
    }
}
