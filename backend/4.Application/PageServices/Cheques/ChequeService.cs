using AuditAction = PrimeERP.Domain.Enums.AuditAction;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Treasury;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Application.Validation;

namespace PrimeERP.Application.PageServices.Cheques
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
        Result RepairHoldingEntries();
        Result<List<ChequeDto>> GetOpenForParty(int partyId, DateTime from, DateTime to);
    }

    public class ChequeService : ServiceBase, IChequeService
    {
        private readonly IChequeRepository _repo;
        private readonly AccountOf _accountOf;
        private readonly PartyByKind _parties;
        private readonly ITreasuryRepository _treasuries;
        private readonly Entries _journals;

        public ChequeService(IChequeRepository repo, AccountOf accountOf, PartyByKind parties, ITreasuryRepository treasuries, Entries journals,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo;
            _accountOf = accountOf;
            _parties = parties;
            _treasuries = treasuries;
            _journals = journals;
        }

        /// <summary>انتقالات الشيك، والمُرحِّلة منها</summary>
        private static readonly StatusChange<ChequeStatus> Cycle = new(new Dictionary<ChequeStatus, ChequeStatus[]>
        {
            [ChequeStatus.InHand]    = new[] { ChequeStatus.Deposited, ChequeStatus.Collected, ChequeStatus.Returned },
            [ChequeStatus.Deposited] = new[] { ChequeStatus.Collected, ChequeStatus.Bounced, ChequeStatus.InHand },
            [ChequeStatus.Bounced]   = new[] { ChequeStatus.Deposited, ChequeStatus.Returned, ChequeStatus.InHand },
            [ChequeStatus.Collected] = new[] { ChequeStatus.Deposited, ChequeStatus.InHand },
            [ChequeStatus.Returned]  = new[] { ChequeStatus.InHand },
            [ChequeStatus.Issued]    = new[] { ChequeStatus.Paid, ChequeStatus.Bounced },
            [ChequeStatus.Paid]      = new[] { ChequeStatus.Issued },
        }, status => status is ChequeStatus.Collected or ChequeStatus.Paid);

        protected override string PermissionPrefix => "Cheques";
        protected override string StringPrefix => "Str.Cheque";
        protected override string EntityName => "Cheque";

        public Result<PagedResult<ChequeDto>> GetPaged(int page, int pageSize, ChequeFilter filter = null) =>
            Can("View") ? Result.Ok(Page(page, pageSize, filter ?? new ChequeFilter())) : FailDenied<PagedResult<ChequeDto>>();

        public Result<ChequeDetailDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<ChequeDetailDto>();

            var cheque = _repo.GetById(id);
            return cheque == null ? Result.Fail<ChequeDetailDto>(Msg("NotFound"), ErrorCode.NotFound) : Result.Ok(Detail(cheque));
        }

        public Result<List<ChequeStatus>> GetAllowedTransitions(int id)
        {
            var cheque = _repo.GetById(id);
            if (cheque == null) return Result.Fail<List<ChequeStatus>>(Msg("NotFound"), ErrorCode.NotFound);

            return Result.Ok(Cycle.Next(cheque.Status).ToList());
        }

        public Result Move(MoveChequeDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var cheque = _repo.GetById(dto.ChequeId);
            if (cheque == null) return Result.Fail(Msg("NotFound"), ErrorCode.NotFound);

            var target = (ChequeStatus)dto.ToStatus;
            if (!Cycle.Allows(cheque.Status, target))
                return Result.Fail(Msg("MoveRefused", StatusName(cheque.Status), StatusName(target)), ErrorCode.ValidationFailed);

            var holding = Holding(cheque);
            var releases = target == ChequeStatus.Returned || cheque.Direction == ChequeDirection.Outgoing && target == ChequeStatus.Bounced;
            var restores = target == InitialStatus(cheque.Direction) && cheque.Status == ChequeStatus.Returned;

            var accounts = Cycle.Posts(target)
                ? AccountOf.Required(_accountOf.TreasuryOrRoot(dto.TreasuryId ?? cheque.TreasuryId), "Str.Cheque.AccountsMissing")
                    .Then(cash => _accountOf.Setting(HoldingKey(cheque), "Str.Cheque.HoldingMissing")
                    .Then(counter => Result.Ok(TwoSided.By(target == ChequeStatus.Collected, cash, counter))))
                : restores ? HoldingSides(cheque)
                : Result.Ok<(string, string)>((null, null));
            if (accounts.IsFailure) return accounts;
            var (debit, credit) = accounts.Value;
            var released = releases ? holding : null;

            // الخروج يعكس قيد الدخول
            var posted = Cycle.Reverses(cheque.Status)
                ? _repo.GetMovements(cheque.Id).LastOrDefault(m => m.ToStatus == cheque.Status && m.JournalEntryId != null)
                : null;
            var reversible = Posting.EnsureReversible(_journals, posted?.JournalEntryId)
                .Then(() => Posting.EnsureReversible(_journals, released?.JournalEntryId));
            if (reversible.IsFailure) return reversible;

            var moved = Commit(db =>
            {
                foreach (var movement in new[] { posted, released }.Where(m => m != null))
                {
                    Posting.Reverse(_journals, db, movement.JournalEntryId);
                    _repo.SetMovementEntry(db, movement.Id, null);
                }

                var entryId = Cycle.Posts(target) || restores
                    ? Posting.Entry(_journals, db, dto.MovementDate, Msg("EntryDescription", cheque.ChequeNo, StatusName(target)), EntityName,
                        debit, credit, cheque.Amount)
                    : (int?)null;

                _repo.InsertMovement(db, Rows.Copy(dto, new ChequeMovement(), row =>
                {
                    row.ChequeId = cheque.Id;
                    row.FromStatus = cheque.Status;
                    row.ToStatus = target;
                    row.TreasuryId = dto.TreasuryId ?? cheque.TreasuryId;
                    row.JournalEntryId = entryId;
                }));

                _repo.SetStatus(db, cheque.Id, target, dto.TreasuryId ?? cheque.TreasuryId);
                return Result.Ok();
            });
            if (moved.IsFailure) return moved;

            Audit.Log(EntityName, cheque.Id, AuditAction.Update, newValue: new { From = cheque.Status, To = target });
            return Result.Ok();
        }

        public Result<List<ChequeDto>> GetOpenForParty(int partyId, DateTime from, DateTime to) =>
            Can("View") ? Result.Ok(Of(_repo.GetOpenForParty(partyId, from, to))) : FailDenied<List<ChequeDto>>();

        public Result<ChequeDocumentResultDto> CreateBatch(CreateChequeDocumentDto dto, ChequeDirection direction)
        {
            if (!Can("Create")) return FailDenied<ChequeDocumentResultDto>();

            var any = Check.Valid(dto, new Field<CreateChequeDocumentDto>(x => x.Lines, "", Must: d => d.Lines?.Count > 0, Message: "Str.Cheque.NoLines"));
            var valid = any.IsFailure ? any : dto.Lines.Select(l => Check.Valid(l, LineRules)).FirstOrDefault(r => r.IsFailure) ?? Result.Ok();
            if (valid.IsFailure) return valid.As<ChequeDocumentResultDto>();

            var status = InitialStatus(direction);
            var partyKind = direction == ChequeDirection.Incoming ? PartyKind.Customer : PartyKind.Supplier;
            var ids = new List<int>();

            var cheques = dto.Lines.Select(line => Rows.Copy(line, new Cheque(), c =>
            {
                c.Direction = direction;
                c.PartyKind = partyKind;
                c.IssueDate = dto.DocDate;
                c.DueDate = line.DueDate ?? dto.DocDate;
                c.Status = status;
            })).ToList();
            var sides = cheques.Select(HoldingSides).ToList();
            if (sides.FirstOrDefault(s => s.IsFailure) is { } missing) return missing.As<ChequeDocumentResultDto>();

            var created = Commit(db =>
            {
                foreach (var (cheque, side) in cheques.Zip(sides))
                {
                    var id = _repo.Insert(db, cheque);
                    ids.Add(id);

                    _repo.InsertMovement(db, new ChequeMovement
                    {
                        ChequeId = id, MovementDate = dto.DocDate, FromStatus = status, ToStatus = status, Notes = dto.Notes,
                        JournalEntryId = PostHolding(db, cheque, side.Value, dto.DocDate)
                    });
                }
                return Result.Ok();
            });
            if (created.IsFailure) return created.As<ChequeDocumentResultDto>();

            Audit.Log(EntityName, ids.FirstOrDefault(), AuditAction.Insert, newValue: new { Count = ids.Count, Direction = direction });
            return Result.Ok(new ChequeDocumentResultDto { ChequeIds = ids });
        }

        public Result UpdateUnmoved(int id, CreateChequeLineDto line, DateTime docDate)
        {
            if (!Can("Edit")) return FailDenied();

            var cheque = _repo.GetById(id);
            if (cheque == null) return Result.Fail(Msg("NotFound"), ErrorCode.NotFound);

            var editable = Unmoved(cheque, "Str.Action.EditVerb").Then(() => Check.Valid(line, LineRules));
            if (editable.IsFailure) return editable;

            Rows.Copy(line, cheque, c =>
            {
                c.IssueDate = docDate;
                c.DueDate = line.DueDate ?? docDate;
            });

            var holding = Holding(cheque);
            var sides = holding == null ? Result.Ok<(string, string)>((null, null)) : HoldingSides(cheque);
            var ready = sides.Then(() => Posting.EnsureReversible(_journals, holding?.JournalEntryId));
            if (ready.IsFailure) return ready;

            var updated = Commit(db =>
            {
                _repo.Update(db, cheque);
                if (holding == null) return Result.Ok();

                Posting.Reverse(_journals, db, holding.JournalEntryId);
                _repo.SetMovementEntry(db, holding.Id, PostHolding(db, cheque, sides.Value, docDate));
                return Result.Ok();
            });
            if (updated.IsFailure) return updated;
            Audit.Log(EntityName, id, AuditAction.Update, newValue: cheque);
            return Result.Ok();
        }

        public Result DeleteUnmoved(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var cheque = _repo.GetById(id);
            if (cheque == null) return Result.Fail(Msg("NotFound"), ErrorCode.NotFound);

            var holding = Holding(cheque);
            var deletable = Unmoved(cheque, "Str.Action.DeleteVerb").Then(() => Posting.EnsureReversible(_journals, holding?.JournalEntryId));
            if (deletable.IsFailure) return deletable;

            var deleted = Commit(db =>
            {
                Posting.Reverse(_journals, db, holding?.JournalEntryId);
                _repo.DeleteMovements(db, id);
                _repo.Delete(db, id);
                return Result.Ok();
            });
            if (deleted.IsFailure) return deleted;

            Audit.Log(EntityName, id, AuditAction.Delete, oldValue: cheque);
            return Result.Ok();
        }

        public Result RepairHoldingEntries()
        {
            foreach (var cheque in _repo.GetOpen().Where(c => Holding(c) == null))
            {
                var sides = HoldingSides(cheque);
                if (sides.IsFailure) continue;

                var creation = _repo.GetMovements(cheque.Id).FirstOrDefault(m => m.ToStatus == InitialStatus(cheque.Direction));
                if (creation == null) continue;

                Commit(db =>
                {
                    _repo.SetMovementEntry(db, creation.Id, PostHolding(db, cheque, sides.Value, cheque.IssueDate));
                    return Result.Ok();
                });
            }
            return Result.Ok();
        }

        private ChequeMovement Holding(Cheque cheque) =>
            _repo.GetMovements(cheque.Id).LastOrDefault(m => m.ToStatus == InitialStatus(cheque.Direction) && m.JournalEntryId != null);

        private Result<(string Debit, string Credit)> HoldingSides(Cheque cheque) =>
            _accountOf.Setting(HoldingKey(cheque), "Str.Cheque.HoldingMissing")
                .Then(holding => _accountOf.Party(cheque.PartyKind, cheque.PartyId, "Str.Cheque.AccountsMissing")
                .Then(party => Result.Ok(TwoSided.By(cheque.Direction == ChequeDirection.Incoming, holding, party))));

        private static string HoldingKey(Cheque cheque) =>
            cheque.Direction == ChequeDirection.Incoming ? SettingKeys.Accounts.ChequesUnderCollection : SettingKeys.Accounts.ChequesPayable;

        private int PostHolding(PrimeDbContext db, Cheque cheque, (string Debit, string Credit) sides, DateTime date) =>
            Posting.Entry(_journals, db, date, Msg("EntryDescription", cheque.ChequeNo, StatusName(InitialStatus(cheque.Direction))), EntityName,
                sides.Debit, sides.Credit, cheque.Amount);

        private static ChequeStatus InitialStatus(ChequeDirection direction) =>
            direction == ChequeDirection.Incoming ? ChequeStatus.InHand : ChequeStatus.Issued;

        private static Result Unmoved(Cheque cheque, string actionKey) =>
            cheque.Status == InitialStatus(cheque.Direction)
                ? Result.Ok()
                : Result.Fail(LocalizationService.Get("Str.Cheque.MovedLocked", LocalizationService.Get(actionKey), StatusName(InitialStatus(cheque.Direction))),
                    ErrorCode.ValidationFailed);

        private static readonly Field<CreateChequeLineDto>[] LineRules =
        {
            new(x => x.ChequeNo, "Str.Field.ChequeNo", Required: true),
            new(x => x.BankName, "Str.Field.ChequeBank", Required: true),
            new(x => x.Amount, "Str.Amount", Positive: true),
            new(x => x.PartyId, "", Required: true, Message: "Str.Cheque.PartyRequired"),
        };

        private PagedResult<ChequeDto> Page(int page, int pageSize, ChequeFilter filter)
        {
            var direction = filter.Direction == 0 ? (ChequeDirection?)null : (ChequeDirection)filter.Direction;
            var status = filter.Status == 0 ? (ChequeStatus?)null : (ChequeStatus)filter.Status;

            var (items, total) = _repo.GetPaged(direction, status, page, pageSize, filter.SearchText);
            return Paged(items, total, page, pageSize, Of);
        }

        private List<ChequeDto> Of(List<Cheque> cheques)
        {
            var names = NamesFor(cheques);
            return cheques.Select(c => Row<ChequeDto>(c, names)).ToList();
        }

        private ChequeDetailDto Detail(Cheque cheque)
        {
            var dto = Row<ChequeDetailDto>(cheque, NamesFor(new List<Cheque> { cheque }));
            dto.Movements = _repo.GetMovements(cheque.Id).Select(m => Rows.Copy(m, new ChequeMovementDto(), row =>
            {
                row.FromStatusName = StatusName(m.FromStatus);
                row.ToStatusName = StatusName(m.ToStatus);
            })).ToList();
            return dto;
        }

        public static string StatusName(ChequeStatus status) => status switch
        {
            ChequeStatus.InHand    => LocalizationService.Get("Str.Cheque.Status.InHand"),
            ChequeStatus.Deposited => LocalizationService.Get("Str.Cheque.Status.Deposited"),
            ChequeStatus.Collected => LocalizationService.Get("Str.Cheque.Status.Collected"),
            ChequeStatus.Bounced   => LocalizationService.Get("Str.Cheque.Status.Bounced"),
            ChequeStatus.Returned  => LocalizationService.Get("Str.Cheque.Status.Returned"),
            ChequeStatus.Issued    => LocalizationService.Get("Str.Cheque.Outgoing"),
            ChequeStatus.Paid      => LocalizationService.Get("Str.Cheque.Status.Paid"),
            _ => status.ToString()
        };

        /// <summary>أسماء الأطراف والخزائن لصفحة</summary>
        private sealed record ChequeNames(IReadOnlyDictionary<int, string> Customers, IReadOnlyDictionary<int, string> Suppliers,
                                          IReadOnlyDictionary<int, string> Treasuries);

        private ChequeNames NamesFor(List<Cheque> cheques) => new(
            _parties.NamesOf(PartyKind.Customer, cheques.Where(c => c.PartyKind == PartyKind.Customer).Select(c => c.PartyId)),
            _parties.NamesOf(PartyKind.Supplier, cheques.Where(c => c.PartyKind != PartyKind.Customer).Select(c => c.PartyId)),
            _treasuries.NamesOf(cheques.Where(c => c.TreasuryId != null).Select(c => c.TreasuryId.Value)));

        private T Row<T>(Cheque c, ChequeNames names) where T : ChequeDto, new() => Rows.Copy(c, new T(), row =>
        {
            row.DirectionName = c.Direction == ChequeDirection.Incoming ? Msg("Incoming") : Msg("Outgoing");
            row.StatusName = StatusName(c.Status);
            row.PartyName = c.PartyId is int party ? (c.PartyKind == PartyKind.Customer ? names.Customers : names.Suppliers).GetValueOrDefault(party) : null;
            row.TreasuryName = c.TreasuryId is int treasury ? names.Treasuries.GetValueOrDefault(treasury) : null;
        });
    }
}
