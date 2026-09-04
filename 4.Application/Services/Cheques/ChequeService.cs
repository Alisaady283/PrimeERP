using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Application.Services.Accounting;
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
        Result<ChequeDocumentResultDto> CreateBatch(CreateChequeDocumentDto dto, ChequeDirection direction);
        Result<List<ChequeDto>> GetOpenForParty(int partyId, DateTime from, DateTime to);
    }

    /// <summary>دورة الشيك كاملة. القاعدة الوحيدة: لا تتغيّر الحالة بلا سطر حركة وقيد مقابلين في نفس المعاملة —
    /// فيبقى "أين الشيك الآن ولماذا" مقروءاً من السجل لا مستنتَجاً.</summary>
    public class ChequeService : ServiceBase, IChequeService
    {
        private readonly IChequeRepository _repo;
        private readonly ITreasuryService _treasuries;
        // المستودعان لا الخدمتان عمداً: خدمة العملاء تستهلك خدمة الشيكات (أسطر الشيك في كشف الحساب)،
        // فاعتمادها هنا على الخدمة يغلق حلقة اعتماد. المطلوب هنا اسم الطرف وحسابه فقط — كلاهما على الكيان.
        private readonly ICustomerRepository _customers;
        private readonly ISupplierRepository _suppliers;
        private readonly IJournalService _journals;
        private readonly ISettingsService _settingsService;

        public ChequeService(IChequeRepository repo, ITreasuryService treasuries, ICustomerRepository customers,
            ISupplierRepository suppliers, IJournalService journals, ISettingsService settingsService,
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
            if (AffectsLedger(target) && (string.IsNullOrWhiteSpace(debit) || string.IsNullOrWhiteSpace(credit)))
                return Result.Fail("لا حساب مرتبط بالخزينة أو بالطرف — اربطهما بحسابيهما أولاً", ErrorCode.ValidationFailed);

            try
            {
                Db.RunTransaction((conn, tx) =>
                {
                    var entryId = AffectsLedger(target) ? PostEntry(conn, tx, cheque, target, debit, credit, dto.MovementDate) : (int?)null;

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

        /// <summary>الشيك لا يمسّ الأرصدة قبل أن يُسدَّد من البنك فعلاً — لذلك القيد يقع عند التحصيل/الصرف
        /// وحدهما، وباقي الحركات (استلام/إيداع/ارتداد/رد) تُسجَّل كحركة بلا قيد. قبلها يظهر الشيك بكشف
        /// الحساب كقيمة استعلامية فقط (راجع GetOpenForParty).</summary>
        private (string Debit, string Credit) ResolveAccounts(Cheque cheque, ChequeStatus target, int? treasuryId)
        {
            var cash  = TreasuryAccount(treasuryId ?? cheque.TreasuryId);
            var party = PartyAccount(cheque);

            return target switch
            {
                ChequeStatus.Collected => (cash, party),
                ChequeStatus.Paid      => (party, cash),
                _ => (null, null)
            };
        }

        private static bool AffectsLedger(ChequeStatus target) =>
            target is ChequeStatus.Collected or ChequeStatus.Paid;

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

            return cheque.PartyKind == PartyKind.Customer
                ? _customers.GetById(cheque.PartyId.Value)?.AccountCode
                : _suppliers.GetById(cheque.PartyId.Value)?.AccountCode;
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

            return c.PartyKind == PartyKind.Customer
                ? _customers.GetById(c.PartyId.Value)?.Name
                : _suppliers.GetById(c.PartyId.Value)?.Name;
        }

        /// <summary>مستند استلام/صرف شيكات: رأس واحد وعدة شيكات — بلا قيد، الشيك يبقى استعلامياً حتى يُسدَّد.</summary>
        public Result<ChequeDocumentResultDto> CreateBatch(CreateChequeDocumentDto dto, ChequeDirection direction)
        {
            if (!Can("Create")) return FailDenied<ChequeDocumentResultDto>();
            if (dto.Lines == null || dto.Lines.Count == 0)
                return Result.Fail<ChequeDocumentResultDto>("المستند يحتاج شيكاً واحداً على الأقل", ErrorCode.ValidationFailed);

            foreach (var line in dto.Lines)
            {
                if (string.IsNullOrWhiteSpace(line.ChequeNo)) return Result.Fail<ChequeDocumentResultDto>("رقم الشيك مطلوب", ErrorCode.ValidationFailed);
                if (line.Amount <= 0) return Result.Fail<ChequeDocumentResultDto>($"مبلغ الشيك {line.ChequeNo} يجب أن يكون أكبر من صفر", ErrorCode.ValidationFailed);
            }

            var status = direction == ChequeDirection.Incoming ? ChequeStatus.InHand : ChequeStatus.Issued;
            var partyKind = direction == ChequeDirection.Incoming ? PartyKind.Customer : PartyKind.Supplier;
            var ids = new List<int>();

            Db.RunTransaction((conn, tx) =>
            {
                foreach (var line in dto.Lines)
                {
                    var cheque = new Cheque
                    {
                        ChequeNo = line.ChequeNo, Direction = direction, PartyKind = partyKind,
                        PartyId = line.PartyId ?? dto.PartyId, Amount = line.Amount,
                        IssueDate = dto.DocDate, DueDate = line.DueDate ?? dto.DocDate,
                        BankName = line.BankName, Status = status, Notes = line.Notes, CreatedBy = AppSession.Username
                    };
                    var id = _repo.Insert(conn, tx, cheque);
                    ids.Add(id);

                    _repo.InsertMovement(conn, tx, new ChequeMovement
                    {
                        ChequeId = id, MovementDate = dto.DocDate, FromStatus = status, ToStatus = status,
                        Notes = dto.Notes, CreatedBy = AppSession.Username
                    });
                }
            });

            Audit.Log(EntityName, ids.FirstOrDefault(), AuditAction.Insert, newValue: new { Count = ids.Count, Direction = direction });
            return Result.Ok(new ChequeDocumentResultDto { ChequeIds = ids });
        }

        public Result<List<ChequeDto>> GetOpenForParty(int partyId, DateTime from, DateTime to)
        {
            if (!Can("View")) return FailDenied<List<ChequeDto>>();
            return Result.Ok(_repo.GetOpenForParty(partyId, from, to).Select(ToDto).ToList());
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
