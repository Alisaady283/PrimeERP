using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
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
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Admin;

namespace PrimeERP.Application.Services.Cheques
{
    /// <summary>دورة الشيك كاملة</summary>
    public interface IChequeService
    {
        Result<PagedResult<ChequeDto>> GetPaged(int page, int pageSize, ChequeFilter filter = null);
        Result<ChequeDetailDto> GetById(int id);
        Result<List<ChequeStatus>> GetAllowedTransitions(int id);
        Result Move(MoveChequeDto dto);
        Result<ChequeDocumentResultDto> CreateBatch(CreateChequeDocumentDto dto, ChequeDirection direction);

        Result UpdateUnmoved(int id, CreateChequeLineDto line, DateTime docDate);
        Result DeleteUnmoved(int id);
        Result<List<ChequeDto>> GetOpenForParty(int partyId, DateTime from, DateTime to);
    }

    public class ChequeService : ServiceBase, IChequeService
    {
        private readonly IChequeRepository _repo;
        private readonly ITreasuryService _treasuries;
        private readonly IPartyRepository<Customer> _customers;
        private readonly IPartyRepository<Supplier> _suppliers;
        private readonly IJournalService _journals;
        private readonly ISettingsService _settingsService;

        public ChequeService(IChequeRepository repo, ITreasuryService treasuries, IPartyRepository<Customer> customers,
            IPartyRepository<Supplier> suppliers, IJournalService journals, ISettingsService settingsService,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo; _treasuries = treasuries; _customers = customers; _suppliers = suppliers;
            _journals = journals; _settingsService = settingsService;
        }

        protected override string PermissionPrefix => "Cheques";
        protected override string StringPrefix => "Str.Cheque";
        protected override string EntityName => "Cheque";

        private static readonly Dictionary<ChequeStatus, ChequeStatus[]> Allowed = new()
        {
            [ChequeStatus.InHand]    = new[] { ChequeStatus.Deposited, ChequeStatus.Collected, ChequeStatus.Returned },
            [ChequeStatus.Deposited] = new[] { ChequeStatus.Collected, ChequeStatus.Bounced, ChequeStatus.InHand },
            [ChequeStatus.Bounced]   = new[] { ChequeStatus.Deposited, ChequeStatus.Returned, ChequeStatus.InHand },
            [ChequeStatus.Collected] = new[] { ChequeStatus.Deposited, ChequeStatus.InHand },
            [ChequeStatus.Returned]  = new[] { ChequeStatus.InHand },
            [ChequeStatus.Issued]    = new[] { ChequeStatus.Paid, ChequeStatus.Bounced },
            [ChequeStatus.Paid]      = new[] { ChequeStatus.Issued },
        };

        private static ChequeStatus InitialStatus(ChequeDirection direction) =>
            direction == ChequeDirection.Incoming ? ChequeStatus.InHand : ChequeStatus.Issued;

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

            var dto = ToDto<ChequeDetailDto>(cheque);
            dto.Status = cheque.Status;
            dto.Direction = cheque.Direction;
            dto.Movements = _repo.GetMovements(id).Select(m => new ChequeMovementDto
            {
                MovementDate = m.MovementDate, FromStatusName = StatusName(m.FromStatus), ToStatusName = StatusName(m.ToStatus),
                Notes = m.Notes, CreatedBy = m.CreatedBy
            }).ToList();

            return Result.Ok(dto);
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

            if (AffectsLedger(target))
            {
                var funds = _journals.EnsureAffordable(new List<CreateJournalLineDto>
                {
                    new() { AccountCode = debit,  Debit = cheque.Amount, Credit = 0 },
                    new() { AccountCode = credit, Debit = 0, Credit = cheque.Amount }
                });
                if (funds.IsFailure) return funds;
            }

            try
            {
                Tx(db =>
                {
                    var entryId = AffectsLedger(target) ? PostEntry(db, cheque, target, debit, credit, dto.MovementDate) : (int?)null;

                    _repo.InsertMovement(db, new ChequeMovement
                    {
                        ChequeId = cheque.Id, MovementDate = dto.MovementDate, FromStatus = cheque.Status, ToStatus = target,
                        TreasuryId = dto.TreasuryId ?? cheque.TreasuryId, JournalEntryId = entryId,
                        Notes = dto.Notes, CreatedBy = AppSession.Username
                    });

                    _repo.SetStatus(db, cheque.Id, target, dto.TreasuryId ?? cheque.TreasuryId);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log(EntityName, cheque.Id, AuditAction.Update, newValue: new { From = cheque.Status, To = target });
            return Result.Ok();
        }

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

        private int PostEntry(PrimeDbContext db, Cheque cheque, ChequeStatus target,
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

            var created = _journals.Create(db, entry);
            if (created.IsFailure) throw new InvalidOperationException(created.ErrorMessage);

            var posted = _journals.Post(db, created.Value.Id);
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

        private ChequeDto ToDto(Cheque c) => ToDto<ChequeDto>(c);

        private T ToDto<T>(Cheque c) where T : ChequeDto, new() => new()
        {
            Id = c.Id, ChequeNo = c.ChequeNo, Amount = c.Amount, IssueDate = c.IssueDate, DueDate = c.DueDate,
            BankName = c.BankName, Notes = c.Notes, PartyId = c.PartyId,
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

        public Result<ChequeDocumentResultDto> CreateBatch(CreateChequeDocumentDto dto, ChequeDirection direction)
        {
            if (!Can("Create")) return FailDenied<ChequeDocumentResultDto>();
            if (dto.Lines == null || dto.Lines.Count == 0)
                return Result.Fail<ChequeDocumentResultDto>("المستند يحتاج شيكاً واحداً على الأقل", ErrorCode.ValidationFailed);

            foreach (var line in dto.Lines)
            {
                var valid = Check(LineRules, line);
                if (valid.IsFailure) return Result.Fail<ChequeDocumentResultDto>(valid.ErrorMessage, valid.ErrorCode);
            }

            var status = direction == ChequeDirection.Incoming ? ChequeStatus.InHand : ChequeStatus.Issued;
            var partyKind = direction == ChequeDirection.Incoming ? PartyKind.Customer : PartyKind.Supplier;
            var ids = new List<int>();

            Tx(db =>
            {
                foreach (var line in dto.Lines)
                {
                    var cheque = new Cheque
                    {
                        ChequeNo = line.ChequeNo, Direction = direction, PartyKind = partyKind,
                        PartyId = line.PartyId, Amount = line.Amount,
                        IssueDate = dto.DocDate, DueDate = line.DueDate ?? dto.DocDate,
                        BankName = line.BankName, Status = status, Notes = line.Notes, CreatedBy = AppSession.Username
                    };
                    var id = _repo.Insert(db, cheque);
                    ids.Add(id);

                    _repo.InsertMovement(db, new ChequeMovement
                    {
                        ChequeId = id, MovementDate = dto.DocDate, FromStatus = status, ToStatus = status,
                        Notes = dto.Notes, CreatedBy = AppSession.Username
                    });
                }
            });

            Audit.Log(EntityName, ids.FirstOrDefault(), AuditAction.Insert, newValue: new { Count = ids.Count, Direction = direction });
            return Result.Ok(new ChequeDocumentResultDto { ChequeIds = ids });
        }

        private static Result EnsureAtInitialStatus(Cheque cheque, string action) =>
            cheque.Status == InitialStatus(cheque.Direction)
                ? Result.Ok()
                : Result.Fail($"لا يمكن {action} شيك تحرّك — أعِده أولاً إلى {StatusName(InitialStatus(cheque.Direction))} بحركة مضادة", ErrorCode.ValidationFailed);

        private static readonly ChequeLineValidator LineRules = new();

        public Result UpdateUnmoved(int id, CreateChequeLineDto line, DateTime docDate)
        {
            if (!Can("Edit")) return FailDenied();

            var cheque = _repo.GetById(id);
            if (cheque == null) return Result.Fail("الشيك غير موجود", ErrorCode.NotFound);

            var editable = EnsureAtInitialStatus(cheque, "تعديل");
            if (editable.IsFailure) return editable;

            var valid = Check(LineRules, line);
            if (valid.IsFailure) return valid;

            cheque.ChequeNo = line.ChequeNo;
            cheque.PartyId  = line.PartyId;
            cheque.Amount   = line.Amount;
            cheque.IssueDate = docDate;
            cheque.DueDate  = line.DueDate ?? docDate;
            cheque.BankName = line.BankName;
            cheque.Notes    = line.Notes;

            Tx(db => _repo.Update(db, cheque));
            Audit.Log(EntityName, id, AuditAction.Update, newValue: cheque);
            return Result.Ok();
        }

        public Result DeleteUnmoved(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var cheque = _repo.GetById(id);
            if (cheque == null) return Result.Fail("الشيك غير موجود", ErrorCode.NotFound);

            var deletable = EnsureAtInitialStatus(cheque, "حذف");
            if (deletable.IsFailure) return deletable;

            Tx(db =>
            {
                _repo.DeleteMovements(db, id);
                _repo.Delete(db, id);
            });

            Audit.Log(EntityName, id, AuditAction.Delete, oldValue: cheque);
            return Result.Ok();
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
